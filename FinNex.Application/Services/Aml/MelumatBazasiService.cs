using FinNex.Application.DTOs.Aml;
using FinNex.Application.DTOs.Risk;
using FinNex.Application.DTOs.Sorgular;
using FinNex.Application.Helpers.Aml;
using FinNex.Application.Interfaces.Aml;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Sorgular;

namespace FinNex.Application.Services.Aml;

/// <summary>
/// AML → «Məlumat Bazası» (BMI: <c>AML/Sorgular/MelumatBazasi.cs</c>).
///
/// ── NƏDİR ────────────────────────────────────────────────────────────────
/// İKİ tarix + AXTARILANLAR SİYAHISI → 11 Oracle SELECT → hazır
/// `Melumat bazasi.xlsx` şablonunun 11 vərəqi + `Esas_Sehife` xülasəsi.
///
/// ⚠️ SİYAHI MƏCBURİDİR. Əsl BMI proqramı FoxPro-dur
/// (`melumat_bazasi_kodlari.prg`) və 11 sorğunun HAMISI
/// <c>odb.aml_yoxlama</c> cədvəli ilə birləşir — yəni hesabat HEÇ VAXT
/// «bütün dövr» demək olmayıb, həmişə siyahı üzrə süzülüb.
///
/// 22.09.2026-ya qədər FinNex-ə BMI-nin C# nüsxəsi (`MelumatBazasi.cs`)
/// köçürülmüşdü və orada `aml_yoxlama` ÜMUMİYYƏTLƏ YOXDUR → sorğular
/// süzgəcsiz idi. İndi cədvəl <c>{SIYAHI}</c> tokeni ilə əvəz olunub:
/// Exceldən oxunan şəxslər <c>select … from dual union all</c> bloku kimi
/// yapışdırılır (Oracle-a yazmaq qadağandır — CLAUDE.md). Sütun adları
/// BMI ilə eynidir (<c>a_s_a / fin / voen / tel</c>), ona görə 11 sorğunun
/// `where` hissəsi BMI-dən OLDUĞU KİMİ köçüb.
///
/// Siyahı <see cref="BmiLatin"/> ilə hazırlanır — Azəri hərfləri BMI-nin
/// <c>func_utf8_to_latin</c> xəritəsi ilə ASCII-yə endirilir (`Ə → A`!).
///
/// ── TARİX ARALIĞI ────────────────────────────────────────────────────────
/// 11 sorğudan **7-si** dövr qəbul edir (`{DOVREVVEL}` / `{DOVRSON}`), **4-ü**
/// dövrsüzdür və HƏMİŞƏ cari vəziyyəti verir — BMI-də də belədir, «tarix
/// qoyulmayıb» deyil, QƏSDƏN belədir:
///   Owner (təsisçilər), A_M_L (əlaqəli şəxslər), Kred_zamin (kredit zaminləri),
///   Aktiv_hesablar (açıq hesablar).
/// Onlara dövr əlavə etmək məzmunu dəyişər — istifadəçidən soruşmadan etmə.
///
/// ── SQL HARADA SAXLANILIR ────────────────────────────────────────────────
/// <c>OracleSorgular</c> cədvəlində (layihə qaydası — CLAUDE.md), `AmlHesabatService`
/// ilə eyni üsulla. Quraşdırma script-i:
/// <c>docs/sql/aml/92_MelumatBazasi_OracleSorgular.sql</c>.
///
/// ⚠️ Sorğu mətnləri `{DOVREVVEL}` tokeni daşıdığı üçün Risk panelinin
/// kart siyahısına DÜŞMÜR — `RiskService.RiskDoldurabilmir` onları süzür.
/// </summary>
public class MelumatBazasiService : IMelumatBazasiService
{
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorgu;

    /// <summary>Bir vərəqə yüklənən maksimum sətir sayı.</summary>
    private const int MaxSetir = 200000;

    /// <summary>
    /// Eyni anda icra olunan Oracle sorğusunun sayı. 11-i birdən buraxmaq
    /// Oracle-ı yükləyər və bağlantı hovuzunu boşaldar; 1 isə BMI-dəki
    /// ardıcıl (yavaş) davranışa qayıtmaqdır. Batch-lərə bölünəndən sonra
    /// tapşırıq sayı (vərəq × batch) artır, amma paralellik dərəcəsi EYNİ
    /// qalır — Oracle-a yük dəyişmir, sadəcə növbə uzanır.
    /// </summary>
    private const int MaxParalel = 4;

    /// <summary>
    /// Ümumilikdə axtarıla bilən maksimum şəxs sayı — 23.09.2026-da real bir
    /// siyahı (17554 sətir) `BmiLatin.MaxSetir` (5000) həddinə ilişdi, ilk 5000-dən
    /// sonrakı sətirlər axtarılmırdı. İndi siyahı `BmiLatin.MaxSetir`-lik
    /// hissələrə bölünür (hər biri ayrı, tək-özü test edilmiş ölçüdə {SIYAHI}
    /// bloku) və nəticələr vərəq üzrə birləşdirilir — 10 batch = 50 000 şəxsə
    /// qədər tam axtarılır. Bundan yuxarısı hələ də kəsilir (Oracle-a həddindən
    /// artıq round-trip getməsin deyə) və istifadəçiyə açıq bildirilir.
    /// </summary>
    public const int MaxUmumiSetir = 50000;

    int IMelumatBazasiService.MaxUmumiSetir => MaxUmumiSetir;

    public MelumatBazasiService(IOracleService oracle, IOracleSorguService sorgu)
    {
        _oracle = oracle;
        _sorgu = sorgu;
    }

    // Vərəq adları BMI şablonundakı sekme adları ilə EYNİDİR — dəyişsən
    // mühasib/AML işçisi öz köhnə faylı ilə tutuşdura bilməz.
    // Sorğu adları QƏSDƏN ASCII-dir (SSMS-də Azərbaycan hərfləri pozula bilir
    // və `=` müqayisəsi sükutla sınır — AmlHesabatService-də eyni qayda).
    private static readonly MelumatBazasiVereqDto[] Tertib =
    {
        new() { Ad = "Open_Accounts",          Baslik = "Açılmış hesablar",            SorguAdi = "AML_MB_OPEN_ACCOUNTS",   Dovrlu = true  },
        new() { Ad = "Kochurme_Daxili_hes",    Baslik = "Köçürmə — daxili hesablar",   SorguAdi = "AML_MB_KOCURME_DAXILI",  Dovrlu = true  },
        new() { Ad = "Kochurme_Mushteri_hes",  Baslik = "Köçürmə — müştəri hesabları", SorguAdi = "AML_MB_KOCURME_MUSTERI", Dovrlu = true  },
        new() { Ad = "Exchange",               Baslik = "Valyuta mübadiləsi",          SorguAdi = "AML_MB_EXCHANGE",        Dovrlu = true  },
        new() { Ad = "Owner",                  Baslik = "Təsisçilər (owner)",          SorguAdi = "AML_MB_OWNER",           Dovrlu = false },
        new() { Ad = "Emit_benef",             Baslik = "Emitent / benefisiar",        SorguAdi = "AML_MB_EMIT_BENEF",      Dovrlu = true  },
        new() { Ad = "3-cu shexs",             Baslik = "3-cü şəxs əməliyyatları",     SorguAdi = "AML_MB_UCUNCU_SEXS",     Dovrlu = true  },
        new() { Ad = "Transfer",               Baslik = "Transferlər (4 mənbə)",       SorguAdi = "AML_MB_TRANSFER",        Dovrlu = true  },
        new() { Ad = "A_M_L",                  Baslik = "Əlaqəli şəxslər",             SorguAdi = "AML_MB_ELAQELI_SEXS",    Dovrlu = false },
        new() { Ad = "Kred_zamin",             Baslik = "Kredit zaminləri",            SorguAdi = "AML_MB_KREDIT_ZAMIN",    Dovrlu = false },
        new() { Ad = "Aktiv_hesablar",         Baslik = "Aktiv hesablar",              SorguAdi = "AML_MB_AKTIV_HESABLAR",  Dovrlu = false },
    };

    public IReadOnlyList<MelumatBazasiVereqDto> Vereqler => Tertib;

    private List<OracleSorguDto>? _cache;

    private async Task<string?> SqlAl(string ad)
    {
        _cache ??= (await _sorgu.HamisiniGetirAsync())?.Data?.ToList() ?? new List<OracleSorguDto>();
        return _cache.FirstOrDefault(x => x.Aktiv
            && !string.IsNullOrWhiteSpace(x.SorguMetni)
            && string.Equals((x.SorguAdi ?? "").Trim(), ad, StringComparison.OrdinalIgnoreCase))?.SorguMetni;
    }

    public async Task<MelumatBazasiNeticeDto> HazirlaAsync(
        DateTime basTarix,
        DateTime sonTarix,
        IReadOnlyList<AxtarisSetriDto> axtarilanlar,
        AxtarisRejimi rejim = AxtarisRejimi.HerIkisi,
        CancellationToken ct = default)
    {
        var netice = new MelumatBazasiNeticeDto
        {
            BasTarix = basTarix.Date,
            SonTarix = sonTarix.Date,
            Rejim    = rejim
        };

        if (netice.SonTarix < netice.BasTarix)
        {
            netice.Xeta = "Son tarix başlanğıc tarixdən əvvəl ola bilməz.";
            return netice;
        }

        // ── Axtarılanlar siyahısı ────────────────────────────────────────────
        // ⚠️ BOŞ SİYAHI İLƏ İCRA ETMƏ. BMI-də bu, `odb.aml_yoxlama` cədvəli idi;
        // siyahı boş olsa `{SIYAHI}` bloku da boş qalar, `( ) y` sintaksis xətası
        // verər — yaxud (daha pisi) kimsə tokeni silsə sorğular BÜTÜN dövrü
        // qaytarar. Ona görə burada açıq dayanırıq.
        if (axtarilanlar == null || axtarilanlar.Count == 0)
        {
            netice.Xeta = "Axtarılacaq şəxs siyahısı boşdur — əvvəlcə Excel faylını yükləyin.";
            return netice;
        }

        // ── Böyük siyahını Oracle-a yapışdırıla bilən (test edilmiş) ölçüdə
        // hissələrə böl. Hər hissə ayrı, TAM MÜSTƏQİL {SIYAHI} blokudur —
        // nəticələr aşağıda vərəq üzrə birləşdirilir (23.09.2026, real hadisə:
        // 17554 sətirlik siyahının 12554-ü tək bloka sığmadığı üçün axtarılmırdı).
        var axtarilacaqSay = Math.Min(axtarilanlar.Count, MaxUmumiSetir);
        netice.AxtarilanSay = axtarilacaqSay;
        if (axtarilanlar.Count > MaxUmumiSetir)
            netice.Xeberdarliq = $"Siyahıda {axtarilanlar.Count} sətir var, " +
                                 $"yalnız ilk {MaxUmumiSetir} sətir axtarıldı.";

        var axtarilacaqlar = axtarilanlar.Count > axtarilacaqSay
            ? axtarilanlar.Take(axtarilacaqSay).ToList()
            : axtarilanlar;

        var siyahiBatchlari = new List<string>();
        for (var i = 0; i < axtarilacaqlar.Count; i += BmiLatin.MaxSetir)
        {
            var parca = axtarilacaqlar.Skip(i).Take(BmiLatin.MaxSetir).ToList();
            var bloku = BmiLatin.SiyahiQur(parca, rejim);
            if (!string.IsNullOrWhiteSpace(bloku)) siyahiBatchlari.Add(bloku);
        }

        if (siyahiBatchlari.Count == 0)
        {
            netice.Xeta = rejim switch
            {
                AxtarisRejimi.FinVoen => "Siyahıda heç bir FİN və ya VÖEN yoxdur — «yalnız FİN/VÖEN» rejimində axtarılacaq heç nə qalmır.",
                AxtarisRejimi.Ad      => "Siyahıda heç bir ad yoxdur — «yalnız ad» rejimində axtarılacaq heç nə qalmır.",
                _                     => "Siyahıdakı sətirlərin heç birində ad, FİN və ya VÖEN tapılmadı."
            };
            return netice;
        }

        // ⚠️ FORMAT `dd-MM-yyyy` OLMALIDIR — sorğularda `TO_DATE(…,'DD-MM-YYYY')`
        // yazılıb (BMI-dən olduğu kimi). Başqa format versək Oracle ORA-01861
        // verər və ya — daha pisi — günü ayla dəyişik oxuyar (03-04 ↔ 04-03),
        // heç bir xəta çıxmadan YANLIŞ DÖVR gələr.
        var d1 = netice.BasTarix.ToString("dd-MM-yyyy");
        var d2 = netice.SonTarix.ToString("dd-MM-yyyy");

        try
        {
            _cache = (await _sorgu.HamisiniGetirAsync())?.Data?.ToList() ?? new List<OracleSorguDto>();
        }
        catch (Exception ex)
        {
            netice.Xeta = "OracleSorgular oxunmadı: " + ex.Message;
            return netice;
        }

        // ── 1-ci addım: SQL mətnlərini əvvəlcədən həll et ───────────────────
        // Paralel hissədən ƏVVƏL, çünki `_cache` yazılır — paralel yazı yarışdır
        // (CLAUDE.md «Bildirişlər — Paralel Yazı» hadisəsi ilə eyni mexanizm).
        //
        // Hər vərəq üçün BATCH sayı qədər tapşırıq yaranır (vərəq × siyahiBatchlari).
        // Nəticə birbaşa `vereq.Setirler`-ə YAZILMIR — eyni vərəqin bir neçə batch-i
        // paralel bitə bilər və `List<T>.Add` thread-safe deyil (CLAUDE.md «Paralel
        // Yazı» hadisəsi ilə eyni tələ). Hər tapşırıq öz nəticəsini ayrıca saxlayır,
        // `Task.WhenAll`-dan SONRA (artıq ardıcıl) vərəq üzrə birləşdirilir.
        var isler = new List<BatchIsi>();
        foreach (var sablon in Tertib)
        {
            var vereq = new MelumatBazasiVereqDto
            {
                Ad = sablon.Ad, Baslik = sablon.Baslik,
                SorguAdi = sablon.SorguAdi, Dovrlu = sablon.Dovrlu
            };
            netice.Vereqler.Add(vereq);

            var sql = await SqlAl(sablon.SorguAdi);
            if (string.IsNullOrWhiteSpace(sql))
            {
                vereq.Xeta = $"OracleSorgular-da «{sablon.SorguAdi}» tapılmadı (və ya aktiv deyil). " +
                             "docs/sql/aml/92_MelumatBazasi_OracleSorgular.sql işlədilməlidir.";
                continue;
            }

            // ⚠️ `{SIYAHI}` TOKENİ OLMAYAN SORĞU = KÖHNƏ (SÜZGƏCSİZ) VARİANT.
            // `Replace` səssizcə heç nə etməzdi və sorğu BÜTÜN dövrü qaytarardı —
            // istifadəçi isə onu «axtarışın nəticəsi» sanardı. Açıq dayanırıq.
            if (!sql.Contains("{SIYAHI}", StringComparison.Ordinal))
            {
                vereq.Xeta = $"«{sablon.SorguAdi}» köhnə (süzgəcsiz) variantdır — mətnində {{SIYAHI}} tokeni yoxdur. " +
                             "docs/sql/aml/92_MelumatBazasi_OracleSorgular.sql yenidən işlədilməlidir.";
                continue;
            }

            // Dövrsüz sorğuda tarix tokeni yoxdur — `Replace` sadəcə heç nə etmir.
            var sqlTemel = sql.Replace("{DOVREVVEL}", d1).Replace("{DOVRSON}", d2);
            for (var b = 0; b < siyahiBatchlari.Count; b++)
                isler.Add(new BatchIsi
                {
                    Vereq     = vereq,
                    BatchNo   = b + 1,
                    BatchSayi = siyahiBatchlari.Count,
                    Sql       = sqlTemel.Replace("{SIYAHI}", siyahiBatchlari[b])
                });
        }

        // ── 2-ci addım: Oracle sorğuları PARALEL ────────────────────────────
        // BMI-də 11 sorğu BİR-BİR gedirdi (`excelDoldur` ardıcıl çağırışlar) —
        // paketin vaxtı hamısının CƏMİ qədərdir, ona görə «Hazırlanır gözləyin...»
        // uzun çəkirdi. Burada vaxt ən uzun sorğunun özü qədərdir (batch sayı qədər
        // uzanır, çünki eyni vərəqin batch-ləri arasında MaxParalel bölüşülür).
        //
        // TƏHLÜKƏSİZDİR: `OracleService` hər çağırışda ÖZ bağlantısını açır
        // (`new OracleConnection` + `OpenAsync`), ortaq vəziyyət yoxdur.
        // ⚠️ EF tərəfi (`_sorgu`) bu blokda ÇAĞIRILMIR — `DbContext` thread-safe
        // deyil; bütün SQL mətnləri yuxarıda oxunub.
        //
        // Paralellik QƏSDƏN MƏHDUDDUR — batch sayı artsa da (vərəq × batch) eyni
        // anda ən çox `MaxParalel` sorğu Oracle-a gedir, yükü ARTIRMIR.
        using var qapi = new SemaphoreSlim(MaxParalel);
        await Task.WhenAll(isler.Select(async i =>
        {
            await qapi.WaitAsync(ct);
            try
            {
                var xam = await _oracle.SelectXamAsync(i.Sql, MaxSetir, ct);
                i.Sutunlar = xam.Sutunlar;
                i.Setirler = xam.Setirler;
            }
            catch (Exception ex)
            {
                // BİR batch-in xətası paketi DAYANDIRMIR — qalanı (o cümlədən eyni
                // vərəqin digər batch-ləri) yenə hazırlanır.
                var kok = ex; while (kok.InnerException != null) kok = kok.InnerException;
                i.Xeta = ReferenceEquals(kok, ex) ? ex.Message : $"{ex.Message} → {kok.Message}";
                if (i.BatchSayi > 1) i.Xeta += $" (batch {i.BatchNo}/{i.BatchSayi})";
            }
            finally { qapi.Release(); }
        }));

        // ── 3-cü addım: batch nəticələrini vərəq üzrə BİRLƏŞDİR (ardıcıl, indi
        // paralellik bitib — `List<T>.Add` təhlükəsizdir). Sütunlar bütün batch-
        // lərdə eyni sxemdən gəlir, ona görə sadəcə ilk uğurlu olandan götürülür.
        foreach (var qrup in isler.GroupBy(i => i.Vereq))
        {
            var vereq = qrup.Key;
            var xetalar = new List<string>();
            foreach (var i in qrup.OrderBy(i => i.BatchNo))
            {
                if (i.Xeta != null) { xetalar.Add(i.Xeta); continue; }
                if (vereq.Sutunlar.Count == 0 && i.Sutunlar != null) vereq.Sutunlar = i.Sutunlar;
                if (i.Setirler != null) vereq.Setirler.AddRange(i.Setirler);
            }
            if (xetalar.Count > 0)
                vereq.Xeta = (vereq.Xeta != null ? vereq.Xeta + " " : "") + string.Join(" ", xetalar);
        }

        return netice;
    }

    /// <summary>Bir vərəqin BİR batch-inə aid tapşırıq — nəticə paralel mərhələdə
    /// buraya yazılır, vərəqə YAZILMIR (bax yuxarıdakı "3-cü addım" izahı).</summary>
    private sealed class BatchIsi
    {
        public required MelumatBazasiVereqDto Vereq { get; init; }
        public required string Sql { get; init; }
        public int BatchNo { get; init; }
        public int BatchSayi { get; init; }
        public List<string>? Sutunlar { get; set; }
        public List<object?[]>? Setirler { get; set; }
        public string? Xeta { get; set; }
    }
}

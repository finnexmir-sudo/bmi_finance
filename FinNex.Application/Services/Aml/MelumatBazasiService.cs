using FinNex.Application.DTOs.Aml;
using FinNex.Application.DTOs.Sorgular;
using FinNex.Application.Interfaces.Aml;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Sorgular;

namespace FinNex.Application.Services.Aml;

/// <summary>
/// AML → «Məlumat Bazası» (BMI: <c>AML/Sorgular/MelumatBazasi.cs</c>).
///
/// ── NƏDİR ────────────────────────────────────────────────────────────────
/// BMI-də forma İKİ tarixdən ibarətdir («Əvvəlki ay son iş günü» — «Cari ay
/// son iş günü») və «Ümumi sorğu» düyməsi <c>excelDoldur()</c>-u çağırır:
/// 11 Oracle SELECT icra olunur, hər biri hazır `Melumat bazasi.xlsx`
/// şablonunun ÖZ VƏRƏQİNƏ (6-cı sətirdən) yazılır və fayl açılır.
///
/// ⚠️ BU, «Axtarılanlar» Excel yoxlaması DEYİL. Köhnə BMI-də ikisi AYRI
/// formadır (`MelumatBazasi.cs` və `AMLexcel.cs`) və 22.09.2026-ya qədər
/// FinNex-ə səhvən ikincisi «Məlumat Bazası» adı ilə köçürülmüşdü.
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
    /// ardıcıl (yavaş) davranışa qayıtmaqdır.
    /// </summary>
    private const int MaxParalel = 4;

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

    public async Task<MelumatBazasiNeticeDto> HazirlaAsync(DateTime basTarix, DateTime sonTarix, CancellationToken ct = default)
    {
        var netice = new MelumatBazasiNeticeDto { BasTarix = basTarix.Date, SonTarix = sonTarix.Date };

        if (netice.SonTarix < netice.BasTarix)
        {
            netice.Xeta = "Son tarix başlanğıc tarixdən əvvəl ola bilməz.";
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
        var isler = new List<(MelumatBazasiVereqDto Vereq, string? Sql)>();
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
                isler.Add((vereq, null));
                continue;
            }

            // Dövrsüz sorğuda token yoxdur — `Replace` sadəcə heç nə etmir.
            isler.Add((vereq, sql.Replace("{DOVREVVEL}", d1).Replace("{DOVRSON}", d2)));
        }

        // ── 2-ci addım: Oracle sorğuları PARALEL ────────────────────────────
        // BMI-də 11 sorğu BİR-BİR gedirdi (`excelDoldur` ardıcıl çağırışlar) —
        // paketin vaxtı hamısının CƏMİ qədərdir, ona görə «Hazırlanır gözləyin...»
        // uzun çəkirdi. Burada vaxt ən uzun sorğunun özü qədərdir.
        //
        // TƏHLÜKƏSİZDİR: `OracleService` hər çağırışda ÖZ bağlantısını açır
        // (`new OracleConnection` + `OpenAsync`), ortaq vəziyyət yoxdur.
        // ⚠️ EF tərəfi (`_sorgu`) bu blokda ÇAĞIRILMIR — `DbContext` thread-safe
        // deyil; bütün SQL mətnləri yuxarıda oxunub.
        //
        // Paralellik QƏSDƏN MƏHDUDDUR: 11 ağır sorğunu eyni anda buraxmaq
        // Oracle-ı yükləyər və bağlantı hovuzunu boşaldar.
        using var qapi = new SemaphoreSlim(MaxParalel);
        await Task.WhenAll(isler.Where(i => i.Sql != null).Select(async i =>
        {
            await qapi.WaitAsync(ct);
            try
            {
                var xam = await _oracle.SelectXamAsync(i.Sql!, MaxSetir, ct);
                i.Vereq.Sutunlar = xam.Sutunlar;
                i.Vereq.Setirler = xam.Setirler;
            }
            catch (Exception ex)
            {
                // BİR vərəqin xətası paketi DAYANDIRMIR — qalanı yenə hazırlanır.
                // Əks halda bir sorğudakı sxem dəyişikliyi bütün aylıq paketi bloklayardı.
                var kok = ex; while (kok.InnerException != null) kok = kok.InnerException;
                i.Vereq.Xeta = ReferenceEquals(kok, ex) ? ex.Message : $"{ex.Message} → {kok.Message}";
            }
            finally { qapi.Release(); }
        }));

        return netice;
    }
}

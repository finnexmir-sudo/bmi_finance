using FinNex.Application.DTOs.Kredit;
using FinNex.Application.Interfaces.Kredit;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using DereceEntity = FinNex.Domain.Entities.HR.KreditFaizDerecesi;

namespace FinNex.Application.Services.Kredit;

/// <summary>
/// VM 98.2.1 — işçi kreditləri üzrə hesabi (imputed) gəlir.
///
/// İşçiyə bazar dərəcəsindən aşağı faizlə kredit verilirsə fərq gəlir sayılır:
/// işçiyə pul ÖDƏNMİR, amma vergi/DSMF/İTSS/işsizlik bazalarına düşür.
/// Tam izahat: docs/kredit-fayda/VM_98_2_1_Isci_Krediti.md
///
/// DÜSTUR (mühasibin iyul Exceli ilə 15/15 sətir tutuşdurulub):
///     fayda(faiz, derece, bazar) = MAX(0, faiz × (bazar − derece) / derece)
///     hesabi gəlir = fayda(faiz_adi, isci_faizi, bazar)
///                  + fayda(faiz_vk,  vk_faizi,   bazar)
///
/// Hər faiz növü ÖZ dərəcəsi ilə hesablanır. `MAX(0, …)` vacibdir: vaxtı keçmiş
/// faiz bu gün 13%-dir (bazardan baha) — ona görə gəlir yaratmır. İstifadəçi
/// təsdiqi (18.08.2026): «gecikmə faizi MB-dəki faizdən aşağı olduğu halda
/// nəzərə alınacaq» — bu, MAX(0,…)-ın dediyinin eynidir.
///
/// ⚠️ QALIQDAN (`summa`) HESABLAMA OLMAZ — mühasib FAKTİKİ hesablanmış faizdən
/// gedir. Qalıqla getsək 6 nəfərdə tam səhv nəticə çıxır (sənəd §3.2).
/// </summary>
public class IsciKreditFaydaService : IIsciKreditFaydaService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;
    private readonly IKreditFaizDerecesiService _derece;

    // `OracleSorgular` cədvəlindəki adlar — seed: docs/sql/kredit/Isci_Kredit_Faizi_OracleSorgu.sql
    private const string SorguFaiz = "Isci Kredit Faizi";
    private const string SorguFin  = "Isci Kredit FIN";

    public IsciKreditFaydaService(
        IUnitOfWork uow, IOracleService oracle, IKreditFaizDerecesiService derece)
    {
        _uow = uow;
        _oracle = oracle;
        _derece = derece;
    }

    // ── Oracle köməkçiləri ────────────────────────────────────────────────
    // Sütun adı böyük/kiçik hərflə gələ bilər — müqayisə həssas deyil.
    private static string Metn(IDictionary<string, object?> s, string sutun)
    {
        foreach (var kv in s)
            if (string.Equals(kv.Key, sutun, StringComparison.OrdinalIgnoreCase))
                return kv.Value?.ToString()?.Trim() ?? "";
        return "";
    }

    private static decimal Reqem(IDictionary<string, object?> s, string sutun)
    {
        var xam = Metn(s, sutun);
        if (xam.Length == 0) return 0m;
        // Oracle rəqəmi nöqtə ilə qaytarır — InvariantCulture MƏCBURİDİR.
        // Server mədəniyyəti az-AZ olduğu üçün nöqtəli dəyər `Parse`-da 12058
        // kimi oxunardı (CLAUDE.md — mədəniyyət tələsi).
        return decimal.TryParse(xam, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d
            : 0m;
    }

    private async Task<string?> SorguMetniAsync(string ad)
    {
        var sorgu = (await _uow.Repository<OracleSorgu>()
                .HamisiniGetirAsync(x => !x.Silinib && x.Aktiv, izlemeden: true))
            .FirstOrDefault(x => string.Equals((x.SorguAdi ?? "").Trim(), ad,
                StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(sorgu?.SorguMetni) ? null : sorgu!.SorguMetni;
    }

    /// <summary>
    /// Düsturun YEGANƏ yeri. Dərəcə 0/mənfi olarsa 0 qaytarır — məxrəcdir,
    /// sıfıra bölmə istisna atardı.
    /// </summary>
    internal static decimal Fayda(decimal faiz, decimal derece, decimal bazar)
    {
        if (faiz <= 0m || derece <= 0m) return 0m;
        var netice = faiz * (bazar - derece) / derece;
        return netice > 0m ? netice : 0m;   // MAX(0, …)
    }

    public async Task<(DateTime? Bas, DateTime Son)> DovrTeklifAsync()
    {
        // SON = cari gün − 1 (dünən daxil). Bugünkü provodkalar hələ tam deyil.
        var son = DateTime.Today.AddDays(-1);

        // BAS = sonuncu ÖDƏNİLMİŞ maaşın ödəniş tarixi.
        // Təqvim ayı işlətmirik: maaş ayın sonuna yaxın verilir və təqvim ayı ilə
        // gün itir (sənəd §4). Layihə/təsdiq statuslu maaş sayılmır — pul hələ
        // getməyibsə dövr də başlamayıb.
        var bas = await _uow.Repository<Maas>().Query()
            .AsNoTracking()
            .Where(x => x.Status == MaasStatus.Odenildi && x.OdenisTarixi != null)
            .MaxAsync(x => (DateTime?)x.OdenisTarixi);

        return (bas?.Date, son);
    }

    public async Task<IsciKreditFaydaNeticeDto> HesablaAsync(
        DateTime bas, DateTime son, CancellationToken ct = default)
    {
        var netice = new IsciKreditFaydaNeticeDto { Bas = bas.Date, Son = son.Date };

        if (netice.Son < netice.Bas)
        {
            netice.Xeta = "Dövrün bitmə tarixi başlama tarixindən əvvəl ola bilməz.";
            return netice;
        }

        var faizSorgu = await SorguMetniAsync(SorguFaiz);
        if (faizSorgu == null)
        {
            netice.Xeta = $"«{SorguFaiz}» Oracle sorğusu tapılmadı və ya deaktivdir. " +
                          "Seed: docs/sql/kredit/Isci_Kredit_Faizi_OracleSorgu.sql";
            return netice;
        }

        List<Dictionary<string, object?>> faizSetirler;
        Dictionary<string, (string Fin, string Ad)> finXerite;

        try
        {
            var sql = faizSorgu
                .Replace("{BAS}", netice.Bas.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))
                .Replace("{SON}", netice.Son.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

            faizSetirler = await _oracle.SelectAsync(sql, 1000, ct);
            finXerite = await FinXeriteAsync(ct);
        }
        catch (Exception ex)
        {
            // İstisna ATMIRIQ: bu servis maaş səhifəsindən çağırılır və Oracle
            // əlçatmaz olanda səhifə ümumiyyətlə açılmamalı deyil. Xəta görünsün,
            // sahə boş qalsın — mühasib əl ilə yaza bilir (əvvəlki davranış).
            netice.Xeta = $"Oracle sorğusu icra edilmədi: {ex.Message}";
            return netice;
        }

        // FIN → işçi. Boşluq/registr fərqi olmasın deyə normallaşdırılır.
        var isciler = await _uow.Repository<Isci>().Query()
            .AsNoTracking()
            .Where(x => x.FIN != null && x.FIN != "")
            .Select(x => new { x.Id, x.FIN, x.Ad, x.Soyad })
            .ToListAsync(ct);

        var finIsci = isciler
            .GroupBy(x => x.FIN.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        // Dərəcə DÖVRÜN SONUNA görə götürülür (mühasibin Excelində bir dərəcə var).
        // Dövr ortasında dəyişsə fərq görünsün deyə tətbiq olunan dərəcə hər sətirdə
        // saxlanılır və ekranda göstərilir.
        var dereceKes = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);

        // Yalnız dərəcə TAPILMAYANDA yüklənir — diaqnostika üçün (aşağı bax).
        IList<DTOs.Kredit.KreditFaizDerecesiDto>? hamiDereceler = null;

        foreach (var s in faizSetirler)
        {
            var kod     = Metn(s, "MUSTERI_KODU");
            var valyuta = Metn(s, "VALYUTA");
            if (valyuta.Length == 0) valyuta = DereceEntity.AznKodu;

            var setir = new IsciKreditFaydaDto
            {
                MusteriKodu = kod,
                ValyutaKodu = valyuta,
                FaizAdi     = Reqem(s, "FAIZ_ADI"),
                FaizVk      = Reqem(s, "FAIZ_VK"),
                IsciFaizi   = Reqem(s, "ISCI_FAIZI"),
                VkFaizi     = Reqem(s, "VK_FAIZI")
            };

            if (finXerite.TryGetValue(kod, out var bmi))
            {
                setir.Fin    = bmi.Fin;
                setir.BmiAdi = bmi.Ad;
            }

            // ── İşçi bağı (FIN) ──────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(setir.Fin))
            {
                setir.Problem = "BMI-də FIN yoxdur — işçiyə bağlana bilmir.";
            }
            else if (finIsci.TryGetValue(setir.Fin.Trim(), out var isci))
            {
                setir.IsciId      = isci.Id;
                setir.IsciAdSoyad = $"{isci.Ad} {isci.Soyad}";
            }
            else
            {
                setir.Problem = $"Bu FIN ({setir.Fin}) ilə işçi tapılmadı — " +
                                "işdən çıxmış ola bilər.";
            }

            // ── Bazar dərəcəsi ───────────────────────────────────────────
            if (!dereceKes.TryGetValue(valyuta, out var bazar))
            {
                bazar = (await _derece.QuvvededirAsync(netice.Son, valyuta))?.Derece;
                dereceKes[valyuta] = bazar;
            }
            setir.BazarDerecesi = bazar;

            if (bazar == null)
            {
                // SƏSSİZCƏ 0 SAYMIRIQ — dərəcəsiz valyutada hesabi gəlir hesablansaydı
                // 0 çıxardı və işçi vergidən qanunsuz yayınardı. Sətir görünür.
                //
                // İKİ FƏRQLİ HAL, İKİ FƏRQLİ MƏTN: «dərəcə yoxdur» ilə «dərəcə var,
                // amma bu dövrdən SONRAKI tarixdən qüvvədədir» eyni deyil. İkinci hal
                // real tələdir — mühasib dərəcəni bu gün yazır, keçmiş dövrü hesablayır
                // və «təyin edilməyib» görüb çaşır. Mətn nə etmək lazım olduğunu desin.
                hamiDereceler ??= await _derece.HamisiniGetirAsync();
                var enErken = hamiDereceler
                    .Where(x => string.Equals(x.ValyutaKodu, valyuta, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Tarix)
                    .FirstOrDefault();

                setir.Problem ??= enErken == null
                    ? $"«{valyuta}» valyutası üçün bazar dərəcəsi təyin edilməyib."
                    : $"«{valyuta}» dərəcəsi yalnız {enErken.Tarix:dd.MM.yyyy} tarixindən qüvvədədir — " +
                      $"bu dövrün sonu ({netice.Son:dd.MM.yyyy}) ondan əvvəldir. " +
                      "Dərəcə sətrinin tarixini geriyə çəkin və ya daha erkən tarixli sətir əlavə edin.";
            }
            else
            {
                setir.HesabiGelir = Math.Round(
                    Fayda(setir.FaizAdi, setir.IsciFaizi, bazar.Value) +
                    Fayda(setir.FaizVk,  setir.VkFaizi,   bazar.Value), 2);
            }

            netice.Setirler.Add(setir);
        }

        return netice;
    }

    // Müştəri kodu → (FIN, ad). Ayrı sorğudur — MƏBLƏĞ sorğusuna QARIŞDIRILMIR.
    //
    // NİYƏ AYRI: `regnom`-u məbləğ sorğusuna join edib GROUP BY-a salsaq və
    // `regnom`-da eyni koda iki sətir olsa, join `arh_dd` sətirlərini ikiqat edir
    // → faiz İKİQAT görünərdi. Ad qatındakı problem pul rəqəmini poza bilməz.
    // (18.08.2026 ölçüldü: birləşmiş sorğu bu gün 15 sətir verir, yəni dublikat
    //  yoxdur — amma məbləğin doğruluğunu bu şərtdən ASILI ETMİRİK.)
    //
    // Xəritə gəlməsə hesablama DAYANMIR: sətirlər «FIN yoxdur» kimi görünür.
    private async Task<Dictionary<string, (string Fin, string Ad)>> FinXeriteAsync(CancellationToken ct)
    {
        var bos = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);

        var sorgu = await SorguMetniAsync(SorguFin);
        if (sorgu == null) return bos;

        try
        {
            var setirler = await _oracle.SelectAsync(sorgu, 500, ct);
            return setirler
                .Select(s => new
                {
                    Kod = Metn(s, "MUSTERI_KODU"),
                    Fin = Metn(s, "FIN"),
                    Ad  = Metn(s, "MUSTERI_ADI")
                })
                .Where(x => x.Kod.Length > 0)
                .GroupBy(x => x.Kod, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (g.First().Fin, g.First().Ad),
                              StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return bos;
        }
    }
}

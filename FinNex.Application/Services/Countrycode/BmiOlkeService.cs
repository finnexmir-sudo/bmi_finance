using System.Globalization;
using FinNex.Application.DTOs.Countrycode;
using FinNex.Application.Interfaces.Countrycode;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Domain.Entities.Sorgular;
using FinNex.Domain.Interfaces;

namespace FinNex.Application.Services.Countrycode;

/// <summary>
/// Ölkə kataloqu — BMI `countrycode`-dan. `BmiValyutaService` (kurval) ilə eyni
/// quruluş: sorğu Admin → Oracle Sorğular-da saxlanılır, Oracle əlçatmaz olanda
/// sabit ehtiyat siyahıya keçilir.
/// </summary>
public class BmiOlkeService : IBmiOlkeService
{
    private readonly IUnitOfWork _uow;
    private readonly IOracleService _oracle;

    // Seed: docs/sql/olke/Olke_OracleSorgu.sql
    private const string SorguAdi = "OLKE_SIYAHISI";

    // Ehtiyat siyahı — Oracle əlçatmaz olanda işə düşür.
    //
    // Bunlar müqavilə formasında əvvəldən sabit yazılmış 5 ölkədir (BMI-dəki
    // dialoqun siyahısı) — yəni ehtiyat rejimi köhnə davranışa qayıdır, forma
    // ölkəsiz qalmır. ADLAR `countrycode.NAME` ilə hərfbəhərf eynidir, çünki
    // müqaviləyə düşən dəyər məhz addır.
    //
    // Kodlar ISO-3-dür (cədvəldəki bütün sətirlər ISO-3 formatındadır).
    // Kod yalnız Oracle dəyərini ada çevirmək üçün işlədilir — ehtiyat rejimdə
    // kod səhv olsa belə müqaviləyə yazılan ad düzgün qalır.
    private static readonly BmiOlkeDto[] Ehtiyat =
    {
        new() { Kod = "AZE", Ad = "Azərbaycan Respublikası" },
        new() { Kod = "IRN", Ad = "İran İslam Respublikası" },
        new() { Kod = "RUS", Ad = "Rusiya Respublikası" },
        new() { Kod = "TUR", Ad = "Türkiyə Respublikası" },
        new() { Kod = "GEO", Ad = "Gürcüstan Respublikası" },
    };

    public BmiOlkeService(IUnitOfWork uow, IOracleService oracle)
    {
        _uow = uow;
        _oracle = oracle;
    }

    public async Task<IList<BmiOlkeDto>> SiyahiAsync(CancellationToken ct = default)
    {
        try
        {
            var sorgu = (await _uow.Repository<OracleSorgu>()
                    .HamisiniGetirAsync(x => !x.Silinib && x.Aktiv, izlemeden: true))
                .FirstOrDefault(x => string.Equals((x.SorguAdi ?? "").Trim(), SorguAdi,
                    StringComparison.OrdinalIgnoreCase));

            if (sorgu == null || string.IsNullOrWhiteSpace(sorgu.SorguMetni))
                return Ehtiyat.ToList();

            var setirler = await _oracle.SelectAsync(sorgu.SorguMetni, 500, ct);

            var siyahi = setirler
                .Select(s => new BmiOlkeDto
                {
                    Kod = Metn(s, "CODE"),
                    Ad  = Metn(s, "NAME")
                })
                // Adsız sətir formada boş sətir kimi görünərdi — atırıq
                // (cədvəldə belə sətir var: kod IIR, adı boş).
                .Where(o => !string.IsNullOrWhiteSpace(o.Ad))
                .OrderBy(o => o.Ad, StringComparer.Create(new CultureInfo("az-Latn-AZ"), true))
                .ToList();

            return siyahi.Count > 0 ? siyahi : Ehtiyat.ToList();
        }
        catch
        {
            // Oracle əlçatmazdır — forma işləməyə davam etsin
            return Ehtiyat.ToList();
        }
    }

    public async Task<string?> AdaCevirAsync(string? kodVeyaAd, CancellationToken ct = default)
        => Ada(await SiyahiAsync(ct), kodVeyaAd);

    /// <summary>
    /// Çevirmənin YEGANƏ implementasiyası. Hazır siyahı ilə işlədiyi üçün bir
    /// səhifədə çox dəyər çevirmək lazım olanda (borcalan + bütün zaminlər)
    /// Oracle-a təkrar müraciət olunmur — çağıran siyahını bir dəfə alır.
    /// `AdaCevirAsync` da buna delegasiya edir ki, məntiq iki yerdə yazılmasın.
    /// </summary>
    public static string? Ada(IList<BmiOlkeDto> siyahi, string? kodVeyaAd)
    {
        var deyer = (kodVeyaAd ?? "").Trim();
        if (deyer.Length == 0) return null;

        // Əvvəlcə KOD kimi yoxlanılır (Oracle sahələri adətən kod saxlayır:
        // creditinfo.COUNTRYCODE / creditinfoguarantee.COUNTRYCODE = "AZE"),
        // sonra AD kimi (sorğu countrycode ilə join edilibsə hazır ad gəlir).
        var tapilan =
            siyahi.FirstOrDefault(o => string.Equals(o.Kod, deyer, StringComparison.OrdinalIgnoreCase))
            ?? siyahi.FirstOrDefault(o => string.Equals(o.Ad, deyer, StringComparison.OrdinalIgnoreCase));

        // Tapılmasa gələn dəyəri OLDUĞU KİMİ qaytarırıq. Boş qaytarsaydıq
        // Oracle-dakı məlumat səssizcə itərdi — bilinməyən kod görünən qalsın.
        return tapilan?.Ad ?? deyer;
    }

    // Sütun adı böyük/kiçik hərflə gələ bilər — müqayisə həssas deyil.
    private static string Metn(IDictionary<string, object?> s, string sutun)
    {
        var acar = s.Keys.FirstOrDefault(k => string.Equals(k, sutun, StringComparison.OrdinalIgnoreCase));
        if (acar == null || s[acar] == null) return "";
        return (Convert.ToString(s[acar], CultureInfo.InvariantCulture) ?? "").Trim();
    }
}

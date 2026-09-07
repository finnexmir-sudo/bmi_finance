using FinNex.Application.DTOs.Emeliyyat;
using FinNex.Domain.Entities.Emeliyyat;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.Emeliyyat;

/// <summary>
/// 20 000 USD AYLIQ KÖÇÜRMƏ LİMİTİ — TƏK MƏNBƏ (07.09.2026).
///
/// Qanun: «məqsədi bəyan edilməklə rezident və qeyri-rezident fiziki şəxsin
/// təqvim ayı ərzində cəmi 20 000 (iyirmi min) ABŞ dolları ekvivalentinədək
/// məbləğdə olan köçürmələri».
///
/// ── QƏRARLAR (istifadəçi, 07.09.2026) ────────────────────────────────────
/// 1. Sayılan məbləğ — <c>Mebleg</c> (müştəridən ALINAN). Rialda köçürülən
///    765 000 000 elə həmin 900 USD-dir; iki dəfə saymaq səhv olardı.
/// 2. USD ekvivalenti Oracle <c>func_get_kurval</c> ilə — əl ilə yazılmır.
/// 3. <c>Secim</c>-in HƏR ÜÇ variantı sayılır (Hesab açmadan / Hesab və
///    mədaxil / Hesabdan). Amma YALNIZ <c>Novu = "Pul"</c> — Tələbə köçürməsi
///    GƏLƏN puldur, fiziki şəxsdən çıxmır.
/// 4. Limit aşılırsa və sənəd seçilməyibsə — ƏMƏLİYYAT KEÇMİR (blok).
/// 5. Kurs alınmasa da BLOK — «Oracle işləməsə heç nə işləməz, o, ən əsasıdır».
///
/// ── NİYƏ AYRI FAYL ───────────────────────────────────────────────────────
/// `KocurmeService` 450 sətirdir və bu məntiq hüquqi tələbdir — dəyişəndə
/// harada olduğu dərhal görünsün. Sinif eynidir (`partial`), yəni servisin
/// `_uow`-una və köməkçilərinə çıxışı var.
/// </summary>
public partial class KocurmeService
{
    /// <summary>Aylıq hədd — USD. Dəyişsə TƏK BU SƏTİR dəyişir.</summary>
    public const decimal AylikLimitUsd = 20_000m;

    /// <summary>
    /// Limitə düşən köçürmə növü. Tələbə köçürməsi GƏLƏN puldur — düşmür.
    /// </summary>
    private const string LimitNovu = "Pul";

    /// <summary>
    /// Formadakı valyuta adı → BMI `kurval` kodu.
    ///
    /// ⚠️ `Kocurme.MedaxilValyuta` MƏTN saxlayır («USD»/«Avro»/«AZN») — Gedən
    /// həvalədən fərqli olaraq (orada KOD saxlanılır). Ona görə xəritə lazımdır.
    /// Formaya yeni mədaxil valyutası əlavə olunsa BURA DA əlavə edilməlidir —
    /// yoxsa kurs tapılmaz və əməliyyat bloklanar (səssiz səhv YOX, görünən blok).
    /// </summary>
    private static string? ValyutaKodu(string? adi) => (adi ?? "").Trim().ToUpperInvariant() switch
    {
        "AZN"                 => "00",
        "USD"                 => "01",
        "AVRO" or "EUR"       => "02",
        "RUBL"                => "03",
        "RIAL" or "RİAL"      => "04",
        "DIRHEM" or "DİRHƏM"  => "05",
        _                     => null
    };

    /// <summary>
    /// <paramref name="mebleg"/>-in USD ekvivalenti + işlədilmiş USD kursu.
    ///
    /// Düstur: <c>mebleg × kurs(valyuta) ÷ kurs(USD)</c>.
    /// AZN-də kurs 1, USD-də kəsr özü-özünə bölünür → məbləğin özü qalır.
    ///
    /// Kurs alınmasa <c>(null, null)</c> — çağıran tərəf BLOKLAYIR.
    /// </summary>
    private async Task<(decimal? usd, decimal? usdKursu)> UsdEkvivalentAsync(
        decimal? mebleg, string? medaxilValyuta, DateTime tarix, CancellationToken ct = default)
    {
        var m = mebleg ?? 0m;
        if (m <= 0) return (0m, null);          // məbləğ yoxdursa kursa ehtiyac yoxdur

        var kod = ValyutaKodu(medaxilValyuta);
        if (kod == null) return (null, null);   // tanınmayan valyuta — blok

        var usdKursu = await _valyuta.KursAsync("01", tarix, ct);
        if (usdKursu is not > 0) return (null, null);

        // USD-dirsə çevirmə lazım deyil — ikinci Oracle sorğusundan qaçırıq
        // və yuvarlaqlaşdırma xətası da yaranmır.
        if (kod == "01") return (decimal.Round(m, 2), usdKursu);

        var valyutaKursu = kod == "00" ? 1m : await _valyuta.KursAsync(kod, tarix, ct) ?? 0m;
        if (valyutaKursu <= 0) return (null, usdKursu);

        return (decimal.Round(m * valyutaKursu / usdKursu.Value, 2), usdKursu);
    }

    /// <summary>
    /// Bir FİN üzrə bir TƏQVİM AYININ cəmi.
    ///
    /// <paramref name="xaricId"/> — redaktədə qeydin ÖZÜNÜ cəmdən çıxarmaq
    /// üçün. Olmasa qeyd öz-özü ilə toplanar və 10 000-lik köçürməni
    /// redaktə edən operator 20 000 görərdi (məzuniyyət konfliktində
    /// `xaricId` ilə eyni qayda).
    ///
    /// Cəm YAZILMIŞ <c>UsdEkvivalent</c> sütunundan gəlir — yenidən
    /// hesablanmır. Kurs sonradan dəyişsə keçmiş ay TƏRPƏNMİR.
    /// </summary>
    public async Task<FinLimitDto> FinAyliqCemAsync(string? fin, int il, int ay, int? xaricId = null)
    {
        var temiz = FinTemizle(fin);
        var netice = new FinLimitDto { Fin = temiz, Il = il, Ay = ay, Limit = AylikLimitUsd };

        if (temiz.Length == 0) return netice;   // FİN yoxdursa cəm də yoxdur

        // Ay sərhədi: [ayın 1-i 00:00, növbəti ayın 1-i 00:00)
        // `.Month == ay` YAZMIRIQ — o, indeksdən istifadə etmir və hər sətri
        // funksiyadan keçirir. Aralıq müqayisəsi indeksə (Novu+Fin+Tarix) düşür.
        var bas = new DateTime(il, ay, 1);
        var son = bas.AddMonths(1);

        var sorgu = _uow.Repository<Kocurme>().Query()
            .Where(x => !x.Silinib
                     && x.Novu == LimitNovu
                     && x.GonderenFin == temiz
                     && x.Tarix >= bas && x.Tarix < son);

        if (xaricId.HasValue)
            sorgu = sorgu.Where(x => x.Id != xaricId.Value);

        // Tək sorğu ilə həm cəm, həm say — iki dəfə bazaya getmirik.
        var xulase = await sorgu.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cem = g.Sum(x => x.UsdEkvivalent ?? 0m),
                Say = g.Count()
            })
            .FirstOrDefaultAsync();

        netice.CemiUsd = xulase?.Cem ?? 0m;
        netice.Sayi    = xulase?.Say ?? 0;
        return netice;
    }

    /// <summary>
    /// Yeni/redaktə olunan əməliyyat üçün tam yoxlama — ekran da, yadda
    /// saxlama da EYNİ bu metodu çağırır ki, göstərilən ilə tətbiq olunan
    /// fərqlənməsin.
    /// </summary>
    public async Task<FinLimitYoxlamaDto> LimitYoxlaAsync(
        string novu, string? fin, decimal? mebleg, string? medaxilValyuta,
        DateTime? tarix, int? xaricId = null, CancellationToken ct = default)
    {
        var t = tarix ?? DateTime.Now;
        var n = new FinLimitYoxlamaDto
        {
            Movcud = new FinLimitDto { Fin = FinTemizle(fin), Il = t.Year, Ay = t.Month, Limit = AylikLimitUsd }
        };

        // Tələbə köçürməsi limitə düşmür — gələn puldur.
        if (!string.Equals(novu, LimitNovu, StringComparison.OrdinalIgnoreCase))
        {
            n.Mesaj = "Bu köçürmə növü aylıq limitə daxil deyil.";
            return n;
        }

        var (usd, _) = await UsdEkvivalentAsync(mebleg, medaxilValyuta, t, ct);
        if (usd == null)
        {
            n.KursAlinmadi = true;
            n.Mesaj = "Valyuta kursu alınmadı (Oracle). Limit hesablana bilmir — əməliyyat qeydə alınmır.";
            return n;
        }

        n.YeniUsd = usd.Value;
        n.Movcud  = await FinAyliqCemAsync(fin, t.Year, t.Month, xaricId);
        n.Movcud.Limit = AylikLimitUsd;

        // Vəziyyət mətni ÜÇ HALDIR. Birincisi qəsdən ayrıdır: operator FİN-i
        // yazan kimi (məbləğ hələ boşdur) «bu əməliyyatla … olur» yazsaq,
        // mövcud olmayan məbləğdən danışmış olarıq. O anda ona lazım olan
        // yeganə şey — bu şəxsə bu ay nə qədər yer qalıb; limit onsuz da
        // aşılıbsa formanı ümumiyyətlə doldurmasın (istifadəçi qərarı 07.09.2026).
        var ayAdi = AyAdi(t.Month);
        var cem   = n.Movcud.CemiUsd;
        var say   = n.Movcud.Sayi;

        if (n.YeniUsd <= 0)
        {
            n.Mesaj = say == 0
                ? $"Bu FİN üzrə {ayAdi} ayında köçürmə yoxdur. " +
                  $"Aylıq hədd {AylikLimitUsd:N0} USD — tam açıqdır."
                : n.Movcud.Asilib
                    ? $"Bu FİN üzrə {ayAdi} ayında {say} köçürmə, cəmi {cem:N2} USD keçib — " +
                      $"{AylikLimitUsd:N0} USD həddi ARTIQ AŞILIB. Yeni əməliyyat üçün əsas sənəd tələb olunacaq."
                    : $"Bu FİN üzrə {ayAdi} ayında {say} köçürmə, cəmi {cem:N2} USD keçib. " +
                      $"Həddə {n.Movcud.Qaliq:N2} USD qalır (hədd {AylikLimitUsd:N0} USD).";
            return n;
        }

        n.Mesaj = n.SenedTelebOlunur
            ? $"Bu FİN üzrə {ayAdi} ayında {cem:N2} USD keçib. " +
              $"Bu əməliyyatla {n.SonraCem:N2} USD olur — {AylikLimitUsd:N0} USD həddi aşılır. " +
              "Əsas sənəd seçilməlidir."
            : $"Bu FİN üzrə {ayAdi} ayında {cem:N2} USD keçib. " +
              $"Bu əməliyyatla {n.SonraCem:N2} USD olur — həddə {(n.Movcud.Limit - n.SonraCem):N2} USD qalır " +
              $"(hədd {AylikLimitUsd:N0} USD).";

        return n;
    }

    /// <summary>
    /// FİN normallaşdırması — boşluqsuz, BÖYÜK hərflə.
    ///
    /// ⚠️ Yazılan da, axtarılan da EYNİ bu metoddan keçməlidir. Biri
    /// normallaşdırıb o biri normallaşdırmasa «5ab2cd1» ilə «5AB2CD1»
    /// iki AYRI şəxs kimi sayılar və limit səssizcə iki dəfə açılar.
    /// </summary>
    public static string FinTemizle(string? fin)
        => new string((fin ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static string AyAdi(int ay) => ay switch
    {
        1 => "yanvar", 2 => "fevral",  3 => "mart",    4 => "aprel",
        5 => "may",    6 => "iyun",    7 => "iyul",    8 => "avqust",
        9 => "sentyabr", 10 => "oktyabr", 11 => "noyabr", 12 => "dekabr",
        _ => ay.ToString()
    };
}

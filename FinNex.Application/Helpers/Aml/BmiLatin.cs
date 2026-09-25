using System.Text;
using FinNex.Application.DTOs.Aml;
using FinNex.Application.DTOs.Risk;

namespace FinNex.Application.Helpers.Aml;

/// <summary>
/// AML → «Məlumat Bazası» axtarışı üçün iki iş görür:
///   1. Azəri mətnini BMI-nin <c>odb.func_utf8_to_latin</c> funksiyası ilə
///      EYNİ qaydada ASCII-yə endirir;
///   2. Exceldən oxunan şəxs siyahısını Oracle sorğusuna yapışdırılan
///      <c>{SIYAHI}</c> bloku kimi qurur.
///
/// ── NİYƏ AYRICA METOD (Sadeles İŞLƏMİR) ──────────────────────────────────
/// Layihədə `DashboardController.Sadeles` var və o, <b>`ə → e`</b> edir —
/// Excel BAŞLIQLARINI tanımaq üçün yazılıb, orada düzgündür.
/// BMI-nin funksiyası isə <b>`Ə → A`</b> edir. 22.09.2026-da BMI-də ölçülüb:
/// <code>
///   odb.func_utf8_to_latin('ƏLİYEVA ÜLVİYYƏ ŞÖVQİ İSMAYIL ÇƏMƏNZƏMİNLİ')
///        →  ALIYEVA ULVIYYA SOVQI ISMAYIL CAMANZAMINLI
/// </code>
/// Sorğularda müqayisə məhz həmin funksiyanın ÇIXIŞI ilə gedir
/// (<c>func_utf8_to_latin(upper(sütun)) like '%'||upper(y.a_s_a)||'%'</c>),
/// ona görə biz də eyni xəritəni işlətməliyik. `Sadeles` işlədilsəydi
/// «MƏLAHƏT» → `MELAHET` olardı, bazada isə `MALAHAT`-dır — heç vaxt
/// tapılmazdı və <b>heç bir xəta verməzdi</b>.
///
/// ── TƏHLÜKƏSİZLİK ────────────────────────────────────────────────────────
/// Bu mətn istifadəçinin yüklədiyi Excel faylından gəlir və birbaşa SQL
/// mətninə yapışdırılır (`IOracleService` bind parametri qəbul etmir).
/// Ona görə <see cref="Tehlukesiz"/> ağ siyahı tətbiq edir: yalnız hərf,
/// rəqəm, boşluq və bir neçə durğu işarəsi keçir; apostrof ikiləşdirilir;
/// idarəedici simvollar atılır; uzunluq kəsilir. Oracle-a onsuz da yalnız
/// SELECT gedir (`OracleService.YalnizSelect`), bu isə ikinci qatdır.
/// </summary>
public static class BmiLatin
{
    /// <summary>Boş xana üçün sentinel — `null` YOX. Oracle-da `''` elə `null`-dır
    /// və `union all` qollarında tip qarışıqlığı yaradır; `'~'` isə heç bir real
    /// dəyərə bərabər deyil, yəni şərt sakitcə söndürülür.</summary>
    public const string Bos = "~";

    /// <summary>
    /// «Yalnız FİN/VÖEN» rejimində `a_s_a` xanasına yazılan dəyər.
    /// `like '%~~AD_YOXDUR~~%'` heç vaxt tutmur → ad şərti söndürülür,
    /// <b>SQL mətninə isə toxunulmur</b> (11 sorğu hər iki rejimdə eynidir).
    ///
    /// ⚠️ Eyni sentinel adı BOŞ qalan sətirlərdə də işlənir. Boş ad
    /// göndərmək OLMAZ: `like '%%'` <b>BÜTÜN sətirləri</b> qaytarar.
    /// </summary>
    public const string AdYoxdur = "~~AD_YOXDUR~~";

    /// <summary>Bir sorğuya yapışdırılan maksimum şəxs sayı.</summary>
    public const int MaxSetir = 5000;

    private const int MaxUzunluq = 200;

    /// <summary>
    /// Azəri hərflərini BMI-nin <c>func_utf8_to_latin</c> xəritəsi ilə ASCII-yə
    /// endirir və böyük hərfə çevirir. Xəritə 22.09.2026-da BMI-də ölçülüb.
    /// </summary>
    public static string Cevir(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";

        var sb = new StringBuilder(s.Length);
        foreach (var c in s.Trim())
        {
            switch (c)
            {
                case 'Ə': case 'ə': sb.Append('A'); break;   // ⚠️ E DEYİL
                case 'İ': case 'ı': sb.Append('I'); break;
                case 'Ü': case 'ü': sb.Append('U'); break;
                case 'Ö': case 'ö': sb.Append('O'); break;
                case 'Ş': case 'ş': sb.Append('S'); break;
                case 'Ç': case 'ç': sb.Append('C'); break;
                case 'Ğ': case 'ğ': sb.Append('G'); break;
                // `İ`-nin ToLower qalıq nöqtəsi — görünmür, amma müqayisəni sındırır.
                case '̇': break;
                default: sb.Append(char.ToUpperInvariant(c)); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// <see cref="Cevir"/> + SQL mətninə yapışdırmaq üçün təmizləmə.
    /// Ağ siyahı: `A–Z`, `0–9`, boşluq, `. - / & '`. Apostrof ikiləşdirilir.
    /// Ardıcıl boşluqlar birləşdirilir (Excel xanalarında tez-tez olur).
    /// </summary>
    public static string Tehlukesiz(string? s)
    {
        var xam = Cevir(s);
        if (xam.Length == 0) return "";

        var sb = new StringBuilder(xam.Length + 8);
        var sonBosluq = false;
        foreach (var c in xam)
        {
            if (sb.Length >= MaxUzunluq) break;

            if (c == '\'')            { sb.Append("''"); sonBosluq = false; continue; }
            if (char.IsWhiteSpace(c)) { if (!sonBosluq && sb.Length > 0) { sb.Append(' '); sonBosluq = true; } continue; }

            var keciril = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                          || c == '.' || c == '-' || c == '/' || c == '&';
            if (keciril) { sb.Append(c); sonBosluq = false; }
            // Qalan hər şey (idarəedici simvollar, `;`, `%`, `_`, bucaq mötərizə…) ATILIR.
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Exceldən oxunan siyahını Oracle sorğusundakı <c>{SIYAHI}</c> blokuna çevirir:
    /// <code>
    ///   select 'HUSEYNOV SAMIR MIRHUSEYN' a_s_a, '1EZVKMS' fin, '~' voen, '~' tel from dual
    ///   union all
    ///   select 'QARADAG TIKINTI MMC', '~', '1234567890', '~' from dual
    /// </code>
    /// Sütun adları BMI-nin <c>odb.aml_yoxlama</c> cədvəli ilə EYNİDİR
    /// (<c>a_s_a / fin / voen / tel</c>) — ona görə 11 sorğunun `where` hissəsi
    /// BMI-dən olduğu kimi köçüb və dəyişdirilməyib.
    ///
    /// ⚠️ Yalnız BİRİNCİ qolda alias yazılır — Oracle `union all`-da sütun
    /// adlarını birinci qoldan götürür; hamısına yazmaq da olardı, amma mətni
    /// uzadır (sorğu mətninin uzunluğu real məhdudiyyətdir).
    /// </summary>
    /// <param name="setirler">Excel sətirləri (boş sətirlər oxuma mərhələsində atılıb).</param>
    /// <param name="rejim">
    /// <see cref="AxtarisRejimi.FinVoen"/> — ad şərti söndürülür (`a_s_a` =
    /// <see cref="AdYoxdur"/>). <see cref="AxtarisRejimi.Ad"/> — FİN/VÖEN
    /// şərti söndürülür (hər ikisi = <see cref="Bos"/>), Excel-də dolu olsalar
    /// belə. <see cref="AxtarisRejimi.HerIkisi"/> — dəyişiklik yoxdur (default).
    /// SQL mətni heç bir halda dəyişmir, yalnız göndərilən dəyər dəyişir.
    /// </param>
    public static string SiyahiQur(IReadOnlyList<AxtarisSetriDto> setirler, AxtarisRejimi rejim)
    {
        var sb = new StringBuilder(1024);
        var say = 0;

        foreach (var s in setirler)
        {
            if (say >= MaxSetir) break;

            var ad   = rejim == AxtarisRejimi.FinVoen ? AdYoxdur : Tehlukesiz(s.AdSoyadAta);
            var fin  = rejim == AxtarisRejimi.Ad ? Bos : Tehlukesiz(s.Fin);
            var voen = rejim == AxtarisRejimi.Ad ? Bos : Tehlukesiz(s.Voen);
            var tel  = Bos;   // Excel şablonunda telefon sütunu YOXDUR (22.09.2026).
                              // Şərtlər sorğularda saxlanılıb — sütun əlavə olunsa
                              // burada bir sətir dəyişmək kifayətdir.

            // ⚠️ BOŞ AD GÖNDƏRMƏ — `like '%%'` BÜTÜN sətirləri qaytarar.
            if (ad.Length == 0) ad = AdYoxdur;
            if (fin.Length  == 0) fin  = Bos;
            if (voen.Length == 0) voen = Bos;

            // Nə ad, nə FİN, nə VÖEN varsa sətrin axtaracağı heç nə yoxdur.
            if (ad == AdYoxdur && fin == Bos && voen == Bos) continue;

            if (say > 0) sb.Append("\n       union all\n       ");

            if (say == 0)
                sb.Append($"select '{ad}' a_s_a, '{fin}' fin, '{voen}' voen, '{tel}' tel from dual");
            else
                sb.Append($"select '{ad}', '{fin}', '{voen}', '{tel}' from dual");

            say++;
        }

        return sb.ToString();
    }
}

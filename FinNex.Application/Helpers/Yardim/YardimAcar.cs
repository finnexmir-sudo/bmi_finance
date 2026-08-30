using System.Text;

namespace FinNex.Application.Helpers.Yardim;

/// <summary>
/// Səhifə yardımının marşrut açarını quran TƏK MƏNBƏ (27.08.2026).
///
/// NİYƏ AYRICA HELPER: açarı iki yerdə hesablasaq (bir yerdə «?» düyməsi,
/// bir yerdə admin redaktoru) biri kiçik hərfə salar, o biri salmaz — və
/// yardım heç vaxt tapılmaz. Heç bir xəta çıxmaz, sadəcə panel boş gələr.
/// Açar lazım olan HƏR yer bu metodu çağırsın.
/// </summary>
public static class YardimAcar
{
    /// <summary>Area olmayan səhifələr üçün yer tutucu (məs. `_/home/index`).</summary>
    public const string AreaYoxdur = "_";

    /// <summary>
    /// Marşrutdan açar qurur: <c>{area}/{controller}/{action}</c>, kiçik hərflə.
    /// Boş gələn hissələr üçün ağıllı defolt verilir ki, açar heç vaxt
    /// yarımçıq («user//») olmasın.
    /// </summary>
    public static string Qur(string? area, string? controller, string? action)
    {
        var a = Tozla(area);
        var c = Tozla(controller);
        var e = Tozla(action);

        if (string.IsNullOrEmpty(a)) a = AreaYoxdur;
        if (string.IsNullOrEmpty(c)) c = "home";
        if (string.IsNullOrEmpty(e)) e = "index";

        return $"{a}/{c}/{e}";
    }

    private static string Tozla(string? s)
        => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

    /// <summary>
    /// Başlıqdan insan üçün qısa ünvan (slug) düzəldir:
    /// «Məzuniyyət müraciəti» → <c>mezuniyyet-muracieti</c>.
    ///
    /// Azərbaycan hərfləri latın qarşılığına çevrilir — yoxsa URL-də
    /// faizli kodlaşma çıxır (`%C9%99`) və link çatda oxunmaz olur.
    /// </summary>
    public static string Slugla(string? metn)
    {
        if (string.IsNullOrWhiteSpace(metn)) return "";

        var sb = new StringBuilder(metn.Length);
        foreach (var ch in metn.Trim().ToLowerInvariant())
        {
            var d = ch switch
            {
                'ə' => "e", 'ı' => "i", 'ö' => "o", 'ü' => "u",
                'ğ' => "g", 'ş' => "s", 'ç' => "c",
                _ => null
            };
            if (d != null) { sb.Append(d); continue; }

            if (char.IsLetterOrDigit(ch) && ch < 128) sb.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_') sb.Append('-');
            // qalan simvollar (nöqtə, vergül, mötərizə…) sadəcə atılır
        }

        // Ard-arda gələn tireləri birləşdir, kənarlardakıları at
        var xam = sb.ToString();
        while (xam.Contains("--")) xam = xam.Replace("--", "-");
        return xam.Trim('-');
    }
}

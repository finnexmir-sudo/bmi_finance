using System.Text;

namespace FinNex.Application.Helpers.Yardim;

/// <summary>
/// Təlimat mətnini göstərmək üçün HTML-ə çevirir (31.08.2026).
///
/// NİYƏ: admin HTML yazmaq istəmir — «sadə mətn kimi yazım» (istifadəçi tələbi).
/// İndi redaktorda adi mətn yazılır, HTML-i bu helper qurur.
///
/// ⚠️ MƏTN OLDUĞU KİMİ SAXLANILIR, çevrilmiş HTML BAZAYA YAZILMIR.
/// Çevirmə yalnız GÖSTƏRMƏ anındadır — yoxsa admin növbəti dəfə redaktəyə
/// girəndə öz yazdığını yox, maşının qurduğu HTML-i görərdi və mətn
/// tədricən oxunmaz hala düşərdi.
///
/// KÖHNƏ QEYDLƏR POZULMUR: mətn onsuz da HTML-dirsə (ilk paketdəki 9 səhifə
/// belədir) olduğu kimi qaytarılır — bax <see cref="HtmlGorunur"/>.
/// </summary>
public static class YardimMetn
{
    /// <summary>
    /// Mətn artıq HTML-dirmi? Sadə əlamət: blok teqlərindən biri var.
    /// Qəsdən DAR saxlanılıb — adi mətndə «&lt;p&gt;» yazılması ehtimalı yoxdur,
    /// amma «5 &lt; 7» kimi ifadə səhvən HTML sayılmasın.
    /// </summary>
    public static bool HtmlGorunur(string? metn)
    {
        if (string.IsNullOrWhiteSpace(metn)) return false;
        var m = metn.ToLowerInvariant();
        return m.Contains("<p>") || m.Contains("<h3") || m.Contains("<ul>")
            || m.Contains("<ol>") || m.Contains("<table") || m.Contains("<div");
    }

    /// <summary>
    /// Sadə mətni HTML-ə çevirir. Qaydalar QƏSDƏN AZDIR — çox qayda yadda
    /// qalmır və istifadə olunmur:
    ///
    ///   # Başlıq            → başlıq
    ///   - sətir  (və ya •)  → nöqtəli siyahı
    ///   1. sətir            → nömrəli siyahı
    ///   *qalın*             → qalın
    ///   boş sətir           → yeni abzas
    ///
    /// Bütün mətn əvvəlcə HTML-dən təmizlənir (escape) — istifadəçinin yazdığı
    /// «&lt;» işarəsi teqə çevrilmir və səhifəni poza bilmir.
    /// </summary>
    public static string Formatla(string? metn)
    {
        if (string.IsNullOrWhiteSpace(metn)) return "";
        if (HtmlGorunur(metn)) return metn;      // köhnə HTML qeydlər toxunulmur

        var setirler = metn.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var sb = new StringBuilder();

        bool ulAcıq = false, olAcıq = false;
        var abzas = new List<string>();

        void AbzasiBagla()
        {
            if (abzas.Count == 0) return;
            sb.Append("<p>").Append(string.Join("<br>", abzas)).Append("</p>");
            abzas.Clear();
        }
        void SiyahiniBagla()
        {
            if (ulAcıq) { sb.Append("</ul>"); ulAcıq = false; }
            if (olAcıq) { sb.Append("</ol>"); olAcıq = false; }
        }

        foreach (var xam in setirler)
        {
            var s = xam.Trim();

            if (s.Length == 0) { AbzasiBagla(); SiyahiniBagla(); continue; }

            // Başlıq: «# Mətn» (və ya «## Mətn» — ikisi də eyni səviyyədir,
            // panel dardır, iki səviyyə başlıq lazım deyil)
            if (s.StartsWith("#"))
            {
                AbzasiBagla(); SiyahiniBagla();
                var b = s.TrimStart('#').Trim();
                sb.Append("<h3>").Append(Qalin(Tozla(b))).Append("</h3>");
                continue;
            }

            // Nöqtəli siyahı: «- », «• », «* » (sonuncu yalnız boşluqla —
            // *qalın* ilə qarışmasın)
            if (s.StartsWith("- ") || s.StartsWith("• ") || s.StartsWith("* "))
            {
                AbzasiBagla();
                if (olAcıq) { sb.Append("</ol>"); olAcıq = false; }
                if (!ulAcıq) { sb.Append("<ul>"); ulAcıq = true; }
                sb.Append("<li>").Append(Qalin(Tozla(s.Substring(2).Trim()))).Append("</li>");
                continue;
            }

            // Nömrəli siyahı: «1. », «2) »
            var nomreli = NomreliMi(s, out var qaliq);
            if (nomreli)
            {
                AbzasiBagla();
                if (ulAcıq) { sb.Append("</ul>"); ulAcıq = false; }
                if (!olAcıq) { sb.Append("<ol>"); olAcıq = true; }
                sb.Append("<li>").Append(Qalin(Tozla(qaliq))).Append("</li>");
                continue;
            }

            SiyahiniBagla();
            abzas.Add(Qalin(Tozla(s)));
        }

        AbzasiBagla();
        SiyahiniBagla();
        return sb.ToString();
    }

    // «1. mətn» / «2) mətn» formasını tanıyır
    private static bool NomreliMi(string s, out string qaliq)
    {
        qaliq = "";
        int i = 0;
        while (i < s.Length && char.IsDigit(s[i])) i++;
        if (i == 0 || i >= s.Length) return false;
        if (s[i] != '.' && s[i] != ')') return false;
        if (i + 1 >= s.Length || s[i + 1] != ' ') return false;
        qaliq = s.Substring(i + 2).Trim();
        return true;
    }

    // HTML-dən təmizləmə — istifadəçinin yazdığı simvol teqə çevrilməsin
    private static string Tozla(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>
    /// *qalın* → &lt;b&gt;qalın&lt;/b&gt;. Cüt ulduzlar axtarılır; tək qalan
    /// ulduz olduğu kimi qalır (riyazi vurma işarəsi pozulmasın).
    /// DİQQƏT: `Tozla`-dan SONRA çağırılır, ona görə buradakı teqlər təhlükəsizdir.
    /// </summary>
    private static string Qalin(string s)
    {
        var sb = new StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '*')
            {
                var son = s.IndexOf('*', i + 1);
                // Boş «**» qalın sayılmır
                if (son > i + 1)
                {
                    sb.Append("<b>").Append(s, i + 1, son - i - 1).Append("</b>");
                    i = son + 1;
                    continue;
                }
            }
            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }
}

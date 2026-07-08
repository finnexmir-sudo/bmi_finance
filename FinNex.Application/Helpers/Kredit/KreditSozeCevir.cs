using System.Globalization;

namespace FinNex.Application.Helpers.Kredit;

/// <summary>
/// BMI-dəki Aletler.cs-in söz-çevirmə köməkçilərinin portu (Azərbaycan dili):
///   - yaziyaCevir           → MebleghSoze        (məbləğ + manat + qəpik)
///   - yaziyaCevirqepiksiz   → MebleghSozeQepiksiz (yalnız tam hissə, sözlə)
///   - TarixiSozeCevir       → TarixiSoze          (gün + ay + il + suffiks)
///   - AyYaziIle             → MuddetSoze          (ay sayı sözlə)
///   - FaizYaziIle           → FaizSoze            (faiz sözlə)
/// Məntiq BMI ilə eyni saxlanılıb ki, müqavilə mətnləri dəyişməsin.
/// </summary>
public static class KreditSozeCevir
{
    private static readonly string[] Birler =
        { "", "bir", "iki", "üç", "dörd", "beş", "altı", "yeddi", "səkkiz", "doqquz" };
    private static readonly string[] Onlar =
        { "", "on", "iyirmi", "otuz", "qırx", "əlli", "altmış", "yetmiş", "səksən", "doxsan" };
    private static readonly string[] Binler =
        { "katrilyon", "trilyon", "milyard", "milyon", "min", "" };
    // BMI ilə eyni — ay adları böyük hərflə başlayır
    private static readonly string[] Aylar =
        { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
          "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };

    // Aletler.yaziyaCevir — məbləğ + "manat" + qəpik
    public static string MebleghSoze(decimal tutar)
    {
        var sTutar = tutar.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
        var lira = sTutar.Substring(0, sTutar.IndexOf(','));
        var kurus = sTutar.Substring(sTutar.IndexOf(',') + 1, 2);
        var yazi = "";

        const int grupSayisi = 6;
        lira = lira.PadLeft(grupSayisi * 3, '0');

        for (var i = 0; i < grupSayisi * 3; i += 3)
        {
            var grupDegeri = "";

            if (lira[i] != '0')
                grupDegeri += Birler[lira[i] - '0'] + " yüz";

            var onlarStr = Onlar[lira[i + 1] - '0'];
            if (!string.IsNullOrEmpty(onlarStr))
                grupDegeri += " " + onlarStr;

            var birlerStr = Birler[lira[i + 2] - '0'];
            if (!string.IsNullOrEmpty(birlerStr))
                grupDegeri += " " + birlerStr;

            if (!string.IsNullOrWhiteSpace(grupDegeri))
            {
                grupDegeri += " " + Binler[i / 3];
                if (grupDegeri.Trim() == "bir min")
                    grupDegeri = "min";
                yazi += grupDegeri + " ";
            }
        }

        if (!string.IsNullOrWhiteSpace(yazi))
            yazi += "manat ";

        var yaziUzunlugu = yazi.Length;

        if (kurus[0] != '0')
            yazi += Onlar[kurus[0] - '0'] + " ";
        if (kurus[1] != '0')
            yazi += Birler[kurus[1] - '0'] + " ";

        yazi += yazi.Length > yaziUzunlugu ? "qəpik." : "sıfır qəpik.";

        return yazi.Trim();
    }

    // Aletler.yaziyaCevirqepiksiz — yalnız tam hissə (girov dəyəri üçün)
    public static string MebleghSozeQepiksiz(decimal tutar)
    {
        var sTutar = tutar.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');
        var lira = sTutar.Substring(0, sTutar.IndexOf(','));
        var yazi = "";

        const int grupSayisi = 6;
        lira = lira.PadLeft(grupSayisi * 3, '0');

        for (var i = 0; i < grupSayisi * 3; i += 3)
        {
            var grupDegeri = "";
            int yuzler = lira[i] - '0', onlarBas = lira[i + 1] - '0', birlerBas = lira[i + 2] - '0';

            if (yuzler != 0)
                grupDegeri += Birler[yuzler] + " yüz";
            if (onlarBas != 0)
                grupDegeri += (grupDegeri != "" ? " " : "") + Onlar[onlarBas];
            if (birlerBas != 0)
                grupDegeri += (grupDegeri != "" ? " " : "") + Birler[birlerBas];

            if (grupDegeri != "")
            {
                if (grupDegeri == "bir min")
                    grupDegeri = "min";
                else
                    grupDegeri += " " + Binler[i / 3];

                if (yazi != "") yazi += " ";
                yazi += grupDegeri.Trim();
            }
        }

        return yazi.Trim();
    }

    // Aletler.TarixiSozeCevir — "07 iyul 2026-cı"
    public static string TarixiSoze(DateTime tarix)
    {
        var gun = tarix.ToString("dd", CultureInfo.InvariantCulture);
        var ay = Aylar[tarix.Month];
        var il = tarix.Year.ToString(CultureInfo.InvariantCulture);
        return $"{gun} {ay} {il}{IlSuffiksi(tarix.Year)}";
    }

    // Aletler.TarixinSonReqemineSufiksElaveEt
    private static string IlSuffiksi(int il)
    {
        var son = il % 10;
        if (son == 0)
        {
            return (il % 100) switch
            {
                10 => "-cu",
                20 => "-ci",
                30 => "-cı",
                40 => "-cı",
                50 => "-ci",
                60 => "-cı",
                70 => "-ci",
                80 => "-ci",
                90 => "-cu",
                _ => ""
            };
        }
        return son switch
        {
            1 => "-ci",
            2 => "-ci",
            3 => "-cü",
            4 => "-cü",
            5 => "-ci",
            6 => "-cı",
            7 => "-ci",
            8 => "-ci",
            9 => "-cu",
            _ => ""
        };
    }

    private static readonly CultureInfo Az = GetAz();
    private static CultureInfo GetAz()
    {
        try { return CultureInfo.GetCultureInfo("az-Latn-AZ"); }
        catch { try { return CultureInfo.GetCultureInfo("az"); } catch { return CultureInfo.InvariantCulture; } }
    }
    private static readonly HashSet<string> AdSuffiks =
        new(StringComparer.OrdinalIgnoreCase) { "oğlu", "oglu", "qızı", "qizi", "kızı" };

    /// <summary>
    /// ALL-CAPS adı başlıq registrinə çevirir (Heydərova Aidə Ərzuman qızı).
    /// "oğlu/qızı" suffiksləri kiçik qalır. Azərbaycan hərfləri (İ/ı/ə) düzgün.
    /// </summary>
    public static string BaslikRegistri(string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return ad ?? "";
        var sozler = ad.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < sozler.Length; i++)
        {
            var w = sozler[i].ToLower(Az);
            if (AdSuffiks.Contains(w)) { sozler[i] = w; continue; }
            sozler[i] = char.ToUpper(w[0], Az) + (w.Length > 1 ? w.Substring(1) : "");
        }
        return string.Join(" ", sozler);
    }

    // Aletler.AyYaziIle — müddət (ay) sözlə
    public static string MuddetSoze(int aySayi)
    {
        if (aySayi == 0) return "sıfır";
        var birlik = aySayi % 10;
        var onluq = aySayi / 10;
        var netice = Onlar[onluq % 10];
        if (birlik > 0)
            netice += " " + Birler[birlik];
        return netice.Trim();
    }

    // Aletler.FaizYaziIle — faiz sözlə (məs. 12,5 → "on iki tam onda beş")
    public static string FaizSoze(decimal faiz)
    {
        var tamHisse = (int)Math.Floor(faiz);
        var ondalik = (int)((faiz - tamHisse) * 10);
        var netice = "";

        if (tamHisse > 0)
        {
            var onluq = tamHisse / 10;
            var birlik = tamHisse % 10;
            netice += Onlar[onluq % 10];
            if (birlik > 0)
                netice += " " + Birler[birlik];
        }

        if (ondalik > 0)
        {
            if (!string.IsNullOrEmpty(netice))
                netice += " tam";
            netice += " onda " + Birler[ondalik];
        }

        return netice.Trim();
    }
}

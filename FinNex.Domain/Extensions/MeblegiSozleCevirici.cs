namespace FinNex.Domain.Extensions;

public static class MeblegiSozleCevirici
{
    private static readonly string[] Tekler = { "", "bir", "iki", "üç", "dörd", "beş", "altı", "yeddi", "səkkiz", "doqquz" };
    private static readonly string[] Onlar = { "", "on", "iyirmi", "otuz", "qırx", "əlli", "altmış", "yetmiş", "səksən", "doxsan" };
    private static readonly string[] Mertebeler = { "", "min", "milyon", "milyard" };

    public static string AzcaSozle(this decimal mebleg)
    {
        if (mebleg == 0) return "Sıfır manat 00 qəpik";

        mebleg = Math.Abs(mebleg);

        long tamHisse = (long)Math.Truncate(mebleg);
        int qepikHissesi = (int)Math.Round((mebleg - tamHisse) * 100);

        string manatMetni = ManatHissesiniCevir(tamHisse).Trim();

        if (!string.IsNullOrEmpty(manatMetni))
        {
            // Baş hərfi böyüdürük
            manatMetni = char.ToUpper(manatMetni[0]) + manatMetni.Substring(1);
        }
        else
        {
            manatMetni = "Sıfır";
        }

        return $"{manatMetni} manat {qepikHissesi:00} qəpik";
    }

    private static string ManatHissesiniCevir(long eded)
    {
        if (eded == 0) return "";
        string netice = "";
        int mertebeIndeksi = 0;

        while (eded > 0)
        {
            int ucReqem = (int)(eded % 1000);
            if (ucReqem > 0)
            {
                string hisse = YuzlukCevirici(ucReqem);

                // Sənin istədiyin dəyişiklik: Həmişə rəqəmi və mərtəbəni yan-yana yazır
                // Beləliklə "1000" -> "bir min" olacaq
                netice = hisse + " " + Mertebeler[mertebeIndeksi] + " " + netice;
            }
            eded /= 1000;
            mertebeIndeksi++;
        }
        return netice;
    }

    private static string YuzlukCevirici(int eded)
    {
        string metn = "";
        int yuzluk = eded / 100;
        int onluq = (eded % 100) / 10;
        int teklik = eded % 10;

        if (yuzluk > 0)
        {
            // Yüzlükdə də "bir yüz" yazılsın deyə:
            metn += Tekler[yuzluk] + " yüz ";
        }
        if (onluq > 0) metn += Onlar[onluq] + " ";
        if (teklik > 0) metn += Tekler[teklik];

        return metn.Trim();
    }
}
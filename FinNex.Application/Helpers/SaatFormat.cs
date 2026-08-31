using System.Globalization;

namespace FinNex.Application.Helpers;

/// <summary>
/// Onluqlu saatı adi saat/dəqiqə kimi yazır (31.08.2026).
///
/// NİYƏ: icazə sayğacı onluqlu saatla işləyir (3,2 saat). İstifadəçilər bunu
/// «3 saat 20 dəqiqə» kimi oxuyurdular — halbuki 3,2 saat = 3 saat 12 dəqiqədir.
/// Ekranda çaşdırırdı, ona görə göstərmə qatında saat/dəqiqəyə çevrilir.
///
/// ⚠️ HESABLAMAYA TOXUNMUR — sayğac, limit yoxlaması və balans hər yerdə
/// onluqlu `double` qalır. Bu yalnız MƏTNDİR.
/// </summary>
public static class SaatFormat
{
    /// <summary>
    /// 3,2 → «3 saat 12 dəq»; 32,8 → «32 saat 48 dəq»; 36 → «36 saat»;
    /// 0,5 → «30 dəq»; 0 → «0 saat».
    /// Mənfi dəyər öz işarəsi ilə qaytarılır («-1 saat 30 dəq»).
    /// </summary>
    public static string Qisa(double saat)
    {
        bool menfi = saat < 0;
        var mutleq = Math.Abs(saat);

        // Əvvəlcə TAM DƏQİQƏYƏ yuvarlaqlaşdırırıq — sonra bölürük.
        // Əks halda 0,999 saat «0 saat 60 dəq» kimi çıxardı.
        var cemiDeq = (int)Math.Round(mutleq * 60.0, MidpointRounding.AwayFromZero);

        var s = cemiDeq / 60;
        var d = cemiDeq % 60;

        string metn;
        if (s == 0 && d == 0) metn = "0 saat";
        else if (d == 0)      metn = $"{s} saat";
        else if (s == 0)      metn = $"{d} dəq";
        else                  metn = $"{s} saat {d} dəq";

        return menfi ? "-" + metn : metn;
    }

    /// <summary>
    /// «3 saat 12 dəq / 36 saat» kimi cüt göstərmə üçün limit tərəfi.
    /// Limit adətən tam ədəddir (36), ona görə artıq sıfır yazılmır.
    /// </summary>
    public static string Limit(double saat)
        => saat.ToString("0.##", CultureInfo.CurrentCulture) + " saat";
}

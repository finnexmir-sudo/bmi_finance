namespace FinNex.Application.Helpers.Emeliyyat;

/// <summary>
/// Pul köçürməsində «nə qədər pul GEDİR» və «hansı valyuta ilə» — TƏK MƏNBƏ.
///
/// Formada iki ayrı məbləğ var və qarışdırılması səssiz səhvə aparır:
///   • <c>Mebleg</c>   — müştəridən ALINAN məbləğ (məs. 900 USD)
///   • köçürülən       — qarşı tərəfə GEDƏN məbləğ (Rial/Rubl-da <c>Mebleg × IranRial</c>)
///
/// 18.08.2026-ya qədər Word ərizəsində «Məbləğ rəqəmlə» xanasına <c>Mebleg</c>
/// yazılırdı — sənədin ƏSAS rəqəmi səhv idi (900 ≠ 765 000 000). Düzəldiləndən
/// sonra eyni qayda Gedən həvalə jurnalına da lazım oldu; iki nüsxə saxlamamaq
/// üçün buraya çıxarıldı.
///
/// QAYDA: köçürülən məbləği/valyuta adını hesablayan yeni kod bu helper-i çağırsın.
/// Rial/Rubl siyahısı dəyişsə tək bir yerdə dəyişir.
/// </summary>
public static class KocurmeValyuta
{
    /// <summary>Köçürülən valyuta kursla çevrilirmi (Rial/Rubl)?</summary>
    public static bool Konversiya(string? kocurulenValyuta)
        => kocurulenValyuta is "Rial" or "Rubl";

    /// <summary>
    /// Qarşı tərəfə GEDƏN məbləğ. Rial/Rubl-da <c>mebleg × kurs</c>, digərlərində
    /// <c>mebleg</c>-in özü.
    /// </summary>
    public static decimal KocurulenMebleg(string? kocurulenValyuta, decimal? mebleg, decimal? iranRial)
    {
        var m = mebleg ?? 0m;
        return Konversiya(kocurulenValyuta) ? m * (iranRial ?? 0m) : m;
    }

    /// <summary>
    /// Valyutanın sənəddə yazılan tam adı. Köhnə BMI formasındakı yazılışla eynidir
    /// — dəyişdirməyin, ərizə mətni ona uyğun ölçülüb.
    /// </summary>
    public static string Adi(string? valyuta) => valyuta switch
    {
        "Rial" => "İran Rialı",
        "Rubl" => "Rubl",
        "USD"  => "ABŞ dolları",
        "Avro" => "Avro",
        "AZN"  => "AZN",
        _      => valyuta ?? ""
    };
}

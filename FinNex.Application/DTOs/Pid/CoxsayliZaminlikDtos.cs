namespace FinNex.Application.DTOs.Pid;

// "Çoxsaylı Zaminlik" səhifəsi — 1-dən çox kreditə zamin duran şəxslər.
// Sorğu Admin → Oracle Sorğular-da saxlanılır, adına görə tapılır ("çoxsaylı zaminlik").
public class CoxsayliZaminlikViewDto
{
    public bool   SorguTapildi { get; set; }
    public string? Xeta        { get; set; }
    public List<CoxsayliZaminlikSetirDto> Setirler { get; set; } = new();

    // Fərqli zamin sayı (FİN üzrə)
    public int ZaminSayi => Setirler
        .Select(x => string.IsNullOrWhiteSpace(x.ZaminFin) ? x.ZaminAd : x.ZaminFin)
        .Distinct()
        .Count();
}

// Bir sətir = bir zaminin durduğu bir kredit
public class CoxsayliZaminlikSetirDto
{
    public string?  ZaminFin              { get; set; }
    public string   ZaminAd               { get; set; } = "(naməlum)";
    public int      ZaminDurduguKreditSayi { get; set; }
    public string?  Borcalan              { get; set; }
    public string?  Region                { get; set; }
    public string?  KreditHesabi          { get; set; }
    public string?  Ks                    { get; set; }
    public decimal? KreditinMeblegi       { get; set; }
    public decimal? Qaliq                 { get; set; }
    public decimal? VkQaliq               { get; set; }
}

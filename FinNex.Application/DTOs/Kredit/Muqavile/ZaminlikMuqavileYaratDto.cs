namespace FinNex.Application.DTOs.Kredit.Muqavile;

/// <summary>
/// Zaminlik krediti — hazırlama formasının POST məlumatı.
/// İpoteka/girov yoxdur; təminat yalnız zaminlərdir (neçə zamin olsa o qədər
/// zaminlik müqaviləsi yaradılır). Kredit məlumatı Oracle-dan oxunur.
/// </summary>
public class ZaminlikMuqavileYaratDto
{
    // Kredit identifikatoru (Oracle-dan yenidən oxumaq üçün)
    public string HesabNo { get; set; } = "";
    public string Ks { get; set; } = "";
    public DateTime KreditTarixi { get; set; }

    // Müqavilə tarixi (kreditin verilmə tarixi)
    public DateTime MuqavileTarixi { get; set; } = DateTime.Today;

    // Forma parametrləri
    public string? Teyinat { get; set; }
    public string? BorcalanOlke { get; set; }

    // Zaminlər — Oracle SELECT-dən avtomatik yüklənir, əl ilə də əlavə/redaktə oluna bilər
    public List<ZaminDaxilDto> Zaminler { get; set; } = new();
}

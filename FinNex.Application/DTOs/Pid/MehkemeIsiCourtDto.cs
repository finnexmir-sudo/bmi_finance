namespace FinNex.Application.DTOs.Pid;

// Məhkəmə İşləri səhifəsi — vahid MehkemeIsi dosyesinin məhkəmə görünüşü
public class MehkemeIsiCourtListDto
{
    public int Id { get; set; }
    public int? Sira { get; set; }
    public string? Status { get; set; }
    public string BorcluAd { get; set; } = "";
    public string? KreditNovu { get; set; }
    public string? KreditHesabi { get; set; }
    public string? Subkod { get; set; }
    public DateTime? MehkemeyeVerilmeTarixi { get; set; }
    public string? MehkemeSenedi { get; set; }
    public DateTime? QetnameTarixi { get; set; }
    public List<MehkemeCedvelIclasDto> Iclaslar { get; set; } = new();
}

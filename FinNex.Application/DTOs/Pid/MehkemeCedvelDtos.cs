namespace FinNex.Application.DTOs.Pid;

public class MehkemeCedvelListDto
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
    public string? Qeyd { get; set; }
    public List<MehkemeCedvelIclasDto> Iclaslar { get; set; } = new();
}

public class MehkemeCedvelIclasDto
{
    public int Id { get; set; }
    public DateTime? Tarix { get; set; }
    public string? Saat { get; set; }
    public string? Netice { get; set; }
}

// Excel-dən parse olunmuş iş (controller → servis)
public class MehkemeCedvelImportDto
{
    public int? Sira { get; set; }
    public string? Status { get; set; }
    public string BorcluAd { get; set; } = "";
    public string? KreditNovu { get; set; }
    public DateTime? MehkemeyeVerilmeTarixi { get; set; }
    public string? MehkemeSenedi { get; set; }
    public List<MehkemeCedvelIclasImportDto> Iclaslar { get; set; } = new();
}

public class MehkemeCedvelIclasImportDto
{
    public DateTime? Tarix { get; set; }
    public string? Saat { get; set; }
}

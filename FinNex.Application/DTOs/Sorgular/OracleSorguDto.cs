namespace FinNex.Application.DTOs.Sorgular;

public class OracleSorguDto : BaseDto
{
    public string SorguAdi { get; set; } = "";
    public string? Mahiyyet { get; set; }
    public string SorguMetni { get; set; } = "";
    public bool Aktiv { get; set; }
    public bool Kataloq { get; set; }
    public int DepartamentId { get; set; }
    public string DepartamentAd { get; set; } = "";
}

public class OracleSorguCreateDto
{
    public string SorguAdi { get; set; } = "";
    public string? Mahiyyet { get; set; }
    public string SorguMetni { get; set; } = "";
    public bool Aktiv { get; set; } = true;
    public bool Kataloq { get; set; } = false;
    public int DepartamentId { get; set; }
}

public class OracleSorguUpdateDto
{
    public int Id { get; set; }
    public string SorguAdi { get; set; } = "";
    public string? Mahiyyet { get; set; }
    public string SorguMetni { get; set; } = "";
    public bool Aktiv { get; set; }
    public bool Kataloq { get; set; }
    public int DepartamentId { get; set; }
}

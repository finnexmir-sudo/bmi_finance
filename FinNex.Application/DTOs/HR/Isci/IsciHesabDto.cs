namespace FinNex.Application.DTOs.HR.Isci;

public class IsciHesabDto
{
    public int    IsciId      { get; set; }
    public string TamAd       { get; set; } = null!;
    public string? Departament { get; set; }
    public string? BankHesabNo { get; set; }
}

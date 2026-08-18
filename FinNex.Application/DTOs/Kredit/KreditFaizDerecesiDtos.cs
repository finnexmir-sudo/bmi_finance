namespace FinNex.Application.DTOs.Kredit;

/// <summary>Oxumaq üçün — siyahı və axtarış nəticəsi.</summary>
public class KreditFaizDerecesiDto
{
    public int      Id          { get; set; }
    public DateTime Tarix       { get; set; }
    public string   ValyutaKodu { get; set; } = "";
    /// <summary>BMI `kurval`-dan gələn ad («ABŞ DOLLARI»); AZN üçün «AZN».</summary>
    public string?  ValyutaAdi  { get; set; }
    public decimal  Derece      { get; set; }
    public string?  Qeyd        { get; set; }

    public string Goster => $"{Derece:0.####}% — {(string.IsNullOrWhiteSpace(ValyutaAdi) ? ValyutaKodu : ValyutaAdi)} ({Tarix:dd.MM.yyyy}-dan)";
}

/// <summary>Yazmaq üçün. Yeniləmədə <see cref="Id"/> dolu olur.</summary>
public class KreditFaizDerecesiCreateDto
{
    public int      Id          { get; set; }
    public DateTime Tarix       { get; set; }
    public string   ValyutaKodu { get; set; } = "00";
    public decimal  Derece      { get; set; }
    public string?  Qeyd        { get; set; }
}

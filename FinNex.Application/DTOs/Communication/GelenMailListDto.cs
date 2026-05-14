namespace FinNex.Application.DTOs.Communication;

public class GelenMailListDto
{
    public int Id { get; set; }
    public string KimdenAd { get; set; } = "";
    public string KimdenEmail { get; set; } = "";
    public string Movzu { get; set; } = "";
    public DateTime AlinmaTarixi { get; set; }
    public bool Oxunub { get; set; }
    public string? AIXulase { get; set; }
    public int QosmaCount { get; set; }
    public string? TapalanIsciAdSoyad { get; set; }
    public bool CavabVerildi { get; set; }
    public int? SenedId { get; set; }
}

namespace FinNex.Application.DTOs.Communication;

public class AIMailTahlilNetic
{
    public string Xulase { get; set; } = "";
    public DateTime? DedlaynTarix { get; set; }
    public string? DedlaynNov { get; set; }   // Gorus / Hesabat / Muqavile / SonTarix / Diger
    public string? DedlaynQeyd { get; set; }
}

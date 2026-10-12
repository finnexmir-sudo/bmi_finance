namespace FinNex.Application.DTOs.Communication;

public class GedenMailListDto
{
    public int Id { get; set; }
    public string KimeAd { get; set; } = "";
    public string KimeEmail { get; set; } = "";
    public string KimeToplu { get; set; } = "";
    public string Movzu { get; set; } = "";
    public DateTime GonderilmeTarixi { get; set; }
    public int QosmaCount { get; set; }
}

namespace FinNex.Application.DTOs.Communication;

public class GelenMailTapshiriqDto
{
    public int MailId { get; set; }
    public string Movzu { get; set; } = "";
    public string KimdenAd { get; set; } = "";
    public string KimdenEmail { get; set; } = "";
    public DateTime AlinmaTarixi { get; set; }
    public DateTime TapalanTarix { get; set; }
    public string? Qeyd { get; set; }
    public bool IcraOlundu { get; set; }
    public DateTime? IcraOlunduTarix { get; set; }
    public bool ImtinaEtdi { get; set; }
    public string? ImtinaSebebi { get; set; }
}

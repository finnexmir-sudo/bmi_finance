namespace FinNex.Application.DTOs.Communication;

public class MailCavabDto
{
    public int MailId { get; set; }
    public string MessageId { get; set; } = "";
    public string KimeEmail { get; set; } = "";
    public string KimeAd { get; set; } = "";
    public string Movzu { get; set; } = "";

    // Orijinal mail (sadəcə göstərmək üçün)
    public string AsilMailMovzu { get; set; } = "";
    public DateTime AsilMailTarix { get; set; }
    public string? AsilMailMetinDuz { get; set; }
    public string? AIXulase { get; set; }

    // İstifadəçi yazır / AI hazırlayır
    public string CavabMetni { get; set; } = "";
}

namespace FinNex.Application.DTOs.Communication;

public class GedenMailDetailDto
{
    public int Id { get; set; }
    public string MessageId { get; set; } = "";
    public string KimeAd { get; set; } = "";
    public string KimeEmail { get; set; } = "";
    public string KimeToplu { get; set; } = "";
    public string Movzu { get; set; } = "";
    public string MetinHtml { get; set; } = "";
    public string MetinDuz { get; set; } = "";
    public DateTime GonderilmeTarixi { get; set; }
    public List<GedenMailQosmaDto> Qosmalar { get; set; } = new();
}

public class GedenMailQosmaDto
{
    public int Id { get; set; }
    public string FaylAdi { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long OlcuBayt { get; set; }
    public string? CixarilmisMetin { get; set; }
}

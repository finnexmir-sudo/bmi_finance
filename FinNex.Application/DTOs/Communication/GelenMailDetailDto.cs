namespace FinNex.Application.DTOs.Communication;

public class GelenMailDetailDto
{
    public int Id { get; set; }
    public string MessageId { get; set; } = "";
    public string KimdenAd { get; set; } = "";
    public string KimdenEmail { get; set; } = "";
    public string Movzu { get; set; } = "";
    public string MetinHtml { get; set; } = "";
    public string MetinDuz { get; set; } = "";
    public DateTime AlinmaTarixi { get; set; }
    public bool Oxunub { get; set; }
    public string? AIXulase { get; set; }
    public DateTime? AITahlilTarixi { get; set; }
    public bool CavabVerildi { get; set; }
    public int? SenedId { get; set; }
    public int? TapalanIsciId { get; set; }
    public string? TapalanIsciAdSoyad { get; set; }
    public string? TapalanQeyd { get; set; }
    public DateTime? TapalanTarix { get; set; }
    public List<GelenMailQosmaDto> Qosmalar { get; set; } = new();
}

public class GelenMailQosmaDto
{
    public int Id { get; set; }
    public string FaylAdi { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long OlcuBayt { get; set; }
    public string? CixarilmisMetin { get; set; }
}

namespace FinNex.Application.DTOs.Communication.IsciMail;

public class IsciMailDetayDto
{
    public int Id { get; set; }
    public int GonderenIsciId { get; set; }
    public string GonderenAdSoyad { get; set; } = "";
    public string Movzu { get; set; } = "";
    public string Metin { get; set; } = "";
    public string MetinHtml { get; set; } = "";
    public DateTime? GondermeTarixi { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
    public bool Taslagdirmi { get; set; }
    public List<IsciMailAliciDto> Alicilar { get; set; } = new();
}

public class IsciMailAliciDto
{
    public int IsciId { get; set; }
    public string AdSoyad { get; set; } = "";
    public string AliciNovu { get; set; } = "To";
    public bool Oxunub { get; set; }
    public DateTime? OxunmaTarixi { get; set; }
}

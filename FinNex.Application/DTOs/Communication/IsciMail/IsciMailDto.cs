namespace FinNex.Application.DTOs.Communication.IsciMail;

public class IsciMailDto
{
    public int Id { get; set; }
    public int GonderenIsciId { get; set; }
    public string GonderenAdSoyad { get; set; } = "";
    public string Movzu { get; set; } = "";
    public string MetinKisar { get; set; } = "";
    public DateTime? GondermeTarixi { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
    public bool Oxunub { get; set; }
    public bool Taslagdirmi { get; set; }
    public int AliciSayi { get; set; }
}

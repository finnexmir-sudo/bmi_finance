namespace FinNex.Application.DTOs.Communication;

public class MailTapshirilanDto
{
    public int MailId { get; set; }
    public string Movzu { get; set; } = "";
    public string KimdenAd { get; set; } = "";
    public string KimdenEmail { get; set; } = "";
    public DateTime AlinmaTarixi { get; set; }
    public DateTime TapalanTarix { get; set; }
    public string? Qeyd { get; set; }
    public List<MailTapshirilanIsciDto> Isciler { get; set; } = new();

    public int TamamlananSayi => Isciler.Count(x => x.IcraOlundu);
    public bool HamisiTamamladi => Isciler.Count > 0 && Isciler.All(x => x.IcraOlundu);
}

public class MailTapshirilanIsciDto
{
    public int IsciId { get; set; }
    public string AdSoyad { get; set; } = "";
    public DateTime TapalanTarix { get; set; }
    public string? Qeyd { get; set; }
    public bool IcraOlundu { get; set; }
    public DateTime? IcraOlunduTarix { get; set; }
}

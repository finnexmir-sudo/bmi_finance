namespace FinNex.Application.DTOs.Communication.IsciMail;

public class IsciMailCreateDto
{
    public List<int> AliciIsciIds { get; set; } = new();
    public List<int> CcIsciIds { get; set; } = new();
    public string Movzu { get; set; } = "";
    public string Metin { get; set; } = "";
    public string MetinHtml { get; set; } = "";
    public bool Taslagdir { get; set; } = false;
}

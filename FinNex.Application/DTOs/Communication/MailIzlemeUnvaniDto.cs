namespace FinNex.Application.DTOs.Communication;

public class MailIzlemeUnvaniDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string? Ad { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
}

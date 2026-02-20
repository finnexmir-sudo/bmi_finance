namespace FinNex.Application.DTOs.SenedDovriyyesi;

public class AuditLogDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = null!;
    public int? SenedId { get; set; }
    public string? Ip { get; set; }
    public string? DetailsJson { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
}

namespace FinNex.Domain.Entities.AI;

public class HRSohbetMesaj
{
    public int Id { get; set; }

    public int SohbetId { get; set; }
    public virtual HRSohbet Sohbet { get; set; } = null!;

    /// <summary>"user" ya "assistant"</summary>
    public string Rol { get; set; } = "user";

    public string Metn { get; set; } = "";

    public DateTime Tarix { get; set; } = DateTime.Now;
}

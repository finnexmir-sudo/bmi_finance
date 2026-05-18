namespace FinNex.Domain.Entities.AI;

public class HRSohbet : BaseEntity
{
    public int AppUserId { get; set; }
    public virtual AppUser AppUser { get; set; } = null!;

    public DateTime BaslanmaTarixi { get; set; } = DateTime.Now;

    public virtual ICollection<HRSohbetMesaj> Mesajlar { get; set; } = new List<HRSohbetMesaj>();
}

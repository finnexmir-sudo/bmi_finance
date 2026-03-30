// FinNex.Domain.Entities.Communication/EvezediciTesdiq.cs
using FinNex.Domain.Entities.HR;


// FinNex.Domain.Entities.Communication/EvezediciTesdiq.cs
namespace FinNex.Domain.Entities.Communication
{
    public class EvezediciTesdiq : BaseEntity
    {
        public int MezuniyyetId { get; set; }
        public Mezuniyyet Mezuniyyet { get; set; } = null!;

        public int EvezediciIsciId { get; set; }
        public Isci EvezediciIsci { get; set; } = null!;

        public EvezediciTesdiqStatus Status { get; set; } = EvezediciTesdiqStatus.Gozlemede;
        public string? Qeyd { get; set; }
        public DateTime? CavabTarixi { get; set; }
    }
}
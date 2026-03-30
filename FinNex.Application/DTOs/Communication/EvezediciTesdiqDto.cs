using FinNex.Domain.Entities.Communication;

namespace FinNex.Application.DTOs.Communication
{
    public class EvezediciTesdiqDto
    {
        public int Id { get; set; }
        public int MezuniyyetId { get; set; }
        public string MezuniyyetIsciAd { get; set; } = "";
        public string MezuniyyetNov { get; set; } = "";
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunu { get; set; }
        public EvezediciTesdiqStatus Status { get; set; }
        public string? Qeyd { get; set; }
        public DateTime? CavabTarixi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
}

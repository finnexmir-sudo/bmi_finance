// FinNex.Domain.Entities.Communication/Bildiris.cs
using FinNex.Domain.Entities.HR;

// FinNex.Domain.Entities.Communication/Bildiris.cs
namespace FinNex.Domain.Entities.Communication
{
    public class Bildiris : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public BildirisNovu Nov { get; set; }
        public string Bashliq { get; set; } = "";
        public string Metn { get; set; } = "";

        public bool Oxunub { get; set; } = false;
        public DateTime? OxunmaTarixi { get; set; }

        public int? MezuniyyetId { get; set; }
        public int? IcazeId { get; set; }
        public int? MesajId { get; set; }
        public string? RedirectUrl { get; set; }
    }
}

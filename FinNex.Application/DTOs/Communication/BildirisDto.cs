using FinNex.Domain.Entities.Communication;

namespace FinNex.Application.DTOs.Communication
{
    public class BildirisDto
    {
        public int Id { get; set; }
        public BildirisNovu Nov { get; set; }
        public string Bashliq { get; set; } = "";
        public string Metn { get; set; } = "";
        public bool Oxunub { get; set; }
        public DateTime? OxunmaTarixi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
        public string? RedirectUrl { get; set; }
        public int? MezuniyyetId { get; set; }
        public int? IcazeId { get; set; }
        public int? MesajId { get; set; }
    }
}

using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.DTOs.SenedDovriyyesi.Sened
{
    public class SenedDetailDto
    {
        public int Id { get; set; }
        public string? SenedNomresi { get; set; }
        public int DepartmentId { get; set; }
        public int SenedNovuId { get; set; }
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public SenedStatusu Status { get; set; }
        public string Sobe { get; set; } = null!;
        public string SenedNovu { get; set; } = null!;
        public DateTime SenedTarixi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
        public int? YaradanIcraciId { get; set; }
        public DateTime? YenilenmeTarixi { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<SenedFaylDto> Fayllar { get; set; } = new();
        public List<AuditLogDto> AuditLogs { get; set; } = new();


    }
}

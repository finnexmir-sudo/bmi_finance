using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.DTOs.SenedDovriyyesi.Sened
{
    public class SenedListDto
    {
        public int Id { get; set; }
        public string? SenedNomresi { get; set; }
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public SenedStatusu Status { get; set; }
        public string Sobe { get; set; } = null!;
        public string? IsciAdi { get; set; }
        public string SenedNovu { get; set; } = null!;
        public int FaylSayi { get; set; }
        public DateTime SenedTarixi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
        public SenedKateqoriyasi Kateqoriya { get; set; } = SenedKateqoriyasi.Umumi;
    }
}

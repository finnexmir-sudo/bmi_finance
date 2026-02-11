using FinNex.Application.DTOs.SenedDovriyyesi.Fayl;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Application.DTOs.SenedDovriyyesi.Sened
{
    public class SenedCreateDto
    {
        public int SobeId { get; set; }
        public int SenedNovuId { get; set; }
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public List<int> TagIds { get; set; } = new();
    }
    public class SenedListDto
    {
        public int Id { get; set; }
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public SenedStatusu Status { get; set; }
        public string Sobe { get; set; } = null!;
        public string SenedNovu { get; set; } = null!;
        public int FaylSayi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
    public class SenedDetailDto
    {
        public int Id { get; set; }
        public int SobeId { get; set; }
        public int SenedNovuId { get; set; }
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public SenedStatusu Status { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<SenedFaylDto> Fayllar { get; set; } = new();
    }
}

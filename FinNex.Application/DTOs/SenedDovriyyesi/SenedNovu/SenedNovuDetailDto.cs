namespace FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu
{
    public class SenedNovuDetailDto
    {
        public int Id { get; set; }

        public string Kod { get; set; } = null!;
        public string Ad { get; set; } = null!;

        public int DepartmentId { get; set; }
        public string DepartmentAd { get; set; } = null!;

        public bool Aktiv { get; set; }

        public DateTime YaradilmaTarixi { get; set; }
        public DateTime? YenilenmeTarixi { get; set; }
    }
}

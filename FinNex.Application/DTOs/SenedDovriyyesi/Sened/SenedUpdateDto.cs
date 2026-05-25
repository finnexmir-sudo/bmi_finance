namespace FinNex.Application.DTOs.SenedDovriyyesi.Sened
{
    public class SenedUpdateDto
    {
        public int Id { get; set; }
        public int SobeId { get; set; }
        public int SenedNovuId { get; set; }
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public List<int> TagIds { get; set; } = new();
        public FinNex.Domain.Entities.SenedDovriyyesi.SenedKateqoriyasi Kateqoriya { get; set; } = FinNex.Domain.Entities.SenedDovriyyesi.SenedKateqoriyasi.Umumi;
    }

    public class SenedRestoreDto
    {
        public int Id { get; set; }
    }
}

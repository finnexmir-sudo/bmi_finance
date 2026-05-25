namespace FinNex.Application.DTOs.SenedDovriyyesi.Sened
{
    public class SenedCreateDto
    {
        public int SobeId { get; set; }
        public int SenedNovuId { get; set; }
        public string Basliq { get; set; } = null!;
        public string AcarSoz { get; set; } = null!;
        public List<int> TagIds { get; set; } = new();

        // Sənədin tarixi — istifadəçi tərəfindən daxil edilir (default: bu gün)
        public DateTime SenedTarixi { get; set; } = DateTime.Now.Date;
        public FinNex.Domain.Entities.SenedDovriyyesi.SenedKateqoriyasi Kateqoriya { get; set; } = FinNex.Domain.Entities.SenedDovriyyesi.SenedKateqoriyasi.Umumi;
    }
}

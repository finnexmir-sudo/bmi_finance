namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class Sened : BaseEntity
    {
        public int SobeId { get; set; }
        public Sobe Sobe { get; set; } = null!;

        public int SenedNovuId { get; set; }
        public SenedNovu SenedNovu { get; set; } = null!;

        public string Basliq { get; set; } = null!;

        // AÇAR SÖZ (axtarış üçün)
        public string AcarSoz { get; set; } = null!; // FIN:... / VOEN:... / MUQ:...

        public SenedStatusu Status { get; set; } = SenedStatusu.Yeni;

        public ICollection<SenedFayl> Fayllar { get; set; } = new List<SenedFayl>();
    }
}

namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class SenedSablon : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }

        public int SenedNovuId { get; set; }
        public SenedNovu SenedNovu { get; set; } = null!;

        public string FaylYolu { get; set; } = null!;
        public string FaylAdi { get; set; } = null!;

        public bool Aktiv { get; set; } = true;
    }
}

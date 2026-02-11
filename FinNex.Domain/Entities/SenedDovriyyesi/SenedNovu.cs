namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class SenedNovu : BaseEntity
    {
        public int SobeId { get; set; }
        public Sobe Sobe { get; set; } = null!;

        public string Kod { get; set; } = null!;   // məsələn: MUQAVILE, ERIZE
        public string Ad { get; set; } = null!;
        public bool Aktiv { get; set; } = true;
    }
}

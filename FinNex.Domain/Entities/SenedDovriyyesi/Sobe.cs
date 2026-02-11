namespace FinNex.Domain.Entities.SenedDovriyyesi
{

    public class Sobe : BaseEntity
    {
        public string Kod { get; set; } = null!;   // məsələn: KREDIT, KASSA
        public string Ad { get; set; } = null!;
        public bool Aktiv { get; set; } = true;

        public ICollection<Sened> Senedler { get; set; } = new List<Sened>();
        public ICollection<SenedNovu> SenedNovleri { get; set; } = new List<SenedNovu>();
    }
}

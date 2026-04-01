namespace FinNex.Domain.Entities.HR
{
    public class MezuniyyetBalans : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public MezuniyyetNovu Nov { get; set; } = MezuniyyetNovu.Illik;
        public int ToplamGun { get; set; }
        public int IstifadeOlunanGun { get; set; }
        public int QaliqGun => ToplamGun - IstifadeOlunanGun;
    }
}

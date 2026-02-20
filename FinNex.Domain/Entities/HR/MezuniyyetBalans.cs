namespace FinNex.Domain.Entities.HR
{
    public class MezuniyyetBalans : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; } // Məsələn: 2024
        public int ToplamGun { get; set; } // İllik qanuni haqq (məs: 21)
        public int IstifadeOlunanGun { get; set; } // İstifade etdiyi
        public int QaliqGun => ToplamGun - IstifadeOlunanGun; // Hesablanan sahə
    }
}

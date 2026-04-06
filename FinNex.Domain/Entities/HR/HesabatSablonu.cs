using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class HesabatSablonu : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }

        public HesabatTezlik Tezlik { get; set; }
        public HesabatKateqoriya Kateqoriya { get; set; }
        public HesabatPrioritet Prioritet { get; set; } = HesabatPrioritet.Orta;

        // Deadline — gün/saat
        public int SonTarixGunu { get; set; } // Aylıq: ayın neçənci günü, Həftəlik: həftənin günü (1-7), Günlük: 0
        public TimeSpan SonTarixSaati { get; set; } = new TimeSpan(17, 0, 0); // Default: 17:00

        // Məsul şəxs
        public int MesulIsciId { get; set; }
        public Isci MesulIsci { get; set; } = null!;

        // Departament
        public int DepartamentId { get; set; }
        public Departament Departament { get; set; } = null!;

        public bool Aktivdir { get; set; } = true;

        // Navigation
        public ICollection<HesabatTapshiriq> Tapshiriqlar { get; set; } = new List<HesabatTapshiriq>();
    }

    public enum HesabatTezlik
    {
        Gunluk = 1,
        Heftelik = 2,
        Aylik = 3,
        Rubluk = 4,
        Illik = 5
    }

    public enum HesabatKateqoriya
    {
        Vergi = 1,
        Maas = 2,
        Bank = 3,
        Kassa = 4,
        Dovlet = 5,
        Daxili = 6
    }

    public enum HesabatPrioritet
    {
        Asagi = 1,
        Orta = 2,
        Yuksek = 3
    }
}

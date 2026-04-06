using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class HesabatSablonu : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public string? Tesvir { get; set; }

        public HesabatTezlik Tezlik { get; set; }

        // Dinamik kateqoriya (FK)
        public int KateqoriyaId { get; set; }
        public HesabatKateqoriyasi Kateqoriya { get; set; } = null!;

        public HesabatPrioritet Prioritet { get; set; } = HesabatPrioritet.Orta;

        // Deadline — gün/saat
        public int SonTarixGunu { get; set; }
        public TimeSpan SonTarixSaati { get; set; } = new TimeSpan(17, 0, 0);

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

    public enum HesabatPrioritet
    {
        Asagi = 1,
        Orta = 2,
        Yuksek = 3
    }
}

using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class Isci : BaseEntity
    {
        // ===== Şəxsi məlumatlar =====
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string? AtaAdi { get; set; }
        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;
        public DateTime DogumTarixi { get; set; }
        public Cins Cins { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Unvan { get; set; }

        // ===== İş məlumatları =====
        public int DepartamentId { get; set; }
        public Departament Departament { get; set; } = null!;

        public int VezifeId { get; set; }
        public Vezife Vezife { get; set; } = null!;

        public DateTime IsheBaslamaTarixi { get; set; }
        public DateTime? IshtenCixmaTarixi { get; set; }
        public IsciStatus Status { get; set; } = IsciStatus.Aktiv;

        // ===== Əlaqələr =====
        public int? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        // Maliyyə və Maaş əlaqələri
        public IsciMaliye? Maliye { get; set; }
        public MezuniyyetBalans? mezuniyyetBalans { get; set; }
        
        public ICollection<Maas> Maaslar { get; set; } = new List<Maas>();
        public ICollection<IsciMaasTarixcesi> MaasTarixçesi { get; set; } = new List<IsciMaasTarixcesi>();

        public string TamAd => $"{Ad} {Soyad} {AtaAdi}";
    }
}
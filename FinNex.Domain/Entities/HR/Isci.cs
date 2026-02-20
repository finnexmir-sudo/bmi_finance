using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR
{
    public class Isci : BaseEntity
    {
        // ===== Şəxsi məlumatlar =====
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string AtaAdi { get; set; } = null!;
        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;
        public DateTime DogumTarixi { get; set; }
        public Cins Cins { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Unvan { get; set; }

        // ===== İş məlumatları =====
        public int SobeId { get; set; }
        public Department Sobe { get; set; } = null!;

        public int VezifeId { get; set; }
        public Vezife Vezife { get; set; } = null!;

        public DateTime IsheBaslamaTarixi { get; set; }
        public DateTime? IshtenCixmaTarixi { get; set; }
        public IsciStatus Status { get; set; } = IsciStatus.Aktiv;

        // ===== Əlaqələr =====
        public int? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        // Maliyyə və Maaş əlaqələri
        public IsciMaliye Maliye { get; set; } = null!;
        public ICollection<Maas> Maaslar { get; set; } = new List<Maas>();
        public ICollection<IsciMaasTarixcesi> MaasTarixçesi { get; set; } = new List<IsciMaasTarixcesi>();

        public string TamAd => $"{Ad} {Soyad} {AtaAdi}";
    }

    public class IsciMaliye : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public decimal CariMaas { get; set; } // Müqavilə üzrə cari ştat maaşı
        public int MezuniyyetQaliqGunu { get; set; } = 21;
        public string? BankHesabNo { get; set; } // IBAN
        public string? SosialSigortaNo { get; set; } // SSN
    }
}
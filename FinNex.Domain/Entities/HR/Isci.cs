namespace FinNex.Domain.Entities.HR
{
    public class Isci : BaseEntity
    {
        // Şəxsi məlumatlar
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

        // İşçi statusu
        public DateTime IsheQebulTarixi { get; set; }
        public DateTime? IsdenAyrilmaTarixi { get; set; }
        public IsciStatus Status { get; set; } = IsciStatus.Aktiv;

        // Sistem əlaqəsi
        public int? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        // Əlaqələr
        public IsciMaliye? Maliye { get; set; }
        public MezuniyyetBalans? MezuniyyetBalans { get; set; }

        public ICollection<Maas> Maaslar { get; set; } = new List<Maas>();
        public ICollection<IsciMaasTarixcesi> MaasTarixcesi { get; set; } = new List<IsciMaasTarixcesi>();
        public ICollection<IsciTeyinat> IsciTeyinatlari { get; set; } = new List<IsciTeyinat>();

        public string TamAd => $"{Ad} {Soyad} {AtaAdi}";
    }
}
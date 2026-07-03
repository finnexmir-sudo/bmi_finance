using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciDetailDto
    {
        public int Id { get; set; }

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

        // Məzuniyyət üçün şəxsi faktlar (M.117)
        public int UsaqSayi { get; set; }
        public bool EngelliUsaqVar { get; set; }
        public bool TekValideyn { get; set; }

        public DateTime IsheQebulTarixi { get; set; }
        public DateTime? IsdenAyrilmaTarixi { get; set; }

        public IsciStatus Status { get; set; }

        // IsciTeyinat-dan (aktiv)
        public string? SobeAdi { get; set; }
        public string? VezifeAdi { get; set; }
        public int? AktivDepartamentId { get; set; }
        public int? AktivVezifeId { get; set; }

        // IsciMaliye-dan
        public decimal CariMaas { get; set; }
        public string? BankHesabNo { get; set; }
        public string? SosialSigortaNo { get; set; }

        public bool LoginVar { get; set; }

        public string TamAd => $"{Ad} {Soyad} {AtaAdi}".Trim();
    }
}

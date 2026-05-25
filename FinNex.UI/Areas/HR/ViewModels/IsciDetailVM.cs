using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.IsciTeyinat;
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class IsciDetailVM
    {
        public int Id { get; set; }
        public string TamAd { get; set; } = null!;
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string? AtaAdi { get; set; }
        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;
        public DateTime DogumTarixi { get; set; }
        public string CinsAd { get; set; } = null!;
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public string? Unvan { get; set; }
        public DateTime IsheQebulTarixi { get; set; }
        public DateTime? IsdenAyrilmaTarixi { get; set; }
        public IsciStatus Status { get; set; }
        public bool LoginVar { get; set; }
        public string? IstifadeciAd { get; set; }

        // Aktiv teyinat məlumatları
        public string? CariDepartament { get; set; }
        public string? CariVezife { get; set; }
        public decimal CariMaas { get; set; }

        // Bank hesabı (IsciMaliye.BankHesabNo)
        public string? Iban { get; set; }

        // Tarixçə
        public IList<IsciTeyinatDto> TeyinatTarixcesi { get; set; } = new List<IsciTeyinatDto>();
        public IList<IsciMaasTarixcesiDto> MaasTarixcesi { get; set; } = new List<IsciMaasTarixcesiDto>();
    }
}

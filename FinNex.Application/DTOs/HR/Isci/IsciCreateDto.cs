using FinNex.Domain.Entities.HR;
using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.HR.Isci
{
    // List üçün (Siyahıda görünəcəklər)
    public class IsciListDto : BaseDto
    {
        // Ad və Soyadı ayrı-ayrı saxlamağa ehtiyac yoxdur, 
        // çünki siyahıda (Grid-də) adətən bütöv görünür.
        public string TamAd { get; set; } = null!;
        public string FIN { get; set; } = null!;

        // Struktur məlumatları
        public string SobeAdi { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;

        // Maliyyə məlumatı
        public decimal CariMaas { get; set; }

        public IsciStatus Status { get; set; }
    }

    // Create üçün (Formdan gələnlər)
    public class IsciCreateDto
    {
        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string AtaAdi { get; set; } = null!;
        public string FIN { get; set; } = null!;
        public string SeriyaNomre { get; set; } = null!;
        public DateTime DogumTarixi { get; set; }
        public Cins Cins { get; set; }
        public string? Telefon { get; set; }
        public string? Email { get; set; }
        public int SobeId { get; set; }
        public int VezifeId { get; set; }
        public DateTime IsheBaslamaTarixi { get; set; }

        // Maliyyə hissəsi (Generic Service daxilində Map olunacaq)
        public decimal CariMaas { get; set; }
        public int MezuniyyetQaliqGunu { get; set; } = 21;
        public string? BankHesabNo { get; set; }
    }

    // Update üçün
    public class IsciUpdateDto : IsciCreateDto
    {
        public int Id { get; set; }
    }

}

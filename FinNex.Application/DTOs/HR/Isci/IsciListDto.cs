using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Isci
{
    public class IsciListDto : BaseDto
    {
        public string TamAd { get; set; } = null!;

        /// <summary>İşçi siyahılarında göstərmə sırası (HR → "İşçi Sıralaması" səhifəsi).</summary>
        public int Sira { get; set; }
        public string FIN { get; set; } = null!;

        // IsciTeyinat-dan (aktiv)
        public string? SobeAdi { get; set; }
        public string? VezifeAdi { get; set; }

        public string? Email { get; set; }
        public string? Telefon { get; set; }

        // IsciMaliye-dan
        public decimal CariMaas { get; set; }

        public IsciStatus Status { get; set; }
        public Cins Cins { get; set; }
        public DateTime? IsdenAyrilmaTarixi { get; set; }
        public string? FesihSebebi { get; set; }
    }
}

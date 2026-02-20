using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    // Ümumi baxış üçün (Siyahılama)
    public class MezuniyyetDto
    {
        public int Id { get; set; }

        // İşçi məlumatları
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!; // Şöbə məlumatı vacibdir
        public string VezifeAdi { get; set; } = null!; // Vəzifə məlumatı vacibdir

        // Əvəzedici şəxs
        public int? EvezEdenIsciId { get; set; }
        public string? EvezEdenIsciAdSoyad { get; set; }

        public MezuniyyetNovu Nov { get; set; }
        public MezuniyyetStatus Status { get; set; }

        // Tarixlər
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; }

        public string? Qeyd { get; set; }
        public string? ImtinaSebebi { get; set; }

        // Workflow Tarixçəsi
        public bool? SobeReisiTesdiq { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        public bool? RehberTesdiq { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        public bool? HrTesdiq { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }
    }
}

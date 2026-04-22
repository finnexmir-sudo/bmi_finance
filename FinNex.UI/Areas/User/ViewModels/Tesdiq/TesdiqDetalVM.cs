using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.User.ViewModels.Tesdiq
{
    public class TesdiqDetalVM
    {
        // Əsas
        public int Id { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;
        public string? EvezEdenIsciAdSoyad { get; set; }

        // Tip
        public bool IsMezuniyyet { get; set; }

        // Məzuniyyət xüsusi
        public string? NovText { get; set; }
        public DateTime? BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }
        public int? IsGunlerininSayi { get; set; }
        public string? Qeyd { get; set; }

        // İcazə xüsusi
        public DateTime? IcazeTarixi { get; set; }
        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }
        public double? IcazeSaati { get; set; }
        public string? Sebeb { get; set; }

        // Ümumi
        public int Status { get; set; }
        public string StatusText { get; set; } = null!;
        public string? ImtinaSebebi { get; set; }

        // Workflow
        public bool? SobeReisiTesdiq { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }
        public bool? RehberTesdiq { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }
        public bool? HrTesdiq { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }

        // Cari təsdiqçi rolu
        public StrukturRolTipi TesdiqciRol { get; set; }

        // Əmr nömrəsi — yalnız məzuniyyət üçün, HR təsdiqindən sonra dolur
        public int? EmrRegem { get; set; }
        public string? EmrSuffiks { get; set; }
        public int? EmrIl { get; set; }
        public string? EmrNomresi => EmrRegem.HasValue ? $"K/M {EmrRegem}{EmrSuffiks ?? string.Empty}" : null;

        // Təsdiq ekranı üçün əlavə kontekst — yalnız məzuniyyət üçün
        public List<MezuniyyetOverlapDto> OverlapMezuniyyetler { get; set; } = new();
        public List<EvezediciKonfliktDto> EvezediciKonfliktleri { get; set; } = new();
    }
}

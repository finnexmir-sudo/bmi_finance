using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    public class MezuniyyetListDto
    {
        public int Id { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;

        public string? EvezEdenIsciAdSoyad { get; set; }
        public bool EvezediciSecildi => !string.IsNullOrEmpty(EvezEdenIsciAdSoyad);
        public bool? EvezediciTesdiqlenib { get; set; } // null=gözlənilir, true=qəbul, false=rədd

        public string NovText => Nov switch
        {
            MezuniyyetNovu.Illik => "İllik məzuniyyət",
            MezuniyyetNovu.Xestelik => "Xəstəlik məzuniyyəti",
            MezuniyyetNovu.Ezamiyyet => "Ezamiyyət",
            _ => Nov.ToString()
        };

        public MezuniyyetNovu Nov { get; set; }
        public MezuniyyetStatus Status { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; }

        public string WorkflowMerhele => Status switch
        {
            MezuniyyetStatus.Gozlemede => "Gözləmədə",
            MezuniyyetStatus.SobeReisiTesdiqinde => "Şöbə rəisi təsdiqində",
            MezuniyyetStatus.RehberTesdiqinde => "Rəhbər təsdiqində",
            MezuniyyetStatus.HrTesdiqinde => "HR təsdiqində",
            MezuniyyetStatus.Tesdiqlenib => "Təsdiqlənib",
            MezuniyyetStatus.ImtinaEdildi => "İmtina edildi",
            _ => "-"
        };
        // MezuniyyetListDto.cs-ə əlavə:
        public string? SobeReisiAdSoyad { get; set; }
        public bool? SobeReisiTesdiq { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        public string? RehberAdSoyad { get; set; }
        public bool? RehberTesdiq { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        public string? HrAdSoyad { get; set; }
        public bool? HrTesdiq { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }

        public string? ImtinaSebebi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }

        // Əmr nömrəsi (K/M)
        public int? EmrRegem { get; set; }
        public string? EmrSuffiks { get; set; }
        public int? EmrIl { get; set; }
        public string? EmrNomresi => EmrRegem.HasValue ? $"K/M {EmrRegem}{EmrSuffiks ?? string.Empty}" : null;

        // Müraciət sahibinin rolu — workflow göstəricisi üçün
        public bool MuracietSahibiRehberdirmi { get; set; }
        public bool MuracietSahibiSobeReisidirmi { get; set; }

        // Keçilmiş addımlar — rol əsaslı workflow üçün
        public bool SobeReisiKecildi => MuracietSahibiRehberdirmi || MuracietSahibiSobeReisidirmi
            || (SobeReisiTesdiq == null && (int)Status >= 3);
        public bool RehberKecildi => MuracietSahibiRehberdirmi
            || (RehberTesdiq == null && (int)Status >= 4);
    }
}
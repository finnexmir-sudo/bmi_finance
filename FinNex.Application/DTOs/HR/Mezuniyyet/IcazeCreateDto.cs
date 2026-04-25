using FinNex.Domain.Entities.HR;
using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.HR.Icaze
{
    // ── Yeni müraciət formu ──────────────────────────────
    public class IcazeCreateDto
    {
        public int IsciId { get; set; }  // Sistem avtomatik doldurur

        public int? EvezEdenIsciId { get; set; }

        [Required(ErrorMessage = "İcazə tarixi seçilməlidir")]
        public DateTime IcazeTarixi { get; set; }

        [Required(ErrorMessage = "Başlama saatı seçilməlidir")]
        public TimeSpan BaslamaSaati { get; set; }

        [Required(ErrorMessage = "Bitmə saatı seçilməlidir")]
        public TimeSpan BitisSaati { get; set; }

        [MaxLength(500, ErrorMessage = "Səbəb 500 simvoldan çox ola bilməz")]
        public string? Sebeb { get; set; }

        // Rol əsaslı workflow — controller tərəfindən doldurulur
        public bool MuracietSahibiRehberdirmi { get; set; }
        public bool MuracietSahibiSobeReisidirmi { get; set; }
        public bool MuracietSahibiHrdirmi { get; set; }
    }

    // ── Siyahı üçün ─────────────────────────────────────
    public class IcazeListDto
    {
        public int Id { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;

        public string? EvezEdenAdSoyad { get; set; }
        public bool EvezediciSecildi => !string.IsNullOrEmpty(EvezEdenAdSoyad) && EvezEdenAdSoyad != "—";
        public bool? EvezediciTesdiqlenib { get; set; }

        public DateTime IcazeTarixi { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }
        public double IcazeSaati { get; set; }

        public string? Sebeb { get; set; }
        public IcazeStatus Status { get; set; }

        public string WorkflowMerhele => Status switch
        {
            IcazeStatus.Gozlemede => "Gözləmədə",
            IcazeStatus.SobeReisiTesdiqinde => "Şöbə rəisi təsdiqində",
            IcazeStatus.RehberTesdiqinde => "Rəhbər təsdiqində",
            IcazeStatus.HrTesdiqinde => "HR təsdiqində",
            IcazeStatus.Tesdiqlenib => "Təsdiqlənib",
            IcazeStatus.ImtinaEdildi => "İmtina edildi",
            _ => "Naməlum"
        };
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

        // Müraciət sahibinin rolu — workflow göstəricisi üçün
        public bool MuracietSahibiRehberdirmi { get; set; }
        public bool MuracietSahibiSobeReisidirmi { get; set; }
        public bool MuracietSahibiHrdirmi { get; set; }

        // Keçilmiş addımlar — rol əsaslı workflow üçün
        public bool SobeReisiKecildi => MuracietSahibiRehberdirmi || MuracietSahibiSobeReisidirmi || MuracietSahibiHrdirmi
            || (SobeReisiTesdiq == null && (int)Status >= 3);
        public bool RehberKecildi => MuracietSahibiRehberdirmi
            || (RehberTesdiq == null && (int)Status >= 4);
        // HR muraciet edibsə HR step atlanır (Rehber təsdiqindən sonra birbaşa Tesdiqlenib)
        public bool HrKecildi => MuracietSahibiHrdirmi && HrTesdiq == null;
    }

    // ── Detallı baxış üçün ──────────────────────────────
    public class IcazeDetailDto : IcazeListDto
    {
        public string? ImtinaSebebi { get; set; }

        // Workflow tarixçəsi
        public bool? SobeReisiTesdiq { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        public bool? RehberTesdiq { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        public bool? HrTesdiq { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }
    }

    // ── Ləğv / güncəlləşdirmə üçün ──────────────────────
    public class IcazeUpdateDto
    {
        public int Id { get; set; }
        public IcazeStatus Status { get; set; }
        public string? ImtinaSebebi { get; set; }
    }
}
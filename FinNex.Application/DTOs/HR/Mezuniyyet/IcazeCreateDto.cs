using FinNex.Domain.Entities.HR;
using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.HR.Icaze
{
    // ── Yeni müraciət formu ──────────────────────────────
    public class IcazeCreateDto
    {
        public int IsciId { get; set; }

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
        public bool Birdefelik { get; set; }

        public string WorkflowMerhele => Status switch
        {
            IcazeStatus.Gozlemede             => "Gözləmədə",
            IcazeStatus.SobeReisiTesdiqinde   => "Şöbə rəisi təsdiqində",
            IcazeStatus.RehberTesdiqinde      => "Rəhbər təsdiqində",
            IcazeStatus.HrTesdiqinde          => "HR təsdiqində",
            IcazeStatus.Tesdiqlenib           => "Təsdiqlənib",
            IcazeStatus.ImtinaEdildi          => "İmtina edildi",
            _                                  => "Naməlum"
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

        // Müraciət sahibinin rolu
        public bool MuracietSahibiRehberdirmi { get; set; }
        public bool MuracietSahibiSobeReisidirmi { get; set; }
        public bool MuracietSahibiHrdirmi { get; set; }

        // YENİ AXİN: Şöbə rəisi artıq təsdiq zəncirindən çıxarıldı
        public bool SobeReisiKecildi => true; // həmişə keçilmiş sayılır
        public bool RehberKecildi => MuracietSahibiRehberdirmi
            || (RehberTesdiq == null && (int)Status >= 4);
        public bool HrKecildi => MuracietSahibiHrdirmi && HrTesdiq == null;

        // Cihaz çıxış/qayıdış izlənməsi
        public DateTime? CixisVaxt { get; set; }
        public DateTime? QayidisVaxt { get; set; }
        public IcazeCixisGirisStatus? CixisGirisStatus { get; set; }

        public string CixisGirisStatusText => CixisGirisStatus switch
        {
            IcazeCixisGirisStatus.Gozlenir   => "Gözlənir",
            IcazeCixisGirisStatus.Cixdi      => "İşdən kənarda",
            IcazeCixisGirisStatus.Tamamlandi => "Tamamlandı",
            IcazeCixisGirisStatus.LegvEdildi => "Ləğv edildi",
            _                                 => "—"
        };
    }

    // ── Detallı baxış üçün ──────────────────────────────
    public class IcazeDetailDto : IcazeListDto
    {
        public DateTime? CixisVaxtDetail => CixisVaxt;
        public DateTime? QayidisVaxtDetail => QayidisVaxt;
    }

    // ── Ləğv / güncəlləşdirmə üçün ──────────────────────
    public class IcazeUpdateDto
    {
        public int Id { get; set; }
        public IcazeStatus Status { get; set; }
        public string? ImtinaSebebi { get; set; }
    }

    // ── Dövriyyə siyahısı üçün ──────────────────────────
    public class IcazeDovriyyeDto
    {
        public int IcazeId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public DateTime IcazeTarixi { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }
        public double PlanlananSaat { get; set; }
        public bool Birdefelik { get; set; }
        public DateTime? CixisVaxt { get; set; }
        public DateTime? QayidisVaxt { get; set; }
        public double? FaktikiSaat { get; set; }
        public IcazeCixisGirisStatus CixisStatus { get; set; }

        public string CixisStatusText => CixisStatus switch
        {
            IcazeCixisGirisStatus.Gozlenir   => "Gözlənir",
            IcazeCixisGirisStatus.Cixdi      => "İşdən kənarda",
            IcazeCixisGirisStatus.Tamamlandi => "Tamamlandı",
            IcazeCixisGirisStatus.LegvEdildi => "Ləğv edildi",
            _                                 => "—"
        };
    }
}

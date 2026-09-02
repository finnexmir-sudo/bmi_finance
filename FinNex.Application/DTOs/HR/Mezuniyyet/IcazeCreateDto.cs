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

        [Required(ErrorMessage = "Səbəb mütləq qeyd edilməlidir")]
        [MaxLength(500, ErrorMessage = "Səbəb 500 simvoldan çox ola bilməz")]
        public string? Sebeb { get; set; }

        // Nahar fasiləsini icazəyə qatır (işçi nahara çıxmır) — max 3 saat 45 dəqiqəyə imkan verir
        public bool NaharNezereAlinmasin { get; set; } = false;

        // "Artıq müddəti jetonumdan ödə" — 3 saatlıq həddi aşan hissə işçinin
        // jeton balansından tutulur, buna görə pəncərə balans qədər uzun ola bilər.
        // Tutulacaq MİQDAR yazılmır, pəncərədən HESABLANIR (IcazeService.MecburiJetonSaat) —
        // güzəşt (uzun pəncərə) ilə çıxılma (jeton) beləcə heç vaxt bir-birindən ayrılmır.
        public bool JetonlaUzat { get; set; } = false;

        // Rol əsaslı workflow — controller tərəfindən doldurulur
        public bool MuracietSahibiRehberdirmi { get; set; }
        public bool MuracietSahibiSobeReisidirmi { get; set; }
        public bool MuracietSahibiHrdirmi { get; set; }

        // Rəhbər öz müraciəti üçün jetonla ödəmə saatı (0 = adi icazə)
        public decimal JetonOdenenSaat { get; set; } = 0;
    }

    // ── Siyahı üçün ─────────────────────────────────────
    public class IcazeListDto
    {
        public bool NaharNezereAlinmasin { get; set; } = false;
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        // Bu icazənin neçə saatı jetonla ödənilir (0 = adi icazə)
        public decimal JetonOdenenSaat { get; set; } = 0;

        public string? EvezEdenAdSoyad { get; set; }
        public bool EvezediciSecildi => !string.IsNullOrEmpty(EvezEdenAdSoyad) && EvezEdenAdSoyad != "—";
        public bool? EvezediciTesdiqlenib { get; set; }

        public DateTime IcazeTarixi { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }
        public double IcazeSaati { get; set; }

        // ── Nahar (fasilə) parametrləri — effektiv saat hesabı üçün; IsParametri-dən doldurulur.
        public int NaharDeqiqe { get; set; } = 45;

        // "Nahara çıxmıram" halında çıxılan müddət (saat) — SABİT nahar fasiləsi.
        // Güzəşt (+45 dəq uzun pəncərə) və çıxılma (−45 dəq) bir-birini tarazlayır →
        // sayılan icazə heç vaxt 3 saatı keçmir. Real kəsişmə İSTİFADƏ EDİLMİR:
        // nahara toxunmayan pəncərədə (14:00–17:45) çıxılma 0 olurdu və işçi naharda
        // işlədiyi halda sayğacdan tam 3s45d yazılırdı (bax: IcazeService.NaharCixilmaSaat).
        private double NaharCixilma(TimeSpan bas, TimeSpan bitis)
        {
            if (!NaharNezereAlinmasin) return 0;
            var pencere = (bitis - bas).TotalHours;
            if (pencere <= 0) return 0;
            var cixilan = NaharDeqiqe / 60.0;
            return cixilan < pencere ? cixilan : pencere;
        }

        // Effektiv (sayılan) PLANLAŞDIRILMIŞ müddət = pəncərə − nahar fasiləsi.
        public double EffektivSaat
        {
            get { var e = IcazeSaati - NaharCixilma(BaslamaSaati, BitisSaati); return e < 0 ? 0 : e; }
        }

        // Bu icazədən nahara görə çıxılan dəqiqə — UI-da göstərmək üçün (0 = çıxılmayıb).
        public int NaharCixilanDeqiqe =>
            (int)Math.Round(NaharCixilma(BaslamaSaati, BitisSaati) * 60);

        // Faktiki pəncərənin bitiş anı (time-of-day): qayıdış varsa onun saatı;
        // qayıdış yoxdursa (birdəfəlik / günün sonuna kimi) icazə bitmə saatı.
        private TimeSpan? FaktikiBitisTod =>
            QayidisVaxt?.TimeOfDay ?? (CixisVaxt != null ? BitisSaati : (TimeSpan?)null);

        // Effektiv FAKTİKİ müddət = xam faktiki − sabit nahar fasiləsi.
        // Planlaşdırılan ilə eyni məntiq — nahar həm plandan, həm faktikidən çıxılır.
        public double? EffektivFaktikiSaat
        {
            get
            {
                if (!FaktikiSaat.HasValue) return null;
                var cix = CixisVaxt?.TimeOfDay;
                var bit = FaktikiBitisTod;
                var cixilan = (cix.HasValue && bit.HasValue) ? NaharCixilma(cix.Value, bit.Value) : 0;
                var e = FaktikiSaat.Value - cixilan;
                return e < 0 ? 0 : e;
            }
        }
        // Faktiki istifadə (cihaz çıxış/qayıdışından): adi icazə → qayıdış−çıxış;
        // birdəfəlik → çıxışdan icazə sonuna. Punch yoxdursa null. Servis doldurur.
        public double? FaktikiSaat { get; set; }
        // İstifadə olunan (sayılan) saat = effektiv faktiki varsa o, yoxdursa effektiv plan.
        // Hər ikisi sabit nahar fasiləsi çıxılmaqla.
        public double IstifadeSaati => EffektivFaktikiSaat ?? EffektivSaat;

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

        // Cihaz çıxış/qayıdışı icazə pəncərəsinə uyğun gəlmirsə şübhəlidir (insan
        // faktoru — təsadüfi tanınma və s.). HR yoxlamalıdır.
        public bool CixisQayidisAnomaliya
        {
            get
            {
                var basDt = IcazeTarixi.Date + BaslamaSaati;
                var bitDt = IcazeTarixi.Date + BitisSaati;
                if (CixisVaxt.HasValue && CixisVaxt.Value < basDt.AddMinutes(-30)) return true;
                if (!Birdefelik && QayidisVaxt.HasValue && QayidisVaxt.Value > bitDt.AddMinutes(60)) return true;
                return false;
            }
        }
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

        /// <summary>
        /// İcazə qeydinin YARANMA anı (`Icaze.YaradilmaTarixi`) — sıralama açarı.
        ///
        /// NİYƏ LAZIMDIR: `IcazeTarixi` yalnız GÜNDÜR (saatsız). Eyni günə aid
        /// icazələr arasında sıra təsadüfi olurdu — yeni yazılan qeyd siyahının
        /// ortasına düşürdü. İkinci açar kimi bu sahə işlədilir: eyni gün içində
        /// ƏN SON yazılan qeyd ƏN YUXARIDA görünür (02.09.2026, istifadəçi tələbi).
        ///
        /// ⚠️ Sintetik (CixisGiris qeydi olmayan) sətirlərdə də doldurulmalıdır —
        /// yoxsa onlar `DateTime.MinValue` ilə həmişə günün ən altına düşər.
        /// </summary>
        public DateTime YaradilmaTarixi { get; set; }

        // ── Nahar ──────────────────────────────────────────────
        // FaktikiSaat servisdə onsuz da nahar çıxılmış gəlir; plan tərəfi də eyni
        // olmalıdır, yoxsa müqayisə (fərq) həmişə "erkən qayıdıb" kimi görünür.
        public bool NaharNezereAlinmasin { get; set; }
        public int NaharDeqiqe { get; set; } = 45;

        public int NaharCixilanDeqiqe => NaharNezereAlinmasin
            ? (int)Math.Round(Math.Min(NaharDeqiqe / 60.0, PlanlananSaat) * 60)
            : 0;

        // Sayılan (effektiv) planlaşdırılan müddət — nahar çıxılmaqla.
        public double EffektivPlanSaat =>
            Math.Max(0, PlanlananSaat - NaharCixilanDeqiqe / 60.0);

        public string CixisStatusText => CixisStatus switch
        {
            IcazeCixisGirisStatus.Gozlenir   => "Gözlənir",
            IcazeCixisGirisStatus.Cixdi      => "İşdən kənarda",
            IcazeCixisGirisStatus.Tamamlandi => "Tamamlandı",
            IcazeCixisGirisStatus.LegvEdildi => "Ləğv edildi",
            _                                 => "—"
        };

        // Cihaz çıxış/qayıdışı icazə pəncərəsinə uyğun gəlmirsə şübhəlidir (insan
        // faktoru — təsadüfi tanınma və s.). HR yoxlamalıdır.
        public bool CixisQayidisAnomaliya
        {
            get
            {
                var basDt = IcazeTarixi.Date + BaslamaSaati;
                var bitDt = IcazeTarixi.Date + BitisSaati;
                if (CixisVaxt.HasValue && CixisVaxt.Value < basDt.AddMinutes(-30)) return true;
                if (!Birdefelik && QayidisVaxt.HasValue && QayidisVaxt.Value > bitDt.AddMinutes(60)) return true;
                return false;
            }
        }
    }
}

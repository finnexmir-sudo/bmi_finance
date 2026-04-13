using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Dashboard
{
    /// <summary>
    /// Dashboard üçün vahid DTO — bütün lazımi məlumatları bir yerdə toplayır.
    /// DashboardService bu DTO-nu qaytarır, UI layer onu ViewModel-ə map edir.
    /// </summary>
    public class UserDashboardDto
    {
        // ── Profil ────────────────────────────────────────────
        public int IsciId { get; set; }
        public string TamAd { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public DateTime IsheBaslamaTarixi { get; set; }

        // ── Davamiyyət (cari ay) ──────────────────────────────
        public int IslenanGun { get; set; }
        public int IsGunuSayi { get; set; }  // Ay üzrə iş günü sayı
        public int QaibGun { get; set; }
        public int IcazeliGun { get; set; }  // DavamiyyetStatus.Icazeli
        public int GozlenilenGun { get; set; }  // Hələ gəlməmiş gələcək günlər

        // ── Məzuniyyət balansı (cari il, növ üzrə) ───────────
        // MezuniyyetBalans-dan + Mezuniyyet cədvəlindən hesablanır
        public int IllikToplamGun { get; set; }  // MezuniyyetBalans.ToplamGun
        public int IllikIstifadeGun { get; set; }  // MezuniyyetBalans.IstifadeOlunanGun
        public int IllikQaligGun => IllikToplamGun - IllikIstifadeGun;

        public int XestelikIstifadeGun { get; set; } // Nov==Xestelik olan təsdiqlənmiş günlər
        public int XestelikToplamGun { get; set; } = 0; // limitsiz
        public int XestelikQaligGun => 0; // limitsiz — qalıq yoxdur

        public int EzamiyyetIstifadeGun { get; set; }
        public int EzamiyyetToplamGun { get; set; } = 0; // limitsiz
        public int EzamiyyetQaligGun => 0; // limitsiz — qalıq yoxdur

        // ── Davamiyyət təqvimi (cari ay günlər) ──────────────
        public List<DashboardDavamiyyetGunDto> DavamiyyetTakvim { get; set; } = new();

        // ── Son ödənişlər ─────────────────────────────────────
        // Maas cədvəlindən son 3 ay, MaasListDto-dan map olunur
        public List<DashboardMaasDto> SonOdenisler { get; set; } = new();

        // ── Aktiv məzuniyyət müraciətləri ────────────────────
        public List<DashboardMezuniyyetDto> AktivMuracietler { get; set; } = new();

        // ── Aktiv icazə müraciətləri ──────────────────────────
        public List<DashboardIcazeDto> AktivIcazeler { get; set; } = new();

        // ── Bildirişlər ───────────────────────────────────────
        public List<DashboardBildiriDto> Bildiriler { get; set; } = new();
    }

    // ── Davamiyyət təqvim günü ────────────────────────────────
    public class DashboardDavamiyyetGunDto
    {
        public DateTime Tarix { get; set; }
        public int Gun => Tarix.Day;
        public DavamiyyetStatus Status { get; set; }
        public bool HefteSonu => Tarix.DayOfWeek == DayOfWeek.Saturday
                                  || Tarix.DayOfWeek == DayOfWeek.Sunday;
        public bool Gələcek => Tarix.Date > DateTime.Today;
        public bool Bayramdir { get; set; }
        public string? BayramAdi { get; set; }
    }

    // ── Maaş sətri ───────────────────────────────────────────
    public class DashboardMaasDto
    {
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal NetMebleg { get; set; }
        public MaasStatus Status { get; set; }

        public string AyAd => new DateTime(Il, Ay, 1).ToString("MMMM yyyy");
    }

    // ── Məzuniyyət müraciəti sətri ───────────────────────────
    public class DashboardMezuniyyetDto
    {
        public int Id { get; set; }
        public MezuniyyetNovu Nov { get; set; }
        public MezuniyyetStatus Status { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; }

        // Əvəzedici şəxs (bankda mütləqdir)
        public string? EvezEdenAdSoyad { get; set; }

        // Workflow mərhələsi — UI-da göstəriləcək
        public string WorkflowMerhele => Status switch
        {
            MezuniyyetStatus.Gozlemede => "Gözləmədə",
            MezuniyyetStatus.SobeReisiTesdiqinde => "Şöbə rəisi təsdiqində",
            MezuniyyetStatus.RehberTesdiqinde => "Rəhbər təsdiqində",
            MezuniyyetStatus.HrTesdiqinde => "HR təsdiqində",
            MezuniyyetStatus.Tesdiqlenib => "Təsdiqlənib",
            MezuniyyetStatus.ImtinaEdildi => "İmtina edildi",
            _ => "Naməlum"
        };

        public string NovAd => Nov switch
        {
            MezuniyyetNovu.Illik => "İllik məzuniyyət",
            MezuniyyetNovu.Xestelik => "Xəstəlik məzuniyyəti",
            MezuniyyetNovu.Ezamiyyet => "Ezamiyyət",
            _ => "Digər"
        };
    }

    // ── Bildiriş sətri ───────────────────────────────────────
    public class DashboardBildiriDto
    {
        public string Metn { get; set; } = null!;
        public DateTime Tarix { get; set; }
        public BildiriNovu Nov { get; set; }

        public string TarixAd
        {
            get
            {
                var diff = DateTime.Now - Tarix;
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dəq əvvəl";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat əvvəl";
                if (diff.TotalDays < 2) return "Dünən";
                return Tarix.ToString("dd.MM.yyyy");
            }
        }
    }

    public enum BildiriNovu { Ugur, Xeberdarliq, Tehlike, Melumat }

    // ── İcazə müraciəti sətri (dashboard üçün) ───────────────
    public class DashboardIcazeDto
    {
        public int Id { get; set; }
        public DateTime IcazeTarixi { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }
        public double IcazeSaati { get; set; }
        public string? EvezEdenAdSoyad { get; set; }
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
    }
}
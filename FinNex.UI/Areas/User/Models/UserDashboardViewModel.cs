using FinNex.Application.DTOs.HR.Dashboard;

namespace FinNex.UI.Areas.User.Models
{
    public class UserDashboardViewModel
    {
        // ── Profil ─────────────────────────────────────────────
        public string TamAd { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public double IsStaji { get; set; }

        // ── Davamiyyət ──────────────────────────────────────────
        public int IslenanGun { get; set; }
        public int IsGunuSayi { get; set; }
        public int QaibGun { get; set; }
        public int IcazeliGun { get; set; }
        public int GozlenilenGun { get; set; }

        // ── Məzuniyyət balansı ──────────────────────────────────
        public int IllikToplamGun { get; set; }
        public int IllikIstifadeGun { get; set; }
        public int IllikQaligGun => IllikToplamGun - IllikIstifadeGun;

        public int XestelikToplamGun { get; set; }
        public int XestelikIstifadeGun { get; set; }
        public int XestelikQaligGun => XestelikToplamGun - XestelikIstifadeGun;

        public int EzamiyyetToplamGun { get; set; }
        public int EzamiyyetIstifadeGun { get; set; }
        public int EzamiyyetQaligGun => EzamiyyetToplamGun - EzamiyyetIstifadeGun;

        // ── Siyahılar ───────────────────────────────────────────
        public List<DashboardDavamiyyetGunDto> DavamiyyetTakvim { get; set; } = new();
        public List<DashboardMaasDto> SonOdenisler { get; set; } = new();
        public List<DashboardMezuniyyetDto> AktivMuracietler { get; set; } = new();
        public List<DashboardIcazeDto> AktivIcazeler { get; set; } = new();
        public List<DashboardBildiriDto> Bildiriler { get; set; } = new();

        // ── DTO-dan map et ──────────────────────────────────────
        public static UserDashboardViewModel FromDto(UserDashboardDto dto) => new()
        {
            TamAd = dto.TamAd,
            VezifeAdi = dto.VezifeAdi,
            SobeAdi = dto.SobeAdi,
            IsStaji = Math.Round((DateTime.Now - dto.IsheBaslamaTarixi).TotalDays / 365.0, 1),

            IslenanGun = dto.IslenanGun,
            IsGunuSayi = dto.IsGunuSayi,
            QaibGun = dto.QaibGun,
            IcazeliGun = dto.IcazeliGun,
            GozlenilenGun = dto.GozlenilenGun,

            IllikToplamGun = dto.IllikToplamGun,
            IllikIstifadeGun = dto.IllikIstifadeGun,
            XestelikToplamGun = dto.XestelikToplamGun,
            XestelikIstifadeGun = dto.XestelikIstifadeGun,
            EzamiyyetToplamGun = dto.EzamiyyetToplamGun,
            EzamiyyetIstifadeGun = dto.EzamiyyetIstifadeGun,

            DavamiyyetTakvim = dto.DavamiyyetTakvim,
            SonOdenisler = dto.SonOdenisler,
            AktivMuracietler = dto.AktivMuracietler,
            AktivIcazeler = dto.AktivIcazeler,
            Bildiriler = dto.Bildiriler,
        };
    }
}
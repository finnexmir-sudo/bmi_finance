using FinNex.Application.DTOs.HR.Dashboard;

namespace FinNex.UI.Areas.User.Models
{
    /// <summary>
    /// İşçi dashboard ViewModel.
    /// Domain entity-lərinə birbaşa istinad yoxdur —
    /// DavamiyyetStatus və MaasStatus string-ə çevrilərək saxlanılır.
    /// </summary>
    public class UserDashboardViewModel
    {
        // ── Profil ─────────────────────────────────────────────
        public string TamAd { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public double IsStaji { get; set; }

        // ── Davamiyyət (cari ay) ────────────────────────────────
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

        // ── Siyahılar (string-based VM sinifləri) ───────────────
        public List<DavamiyyetGunVM> DavamiyyetTakvim { get; set; } = new();
        public List<MaasVM> SonOdenisler { get; set; } = new();
        public List<DashboardMezuniyyetDto> AktivMuracietler { get; set; } = new();
        public List<DashboardIcazeDto> AktivIcazeler { get; set; } = new();
        public List<BildiriVM> Bildiriler { get; set; } = new();

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

            DavamiyyetTakvim = dto.DavamiyyetTakvim
                .Select(g => new DavamiyyetGunVM
                {
                    Tarix = g.Tarix,
                    StatusText = (int)g.Status switch
                    {
                        1 => "İşdə",
                        2 => "Gecikme",
                        3 => "Qayıb",
                        4 => "İcazəli",
                        _ => ""
                    },
                    HefteSonu = g.HefteSonu,
                    Gelecek = g.Gələcek,
                })
                .ToList(),

            SonOdenisler = dto.SonOdenisler
                .Select(p => new MaasVM
                {
                    Il = p.Il,
                    Ay = p.Ay,
                    AyAd = p.AyAd,
                    NetMebleg = p.NetMebleg,
                    StatusText = (int)p.Status switch
                    {
                        1 => "Hazırlanır",
                        2 => "Təsdiqləndi",
                        3 => "Ödənildi",
                        4 => "Ləğv edildi",
                        _ => "-"
                    },
                })
                .ToList(),

            AktivMuracietler = dto.AktivMuracietler,
            AktivIcazeler = dto.AktivIcazeler,

            Bildiriler = dto.Bildiriler
                .Select(b => new BildiriVM
                {
                    Metn = b.Metn,
                    TarixAd = b.TarixAd,
                    NovText = b.Nov.ToString(),
                })
                .ToList(),
        };
    }

    // ── Dashboard-a məxsus string-based ViewModel sinifləri ────

    public class DavamiyyetGunVM
    {
        public DateTime Tarix { get; set; }
        public int Gun => Tarix.Day;
        public string StatusText { get; set; } = "";
        public bool HefteSonu { get; set; }
        public bool Gelecek { get; set; }
    }

    public class MaasVM
    {
        public int Il { get; set; }
        public int Ay { get; set; }
        public string AyAd { get; set; } = "";
        public decimal NetMebleg { get; set; }
        public string StatusText { get; set; } = "";
    }

    public class BildiriVM
    {
        public string Metn { get; set; } = "";
        public string TarixAd { get; set; } = "";
        public string NovText { get; set; } = "";
    }
}

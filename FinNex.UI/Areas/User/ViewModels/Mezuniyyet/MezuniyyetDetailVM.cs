using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.UI.Areas.User.ViewModels.Mezuniyyet
{
    /// <summary>
    /// Məzuniyyət müraciəti detay səhifəsi üçün ViewModel.
    /// Domain entity-lərinə birbaşa istinad yoxdur.
    /// </summary>
    public class MezuniyyetDetailVM
    {
        // ── Əsas məlumatlar ────────────────────────────────────
        public int Id { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;
        public string? EvezEdenIsciAdSoyad { get; set; }
        public bool EvezediciSecildi { get; set; }
        public bool? EvezediciTesdiqlenib { get; set; }
        public bool MuracietSahibiRehberdirmi { get; set; }
        public bool MuracietSahibiSobeReisidirmi { get; set; }

        // ── Məzuniyyət məlumatları ─────────────────────────────
        public string NovText { get; set; } = null!;
        public string StatusText { get; set; } = null!;
        public string BadgeClass { get; set; } = null!;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; }
        public string? Qeyd { get; set; }
        public string? ImtinaSebebi { get; set; }

        // ── Workflow addımları (0-4 range, -1 = imtina) ────────
        public int WorkflowAddim { get; set; }
        public bool ImtinaEdildi { get; set; }

        // ── Workflow tarixçəsi ─────────────────────────────────
        public bool? SobeReisiTesdiq { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        public bool? RehberTesdiq { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        public bool? HrTesdiq { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }

        // ── Ləğv edə bilər? (yalnız Gözləmədə və ya Şöbə rəisi) *@
        public bool LegvEdeBiler { get; set; }

        // ── DTO-dan map ────────────────────────────────────────
        public static MezuniyyetDetailVM FromDto(MezuniyyetDto dto) => new()
        {
            Id = dto.Id,
            IsciAdSoyad = dto.IsciAdSoyad,
            SobeAdi = dto.SobeAdi,
            VezifeAdi = dto.VezifeAdi,
            EvezEdenIsciAdSoyad = dto.EvezEdenIsciAdSoyad,
            EvezediciSecildi = !string.IsNullOrEmpty(dto.EvezEdenIsciAdSoyad),

            NovText = (int)dto.Nov switch
            {
                1 => "İllik məzuniyyət",
                2 => "Xəstəlik məzuniyyəti",
                3 => "Ezamiyyət",
                _ => dto.Nov.ToString()
            },
            StatusText = (int)dto.Status switch
            {
                1 => "Gözləmədə",
                2 => "Şöbə rəisi təsdiqində",
                3 => "Rəhbər təsdiqində",
                4 => "HR təsdiqində",
                5 => "Təsdiqlənib",
                6 => "İmtina edildi",
                _ => dto.Status.ToString()
            },
            BadgeClass = (int)dto.Status switch
            {
                5 => "fn-badge--done",
                6 => "fn-badge--rejected",
                1 => "fn-badge--wait",
                _ => "fn-badge--review"
            },

            BaslamaTarixi = dto.BaslamaTarixi,
            BitmeTarixi = dto.BitmeTarixi,
            IsGunlerininSayi = dto.IsGunlerininSayi,
            Qeyd = dto.Qeyd,
            ImtinaSebebi = dto.ImtinaSebebi,

            // MezuniyyetStatus: 1=Gozlemede,2=Sobe,3=Rehber,4=HR,5=Tesdiq,6=Imtina
            // WorkflowAddim: Gozlemede=0,Sobe=1,Rehber=2,HR=3,Tesdiq=4,Imtina=-1
            WorkflowAddim = (int)dto.Status switch
            {
                5 => 4,
                6 => -1,
                var s => s - 1
            },
            ImtinaEdildi = (int)dto.Status == 6,

            SobeReisiTesdiq = dto.SobeReisiTesdiq,
            SobeReisiTesdiqTarixi = dto.SobeReisiTesdiqTarixi,
            RehberTesdiq = dto.RehberTesdiq,
            RehberTesdiqTarixi = dto.RehberTesdiqTarixi,
            HrTesdiq = dto.HrTesdiq,
            HrTesdiqTarixi = dto.HrTesdiqTarixi,

            LegvEdeBiler = (int)dto.Status <= 4,  // Gozlemede(1), SobeReisiTesdiqinde(2), RehberTesdiqinde(3), HrTesdiqinde(4)
        };
    }
}

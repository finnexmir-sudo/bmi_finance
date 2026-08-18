using FinNex.Domain.Entities.HR;
using FinNex.Application.Common.Extensions;

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

        public string NovText => Nov.Adi();

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
        public bool MuracietSahibiHrdirmi { get; set; }

        // Jeton ilə əvəzlənmə
        public bool JetonIleOdendi { get; set; }
        public decimal? IstifadeOlunanJetonSaat { get; set; }

        // ── Ləğv müraciəti ──────────────────────────────────
        public MezuniyyetOdenisStatus OdenisStatus { get; set; }
        public bool      LegvTelebEdilib { get; set; }
        public string?   LegvTelebSebebi { get; set; }
        public DateTime? LegvTelebTarixi { get; set; }

        // İşçi ləğv müraciəti göndərə bilər: təsdiqlənmiş + başlamamış + ödənilməmiş + hələ tələb edilməmiş
        public bool LegvTelebiMumkun =>
            Status == MezuniyyetStatus.Tesdiqlenib
            && BaslamaTarixi.Date > DateTime.Today
            && OdenisStatus != MezuniyyetOdenisStatus.Odenilib
            && OdenisStatus != MezuniyyetOdenisStatus.PlanliOdenis
            && !LegvTelebEdilib;

        // ── Keçilmiş addımlar — rol əsaslı workflow göstəricisi ──────────────
        // Bu üç sahə `MezuniyyetService.YaratAsync` (sətir 95-107) marşrutlaşdırmasının
        // GÜZGÜSÜDÜR. Orada şərtlərin SIRASI vacibdir və burada da eyni olmalıdır:
        //   1) HR         → Şöbə rəisi ATLANIR, Rəhbər VAR,     HR ATLANIR
        //   2) Rəhbər     → Şöbə rəisi ATLANIR, Rəhbər ATLANIR, HR VAR
        //   3) Şöbə rəisi → Şöbə rəisi ATLANIR, Rəhbər VAR,     HR VAR
        //   4) adi işçi   → hamısı (şöbədə aktiv şöbə rəisi yoxdursa 1-ci atlanır)
        //
        // 18.08.2026 — REAL HADİSƏ: `RehberKecildi` yalnız `MuracietSahibiRehberdirmi`-yə
        // baxırdı. HR VƏ Rəhbər rolu BİRLİKDƏ olan işçidə (Anar İbrahimov: Operator+HR+Rehber)
        // servis HR şərtini ƏVVƏL yoxlayır → müraciət RehberTesdiqinde-yə düşür, yəni
        // Rəhbər addımı ATLANMIR. Göstərmə qatı isə onu keçilmiş sayıb GİZLƏDİRDİ:
        // işçi öz «Müraciət gedişatı» ekranında «Müraciət göndərildi → HR» görürdü,
        // halbuki müraciət Rəhbərdə gözləyirdi. Heç bir xəta çıxmırdı.
        //
        // DİQQƏT: İcazə modulunda prioritet QƏSDƏN ƏKSİNƏDİR (`IcazeService` sətir 174 —
        // əvvəl Rəhbər, sonra HR), ona görə `IcazeCreateDto.RehberKecildi` belə DEYİL.
        // İkisini «eyniləşdirmək» olmaz.
        public bool SobeReisiKecildi => MuracietSahibiRehberdirmi || MuracietSahibiSobeReisidirmi || MuracietSahibiHrdirmi
            || (SobeReisiTesdiq == null && (int)Status >= 3);
        public bool RehberKecildi => (!MuracietSahibiHrdirmi && MuracietSahibiRehberdirmi)
            || (RehberTesdiq == null && (int)Status >= 4);
        // HR muraciet edibsə HR step atlanır
        public bool HrKecildi => MuracietSahibiHrdirmi && HrTesdiq == null;
    }
}
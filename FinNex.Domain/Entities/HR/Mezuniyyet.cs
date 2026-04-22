using System.ComponentModel.DataAnnotations.Schema;

namespace FinNex.Domain.Entities.HR
{
    public class Mezuniyyet : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        // Banklarda əvəzedici şəxs mütləqdir
        public int? EvezEdenIsciId { get; set; }
        public Isci? EvezEdenIsci { get; set; }

        public MezuniyyetNovu Nov { get; set; }
        public MezuniyyetStatus Status { get; set; } = MezuniyyetStatus.Gozlemede;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; } // Bayramlar çıxılmaqla hesablanan gün

        public string? Qeyd { get; set; }
        public string? ImtinaSebebi { get; set; }

        // --- Təsdiq Mərhələləri (Workflow) ---

        // 1. Şöbə Rəisi
        public bool? SobeReisiTesdiq { get; set; }
        public int? SobeReisiId { get; set; }
        public DateTime? SobeReisiTesdiqTarixi { get; set; }

        // 2. Rəhbər (Məsələn: Departament müdiri və ya Filial müdiri)
        public bool? RehberTesdiq { get; set; }
        public int? RehberId { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }

        // 3. HR (Son nöqtə və sənədləşmə)
        public bool? HrTesdiq { get; set; }
        public int? HrId { get; set; }
        public DateTime? HrTesdiqTarixi { get; set; }

        // Mezuniyyet.cs-ə əlavə:
        public Isci? SobeReisiIsci { get; set; }
        public Isci? RehberIsci { get; set; }
        public Isci? HrIsci { get; set; }

        // ── Ödəniş vaxtı və statusu (QabaqcadanOdenis / AySonuOdenis) ──
        //
        // İşçi müraciət edərkən seçir:
        //  AySonuOdenis (default)  — mövcud davranış: məzuniyyət pulu həmin ayın
        //                            maaşına əlavə olunur (MAX(MH, ƏH)).
        //  QabaqcadanOdenis        — HR təsdiqindən sonra Mühasibə bildiriş gedir,
        //                            Mühasib ödənişi ayrıca edir və “Ödənildi” vurur.
        //                            Maaş hesablamasında həmin ay(lar)ın kəsintisi
        //                            olur, amma məzuniyyət ödənişi əlavə olunmur
        //                            (artıq qabaqcadan verilib).
        public MezuniyyetOdenisTipi OdenisTipi { get; set; } = MezuniyyetOdenisTipi.AySonuOdenis;
        public MezuniyyetOdenisStatus OdenisStatus { get; set; } = MezuniyyetOdenisStatus.TetbiqEdilmir;

        // HR təsdiq anında hesablanan tam məzuniyyət ödənişi məbləği.
        // Mühasib yoxlayıb düzəldə bilər (səhv olarsa).
        public decimal? OdenenMebleg { get; set; }
        public DateTime? OdenilmeTarixi { get; set; }
        public int? OdeyenMuhasibId { get; set; }
        public Isci? OdeyenMuhasib { get; set; }

        // PlanliOdenis mərhələsində — faktiki bank köçürməsi bu tarixdə icra
        // olunur (məzuniyyətdən bir iş günü əvvəl). Fon xidməti həmin tarixdə
        // statusu avtomatik Odenilib-ə çevirir.
        public DateTime? PlanliOdenisTarixi { get; set; }

        // ── Əmr nömrəsi (K/M) ────────────────────────────────
        // HR təsdiqindən sonra avtomatik verilir. Hər il 1-dən başlayır.
        // Rəqəm hissəsi ayrıca saxlanır ki, suffiks ("a", "b" və s.) əlavə
        // etmək lazım olsa (məs. araya düşmüş əmr halları), düzgün sırala bilək.
        public int? EmrRegem { get; set; }
        public string? EmrSuffiks { get; set; }   // nvarchar(5), adətən null; məs "a", "b"
        public int? EmrIl { get; set; }           // Hansı il üçün (reset logic)

        /// <summary>
        /// Tam əmr nömrəsi formatı — "K/M 1", "K/M 10a" və s.
        /// Boşdursa (hələ HR təsdiq olmayıb) — null.
        /// </summary>
        [NotMapped]
        public string? EmrNomresi =>
            EmrRegem.HasValue ? $"K/M {EmrRegem}{EmrSuffiks ?? string.Empty}" : null;
    }
}

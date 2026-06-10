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
        public int IsGunlerininSayi { get; set; } // Avtomatik hesablanan (qayda üzrə)

        /// <summary>
        /// HR təsdiq zamanı manual dəyişə biləcəyi "son" gün sayı.
        /// Null olarsa — IsGunlerininSayi (avtomatik) istifadə olunur.
        /// Dəyər varsa — effektiv gün sayı bu olur (balansdan bu qədər kəsilir,
        /// davamiyyət qeydləri də bu aralıqdan yaradılır).
        /// </summary>
        public int? IsGunlerininSayiManual { get; set; }

        /// <summary>
        /// HR manual düzəliş etdikdə səbəbi (audit üçün).
        /// </summary>
        public string? GunHesabiDuzelisiSebebi { get; set; }

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
        public decimal? OdenenMebleg { get; set; }       // NET (bank köçürməsi)
        public decimal? OdenenMeblegBrut { get; set; }   // Brüt (vergi bazası üçün)
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

        /// <summary>
        /// Effektiv iş günü sayı — balans kəsimi və davamiyyət qeydləri
        /// üçün istifadə olunan final rəqəm. HR manual dəyər varsa onu
        /// qaytarır, yoxdursa avtomatik hesablanmış IsGunlerininSayi.
        /// </summary>
        [NotMapped]
        public int EfektivGunSayi => IsGunlerininSayiManual ?? IsGunlerininSayi;

        // ── Dövlət Vəzifəsi Korreksiyası ─────────────────────────────────
        // DovletVezifelerininIcrasi növündə yaradılan qeydlər üçün:
        //   KorreksiyaOlunanMezuniyyetId — hansı əmək məzuniyyətinin yerinə gəlir
        //   SenedYolu                    — yüklənmiş rəsmi sənəd (hərbi əmr, məhkəmə vərəqəsi)
        //   KorreksiyaSebebi             — HR-in qeyd etdiyi açıqlama
        public int?    KorreksiyaOlunanMezuniyyetId { get; set; }
        public Mezuniyyet? KorreksiyaOlunanMezuniyyet { get; set; }
        public string? SenedYolu       { get; set; }
        public string? KorreksiyaSebebi { get; set; }

        // ── Jeton ilə əvəzlənmə ──────────────────────────────────
        // HR bu məzuniyyəti işçinin jeton balansından ödəyibsə:
        //   JetonleOdend = true  → maaş hesablamasında kəsinti VƏ ödəniş OLMUR
        //   IstifadeOlunanJetonSaat — neçə jeton saatı xərcləndi (audit üçün)
        public bool     JetonleOdend           { get; set; } = false;
        public decimal? IstifadeOlunanJetonSaat { get; set; }
    }
}

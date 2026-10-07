namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// İşçiyə HR tərəfindən verilmiş fərdi imtiyaz/istisna (07.10.2026, istifadəçi
    /// tələbi). Məsələn: "Əlil işçi həftənin 5-ci günü nahardan sonra gedə bilər".
    ///
    /// YALNIZ GÖSTƏRİŞDİR (istifadəçi qərarı) — Davamiyyət statusuna, hesablamaya
    /// və ya hər hansı avtomatik qaydaya TƏSİR ETMİR. Rəhbər/Davamiyyət işçisi bu
    /// qeydi oxuyub öz qərarını özü verir; sistem heç nəyi üstələmir, bloklamır.
    ///
    /// Bir işçinin bir neçə sətri ola bilər (hər sətir bir qayda) — mürəkkəb
    /// çox-günlü seçim widget-i YOXDUR, "üç oxşar sətir premature abstraction-dan
    /// yaxşıdır" prinsipinə uyğun.
    /// </summary>
    public class IsciImtiyaz : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        /// <summary>Qısa başlıq — nümunə: "Nahardan sonra gedə bilər".</summary>
        public string Baslik { get; set; } = string.Empty;

        /// <summary>Həftənin hansı günü aiddir — null = hər gün.</summary>
        public DayOfWeek? HeftaGunu { get; set; }

        /// <summary>Saat aralığı başlanğıcı — null (hər ikisi) = bütün gün.</summary>
        public TimeSpan? BaslamaSaati { get; set; }

        /// <summary>Saat aralığı sonu — null (hər ikisi) = bütün gün.</summary>
        public TimeSpan? BitisSaati { get; set; }

        /// <summary>Səbəb/izah — sərbəst mətn.</summary>
        public string? Aciqlama { get; set; }

        public DateTime BaslamaTarixi { get; set; }

        /// <summary>Qüvvədən düşmə tarixi — null = müddətsiz.</summary>
        public DateTime? BitmeTarixi { get; set; }

        /// <summary>HR istənilən vaxt söndürə bilər — silmədən.</summary>
        public bool Aktivdir { get; set; } = true;
    }
}

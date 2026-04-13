namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Pilləli vergi hesablaması üçün bir pillə.
    ///
    /// Düstur:
    ///   tutulma = SabitMebleg + (mebleg - AsagiHedd) * (Faiz / 100)
    ///
    /// Pillənin əhatə dairəsi: [AsagiHedd, YuxariHedd)
    /// YuxariHedd = null → sonsuzluq (sonuncu pillə).
    ///
    /// Məsələn, 2026 Gəlir Vergisi:
    ///   Pillə 1: 0 – 2500      → SabitMebleg=0,   Faiz=3%
    ///   Pillə 2: 2500 – 8000   → SabitMebleg=75,  Faiz=10%
    ///   Pillə 3: 8000+         → SabitMebleg=625, Faiz=14%
    /// </summary>
    public class VergiPille : BaseEntity
    {
        public MaasParametrNovu Nov { get; set; }

        /// <summary>Eyni növ pillələr arasında sıra (1, 2, 3...)</summary>
        public int Sira { get; set; }

        /// <summary>Aşağı hədd (daxil olmaqla)</summary>
        public decimal AsagiHedd { get; set; }

        /// <summary>Yuxarı hədd (daxil olmayaraq). null = sonsuz (sonuncu pillə)</summary>
        public decimal? YuxariHedd { get; set; }

        /// <summary>Bu pillənin əlavə dərəcəsi (AsagiHedd-dən yuxarı hissəyə tətbiq olunur)</summary>
        public decimal Faiz { get; set; }

        /// <summary>Bu pilləyə qədər yığılmış sabit məbləğ (aşağı pillələrdən)</summary>
        public decimal SabitMebleg { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }

        public bool Aktivdir { get; set; } = true;

        public string? Aciqlama { get; set; }
    }
}

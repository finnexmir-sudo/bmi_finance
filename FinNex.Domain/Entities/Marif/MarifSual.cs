namespace FinNex.Domain.Entities.Marif
{
    /// <summary>
    /// Bir sual dəstinin tək sualı. Çoxseçimlidə <see cref="DuzgunVariant"/> ("A".."D")
    /// ilə avtomatik qiymətləndirilir; açıq sualda <see cref="DuzgunCavabMetni"/>
    /// işçiyə cavabdan dərhal sonra "doğru yanaşma" kimi göstərilir (qiymətləndirmə yoxdur).
    /// </summary>
    public class MarifSual : BaseEntity
    {
        public int SualSetiId { get; set; }
        public MarifSualSeti SualSeti { get; set; } = null!;

        public MarifSualTipi Tip { get; set; }

        public string SualMetni { get; set; } = string.Empty;

        // Yalnız CoxSecimli üçün
        public string? VariantA { get; set; }
        public string? VariantB { get; set; }
        public string? VariantC { get; set; }
        public string? VariantD { get; set; }
        public string? DuzgunVariant { get; set; }

        // Yalnız Acig üçün — "nümunəvi/doğru cavab"
        public string? DuzgunCavabMetni { get; set; }

        /// <summary>Cavabdan dərhal sonra göstərilən izah — mənbə bəndinə istinadla.</summary>
        public string IzahMetni { get; set; } = string.Empty;

        public int Sira { get; set; }
    }
}

using FinNex.Domain.Entities.Marif;

namespace FinNex.Application.DTOs.Marif
{
    /// <summary>
    /// Həm AI-nin hazırladığı qaralama sualı, həm HR-in redaktə edib yadda
    /// saxladığı son sualı daşıyır — iki ayrı şəkil saxlanmır.
    /// </summary>
    public class MarifSualDto
    {
        public int Id { get; set; }
        public MarifSualTipi Tip { get; set; }
        public string SualMetni { get; set; } = "";

        public string? VariantA { get; set; }
        public string? VariantB { get; set; }
        public string? VariantC { get; set; }
        public string? VariantD { get; set; }
        public string? DuzgunVariant { get; set; }

        public string? DuzgunCavabMetni { get; set; }

        public string IzahMetni { get; set; } = "";

        public int Sira { get; set; }
    }
}

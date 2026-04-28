using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Maas
{
    public class FerdiHesablaInputDto
    {
        public int IsciId { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal BonusMeblegi { get; set; } = 0;
        public string? BonusAciqlama { get; set; }
        public decimal CerimeMeblegi { get; set; } = 0;
        public string? CerimeAciqlama { get; set; }

        // Bonusdan ayrı gəlir növü — bəzi işçilərdə ödənilir, vergiyə cəlb olunur
        // və brüt-ə bonus kimi əlavə edilir, lakin ayrıca xəttdə uçota alınır.
        public decimal FerqliGelirMeblegi { get; set; } = 0;
        public string? FerqliGelirAciqlama { get; set; }
    }

    public class FerdiElaveDto
    {
        public int IsciId { get; set; }
        public decimal BonusMeblegi { get; set; } = 0;
        public string? BonusAciqlama { get; set; }
        public decimal CerimeMeblegi { get; set; } = 0;
        public string? CerimeAciqlama { get; set; }
        public decimal FerqliGelirMeblegi { get; set; } = 0;
        public string? FerqliGelirAciqlama { get; set; }
    }
}
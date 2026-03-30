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
    }

    public class FerdiElaveDto
    {
        public int IsciId { get; set; }
        public decimal BonusMeblegi { get; set; } = 0;
        public string? BonusAciqlama { get; set; }
        public decimal CerimeMeblegi { get; set; } = 0;
        public string? CerimeAciqlama { get; set; }
    }
}
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Maas
{
    // Konfiqurasiyalı manual gəlir növü üçün işçi üzrə daxil edilən məbləğ
    public class ElaveGelirGirisi
    {
        public int NovId { get; set; }        // MaasNovu.Id (ManualGelir=true)
        public decimal Mebleg { get; set; }
    }

    public class FerdiHesablaInputDto
    {
        public int IsciId { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public decimal BonusMeblegi { get; set; } = 0;
        public string? BonusAciqlama { get; set; }
        public decimal CerimeMeblegi { get; set; } = 0;
        public string? CerimeAciqlama { get; set; }

        // 18.02.2016 tarixli IH-07 saylı əmrlə əlavə təminat — vergiyə cəlb olunur
        public decimal IH07Meblegi { get; set; } = 0;

        // VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər
        public decimal VM9821Meblegi { get; set; } = 0;

        // Konfiqurasiyalı manual gəlir növləri üzrə məbləğlər (vergi rejimi növün bayraqlarından)
        public List<ElaveGelirGirisi> ElaveGelirler { get; set; } = new();
    }

    public class FerdiElaveDto
    {
        public int IsciId { get; set; }
        public decimal BonusMeblegi { get; set; } = 0;
        public string? BonusAciqlama { get; set; }
        public decimal CerimeMeblegi { get; set; } = 0;
        public string? CerimeAciqlama { get; set; }
        // 18.02.2016 tarixli IH-07 saylı əmrlə əlavə təminat
        public decimal IH07Meblegi { get; set; } = 0;
        // VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər
        public decimal VM9821Meblegi { get; set; } = 0;
        // Konfiqurasiyalı manual gəlir növləri üzrə məbləğlər
        public List<ElaveGelirGirisi> ElaveGelirler { get; set; } = new();
    }
}

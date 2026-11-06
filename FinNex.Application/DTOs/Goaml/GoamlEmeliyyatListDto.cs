using FinNex.Domain.Entities.Goaml;

namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>Təsdiqlənmiş GOAML qeydlərinin siyahı sətri.</summary>
    public class GoamlEmeliyyatListDto
    {
        public int Id { get; set; }
        public DateTime DateOper { get; set; }
        public string? Debet { get; set; }
        public string? Kredit { get; set; }
        public decimal? SummaVNacval { get; set; }
        public GoamlTipi? Tip { get; set; }
        public GoamlHesabatNovu? ReportCode { get; set; }
        public GoamlEmeliyyatStatusu Status { get; set; }
        public string? TesdiqleyenAdi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
}

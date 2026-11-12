using FinNex.Domain.Entities.Goaml;

namespace FinNex.Application.DTOs.Goaml
{
    /// <summary>Təsdiqlənmiş GOAML qeydlərinin siyahı sətri.</summary>
    public class GoamlEmeliyyatListDto
    {
        public int Id { get; set; }

        /// <summary>Namizədin öz Oracle açarı (RECNUM) — "Əməliyyat №" (12.11.2026,
        /// istifadəçi tələbi: siyahıda əməliyyatın öz nömrəsi görünsün).</summary>
        public long CandidateRecNum { get; set; }

        public DateTime DateOper { get; set; }
        public string? Debet { get; set; }
        public string? Kredit { get; set; }
        public decimal? SummaVNacval { get; set; }
        public GoamlTipi? Tip { get; set; }
        public GoamlHesabatNovu? ReportCode { get; set; }

        /// <summary>İnsan-oxunan etiketlər — AJAX/JSON nəticələrdə (məs. ixrac
        /// önizləməsi) raw enum rəqəmi ÖZBAŞINA göndərilməsin deyə (13.11.2026,
        /// JSON serializasiyası enum-u defolt RƏQƏM kimi yazır, string ETMİR —
        /// Razor-un öz `@e.Tip.Value.Etiket()` çağırışının AJAX qarşılığı).
        /// Server-rendered Siyahi.cshtml bunlara ehtiyac duymur, öz Razor
        /// çağırışını saxlayır — bu sahələr yalnız AJAX istehlakçıları üçündür.</summary>
        public string? TipEtiketi { get; set; }
        public string? ReportCodeEtiketi { get; set; }

        public GoamlEmeliyyatStatusu Status { get; set; }
        public string? TesdiqleyenAdi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
}

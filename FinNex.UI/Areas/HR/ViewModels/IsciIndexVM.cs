using FinNex.Application.DTOs.HR.Isci;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class IsciIndexVM
    {
        public List<IsciListDto> Items { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages  { get; set; } = 1;
        public int TotalCount  { get; set; }

        // Tab counts (always whole-table, unaffected by search)
        public int AktivCount  { get; set; }
        public int CixmisCount { get; set; }

        // Active filter state
        public string  Tab    { get; set; } = "aktiv";
        public string? Search { get; set; }
    }
}

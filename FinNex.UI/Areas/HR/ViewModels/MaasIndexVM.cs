using FinNex.Application.DTOs.HR.Maas;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasIndexVM
    {
        public IEnumerable<MaasListDto> Items { get; set; } = new List<MaasListDto>();

        // Filters
        public string? SearchTerm { get; set; }
        public string? SobeFilter { get; set; }
        public int? IlFilter { get; set; }
        public int? AyFilter { get; set; }

        // Stats
        public decimal UmumiMaas { get; set; }
        public decimal UmumiElave { get; set; }
        public decimal UmumiCerime { get; set; }
        public int IsciSayi { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Dropdowns
        public List<SelectListItem> SobeLer { get; set; } = new();
    }
}

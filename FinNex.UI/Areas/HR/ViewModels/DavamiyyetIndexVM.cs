using FinNex.Application.DTOs.HR.Davamiyyet;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class DavamiyyetIndexVM
    {
        public IEnumerable<DavamiyyetListDto> Items { get; set; } = new List<DavamiyyetListDto>();

        // Filters
        public string? SearchTerm { get; set; }
        public string? SobeFilter { get; set; }
        public DateTime? BasTarix { get; set; }
        public DateTime? SonTarix { get; set; }
        public DavamiyyetStatus? StatusFilter { get; set; }

        // Stats
        public int IsdeSayi { get; set; }
        public int GecikmeliSayi { get; set; }
        public int QayibSayi { get; set; }
        public int UmumiSayi { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Dropdowns
        public List<SelectListItem> SobeLer { get; set; } = new();
    }
}

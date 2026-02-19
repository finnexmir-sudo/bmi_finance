using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MezuniyyetIndexVM
    {
        public IEnumerable<MezuniyyetListDto> Items { get; set; } = new List<MezuniyyetListDto>();

        // Filters
        public string? SearchTerm { get; set; }
        public string? SobeFilter { get; set; }
        public DateTime? BasTarix { get; set; }
        public DateTime? SonTarix { get; set; }
        public MezuniyyetStatus? StatusFilter { get; set; }
        public MezuniyyetNovu? NovFilter { get; set; }

        // Stats
        public int GozlemedeSayi { get; set; }
        public int TesdiqlenibSayi { get; set; }
        public int ImtinaEdilib { get; set; }
        public int UmumiSayi { get; set; }

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Dropdowns
        public List<SelectListItem> SobeLer { get; set; } = new();
    }
}

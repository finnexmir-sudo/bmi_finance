using FinNex.Application.DTOs.PR_Document;
using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.UI.Areas.PR_Document.ViewModels
{
    public class SenedSiyahiVM
    {
        public List<SenedListDto> Senedler { get; set; } = new();

        // Filters
        public int? SobeId { get; set; }
        public int? SenedNovuId { get; set; }
        public int? Status { get; set; }
        public string? AcarSoz { get; set; }

        // Pagination
        public int Sehife { get; set; } = 1;
        public int SehifeOlcusu { get; set; } = 10;
        public int UmumiSay { get; set; }
        public int UmumiSehife => (int)Math.Ceiling((double)UmumiSay / SehifeOlcusu);

        // Dropdown data
        public List<Sobe> Sobeler { get; set; } = new();
        public List<SenedNovu> SenedNovleri { get; set; } = new();
    }
}

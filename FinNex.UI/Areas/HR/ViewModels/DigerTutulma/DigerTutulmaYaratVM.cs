using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels.DigerTutulma
{
    public class DigerTutulmaYaratVM
    {
        public int IsciId { get; set; }
        public decimal Mebleg { get; set; }
        public int MuddetAy { get; set; } = 12;
        public int BaslamaIl { get; set; } = DateTime.Now.Year;
        public int BaslamaAy { get; set; } = DateTime.Now.Month;
        public string? Aciqlama { get; set; }

        public List<SelectListItem> Isciler { get; set; } = new();
    }
}

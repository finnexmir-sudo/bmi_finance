// Areas/User/ViewModels/Gorush/GorushYaratVM.cs
using FinNex.Domain.Entities.Communication;

namespace FinNex.UI.Areas.User.ViewModels.Gorush
{
    public class GorushYaratVM
    {
        public int TeshkilatciIsciId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Başlıq mütləqdir")]
        public string Bashliq { get; set; } = "";

        public string? Agenda { get; set; }
        public DateTime Tarix { get; set; } = DateTime.Today;
        public TimeSpan BaslamaSaati { get; set; } = TimeSpan.FromHours(10);
        public TimeSpan? BitisSaati { get; set; }
        public string? Yer { get; set; }
        public List<int> SecilmisIshtirakcilar { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> IsciList { get; set; } = new();
    }
   
}
// Areas/User/ViewModels/Gorush/GorushEditVM.cs
using FinNex.Domain.Entities.Communication;

 // Areas/User/ViewModels/Gorush/GorushEditVM.cs
    namespace FinNex.UI.Areas.User.ViewModels.Gorush
    {
        public class GorushEditVM
        {
            public int Id { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Başlıq mütləqdir")]
            public string Bashliq { get; set; } = "";
            public string? Agenda { get; set; }
            public DateTime Tarix { get; set; }
            public TimeSpan BaslamaSaati { get; set; }
            public TimeSpan? BitisSaati { get; set; }
            public string? Yer { get; set; }
            public List<int> SecilmisIshtirakcilar { get; set; } = new();
            public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> IsciList { get; set; } = new();
        }
    }

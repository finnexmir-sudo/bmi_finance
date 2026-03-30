// Areas/User/ViewModels/Tapshiriq/TapshiriqEditVM.cs
using FinNex.Domain.Entities.Communication;


    // Areas/User/ViewModels/Tapshiriq/TapshiriqEditVM.cs
    namespace FinNex.UI.Areas.User.ViewModels.Tapshiriq
    {
        public class TapshiriqEditVM
        {
            public int Id { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Başlıq mütləqdir")]
            [System.ComponentModel.DataAnnotations.MaxLength(200)]
            public string Bashliq { get; set; } = "";
            public string? Tesvir { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "İşçi seçilməlidir")]
            public int TeyinOlunanIsciId { get; set; }
            public DateTime? SonTarix { get; set; }
            public TapshiriqPrioritet Prioritet { get; set; } = TapshiriqPrioritet.Orta;
            public string? Qeyd { get; set; }

            public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> IsciList { get; set; } = new();
        }
    }

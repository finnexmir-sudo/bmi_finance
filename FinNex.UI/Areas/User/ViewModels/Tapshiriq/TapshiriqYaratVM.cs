// Areas/User/ViewModels/Tapshiriq/TapshiriqYaratVM.cs
using FinNex.Domain.Entities.Communication;

namespace FinNex.UI.Areas.User.ViewModels.Tapshiriq
{
    public class TapshiriqYaratVM
    {
        public int YaradanIsciId { get; set; }

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
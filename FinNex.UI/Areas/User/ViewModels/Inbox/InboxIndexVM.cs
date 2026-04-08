// Areas/User/ViewModels/Inbox/InboxIndexVM.cs
using FinNex.Application.DTOs.Communication;

namespace FinNex.UI.Areas.User.ViewModels.Inbox
{
    public class InboxIndexVM
    {
        public string AktivTab { get; set; } = "bildirisler";
        public List<BildirisDto> Bildirisler { get; set; } = new();
        public List<MesajListDto> GelenMesajlar { get; set; } = new();
        public List<MesajListDto> GonderilenMesajlar { get; set; } = new();
        public List<EvezediciTesdiqDto> EvezediciSorgular { get; set; } = new();

        public int OxunmamisBildiris => Bildirisler.Count(x => !x.Oxunub);
        public int OxunmamisMesaj => GelenMesajlar.Count(x => !x.Oxunub);
        public int GozleyenSorgu => EvezediciSorgular.Count;
        public int CemiOxunmamis => OxunmamisBildiris + OxunmamisMesaj + GozleyenSorgu;
    }

    public class MesajDetailPageVM
    {
        public MesajDetailDto Mesaj { get; set; } = null!;
        public MesajCreateDto CavabDto { get; set; } = new();
    }

    public class YeniMesajVM
    {
        public int GonderenIsciId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Alan seçilməlidir")]
        public int AlanIsciId { get; set; }

        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string Movzu { get; set; } = "";

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Mətn yazılmalıdır")]
        public string Metn { get; set; } = "";

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> IsciList { get; set; } = new();
    }
}
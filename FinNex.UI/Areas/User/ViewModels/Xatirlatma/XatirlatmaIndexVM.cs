// Areas/User/ViewModels/Xatirlatma/XatirlatmaVMs.cs
using FinNex.Application.DTOs.Communication;
using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.User.ViewModels.Xatirlatma
{
    public class XatirlatmaIndexVM
    {
        public List<XatirlatmaListDto> Xatirlatmalar { get; set; } = new();

        // Qruplaşdırma üçün köməkçi
        public List<XatirlatmaListDto> Bugunkular =>
    Xatirlatmalar.Where(x => x.XatirlatmaTarixi.Date == DateTime.Today && !x.OxunubMu).ToList();

        public List<XatirlatmaListDto> Gozleyenler =>
            Xatirlatmalar.Where(x => x.XatirlatmaTarixi.Date > DateTime.Today && !x.OxunubMu).ToList();

        public List<XatirlatmaListDto> Kecmisler =>
            Xatirlatmalar.Where(x => x.XatirlatmaTarixi.Date < DateTime.Today && !x.OxunubMu).ToList();

        public List<XatirlatmaListDto> Oxunmuslar =>
            Xatirlatmalar.Where(x => x.OxunubMu).ToList();
    }

    public class XatirlatmaYaratVM
    {
        [Required(ErrorMessage = "Başlıq mütləqdir.")]
        [StringLength(200, ErrorMessage = "Maksimum 200 simvol.")]
        public string Bashliq { get; set; } = null!;

        [StringLength(1000)]
        public string? Qeyd { get; set; }

        [Required(ErrorMessage = "Tarix mütləqdir.")]
        public DateTime XatirlatmaTarixi { get; set; }
    }
}
// Areas/User/ViewModels/Tapshiriq/TapshiriqIndexVM.cs
using FinNex.Application.DTOs.Communication;
using FinNex.Domain.Entities.Communication;

namespace FinNex.UI.Areas.User.ViewModels.Tapshiriq
{
    public class TapshiriqIndexVM
    {
        public string AktivTab { get; set; } = "menim";
        public List<TapshiriqListDto> MenimTapshiriqlar { get; set; } = new();
        public List<TapshiriqListDto> VerdiklerimTapshiriqlar { get; set; } = new();
        public List<GelenMailTapshiriqDto> MailTapshiriqlari { get; set; } = new();

        public int YeniSayi => MenimTapshiriqlar.Count(x =>
            x.Status == TapshiriqStatus.Yeni);
        public int DavamEdirSayi => MenimTapshiriqlar.Count(x =>
            x.Status == TapshiriqStatus.DavamEdir);
        public int GecikmishSayi => MenimTapshiriqlar.Count(x => x.Gecikibmi);
    }

}
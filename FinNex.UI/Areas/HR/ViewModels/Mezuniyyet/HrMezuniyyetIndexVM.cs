using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.UI.Areas.HR.ViewModels.Mezuniyyet
{
    public class HrMezuniyyetIndexVM
    {
        public List<MezuniyyetListDto> Mezuniyyetler { get; set; } = new();
        public string PageTitle { get; set; } = "";
        public string TesdiqAction { get; set; } = ""; // POST action adı

        // HR panelinin tab rejimi: "tesdiq" (default) | "proses"
        public string AktivTab { get; set; } = "tesdiq";

        // Tab sayğacları
        public int TesdiqSayi { get; set; }
        public int ProsesSayi { get; set; }

        // "Prosesdə" tabı üçün — siyahı read-only göstərilməlidir
        public bool YalnizIzleme => AktivTab == "proses";
    }
    public class HrMezuniyyetDetalVM
    {
        public MezuniyyetDto Mezuniyyet { get; set; } = null!;
        public string ReturnAction { get; set; } = "Hr"; // geri qayıdacaq action

        // Təsdiqçinin görməsi üçün əlavə kontekst
        public List<MezuniyyetOverlapDto> OverlapMezuniyyetler { get; set; } = new();
        public List<EvezediciKonfliktDto> EvezediciKonfliktleri { get; set; } = new();
    }
}

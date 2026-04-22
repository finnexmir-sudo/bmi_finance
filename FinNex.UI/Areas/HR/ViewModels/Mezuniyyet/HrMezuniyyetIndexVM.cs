using FinNex.Application.DTOs.HR.Mezuniyyet;

namespace FinNex.UI.Areas.HR.ViewModels.Mezuniyyet
{
    public class HrMezuniyyetIndexVM
    {
        public List<MezuniyyetListDto> Mezuniyyetler { get; set; } = new();
        public string PageTitle { get; set; } = "";
        public string TesdiqAction { get; set; } = ""; // POST action adı

        // HR panelinin tab rejimi: "tesdiq" (default) | "proses" | "tarixce"
        public string AktivTab { get; set; } = "tesdiq";

        // Tab sayğacları
        public int TesdiqSayi { get; set; }
        public int ProsesSayi { get; set; }
        public int TarixceSayi { get; set; }

        // "Prosesdə" və "Tarixçə" tabları üçün — siyahı read-only göstərilir
        public bool YalnizIzleme => AktivTab == "proses" || AktivTab == "tarixce";

        // Axtarış üçün (istəyə görə GET-dən gəlir)
        public string? Axtaris { get; set; }
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

using FinNex.Application.Interfaces.Emeliyyat;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

public class PulKocurmeController : KocurmeControllerBase
{
    // 20 000 USD limitinin sənəd növləri — bazaya ötürülür (07.09.2026).
    // ⚠️ `KocurmeControllerBase`-dən törəyən YEGANƏ controller budur
    // (`TelebeKocurmeController` ayrıdır, `ITelebeKocurmeService` işlədir) —
    // yəni limit məntiqi praktikada yalnız «Pul» axınında işə düşür.
    public PulKocurmeController(IKocurmeService service, IWebHostEnvironment env,
                                IKocurmeSenedNovuService senedNovu)
        : base(service, env, senedNovu) { }

    protected override string Novu => "Pul";
    protected override string Baslik => "Pul köçürməsi";
}

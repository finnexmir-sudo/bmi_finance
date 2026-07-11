using FinNex.Application.Interfaces.Emeliyyat;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

public class TelebeKocurmeController : KocurmeControllerBase
{
    public TelebeKocurmeController(IKocurmeService service) : base(service) { }

    protected override string Novu => "Telebe";
    protected override string Baslik => "Tələbə köçürməsi";
}

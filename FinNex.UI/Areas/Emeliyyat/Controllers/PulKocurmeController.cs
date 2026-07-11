using FinNex.Application.Interfaces.Emeliyyat;

namespace FinNex.UI.Areas.Emeliyyat.Controllers;

public class PulKocurmeController : KocurmeControllerBase
{
    public PulKocurmeController(IKocurmeService service, IWebHostEnvironment env) : base(service, env) { }

    protected override string Novu => "Pul";
    protected override string Baslik => "Pul köçürməsi";
}

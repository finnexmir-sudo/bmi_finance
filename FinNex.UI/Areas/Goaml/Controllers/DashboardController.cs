using FinNex.Domain;
using FinNex.UI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.Goaml.Controllers;

// GOAML — Risk/AML şübhəli əməliyyat hesabatı modulu (05.11.2026, iş başlanğıcı).
// BMI-nin köhnə Oracle Forms "AYSEL/GOAML" ekranının FinNex-ə köçürülməsi —
// hələ yalnız qabıq: giriş nöqtəsi + icazə. "Əməliyyatlar" və qalan 13 tab
// (Risk, Keçməyən əməliyyat, Hesab risk, Risk Progress, Cash IN, Card To Card,
// 18% 4%, Setup, SİYAHI, kassa >30, Təhlil, MMX, SCORING, Export to file)
// ekran-ekran təsdiqləndikcə əlavə olunacaq.
//
// İcazə QƏSDƏN Risk-in özündən AYRIDIR (Risk Area-nın controller-lərində hazırda
// HEÇ BİR rol/icazə şərti yoxdur — istənilən giriş etmiş işçi görə bilir). GOAML
// həssas şəxsi məlumat (sənəd №, PİN, ünvan) daşıdığı üçün öz, dar icazəsi var.
[Area("Goaml")]
[Authorize]
[Icaze(IcazeKodlari.GoamlBax)]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "GOAML";
        return View();
    }
}

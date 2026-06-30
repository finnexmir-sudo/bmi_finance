using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    // "Sistem necə işləyir" — daxili bələdçi / bilik bazası.
    // Sistemin müxtəlif modullarının real davranışını izah edən statik sənəd səhifəsi.
    // Məqsəd: HR/Admin lazım olanda buradan oxusun. Yeni bölmələr birbaşa
    // Views/Sistem/NeceIsleyir.cshtml faylına əlavə olunur (DB tələb olunmur).
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class SistemController : Controller
    {
        public IActionResult NeceIsleyir() => View();
    }
}

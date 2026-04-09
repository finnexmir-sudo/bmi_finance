using FinNex.Domain;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class ElanController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ElanController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── GET /User/Elan ──────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Elanlar";

        var elanlar = await _unitOfWork.Repository<Elan>()
            .Query()
            .Where(x => !x.Silinib && x.Aktivdir
                && (x.BitirmeTarixi == null || x.BitirmeTarixi > DateTime.Now))
            .Include(x => x.GonderenIsci)
            .OrderByDescending(x => x.Vacibdir)
            .ThenByDescending(x => x.YaradilmaTarixi)
            .ToListAsync();

        return View(elanlar);
    }
}

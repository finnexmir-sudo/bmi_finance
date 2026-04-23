using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.User.Controllers;

// Bütün işçilərin qeyri iş günləri təqvimini oxu rejimində göstərir.
// Redaktə / əlavə / silmə — yalnız HR/Admin/Rehber (ora /HR/BayramGunu).
[Area("User")]
[Authorize]
public class TakvimController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public TakvimController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET /User/Takvim
    public IActionResult Index()
    {
        ViewData["Title"] = "Qeyri iş günləri təqvimi";
        return View();
    }

    // GET /User/Takvim/Year?year=2026
    // Public (istənilən login olmuş işçi üçün) — yalnız oxu.
    [HttpGet]
    public async Task<IActionResult> Year(int year)
    {
        var baslangic = new DateTime(year, 1, 1);
        var bitis = new DateTime(year, 12, 31);

        var list = await _unitOfWork.Repository<BayramGunu>()
            .HamisiniGetirAsync(x => !x.Silinib && x.Tarix >= baslangic && x.Tarix <= bitis);

        var records = list.Select(x => new
        {
            id = x.Id,
            tarix = x.Tarix.ToString("yyyy-MM-dd"),
            ad = x.Ad,
            tip = (int)x.Tip
        });

        return Json(new
        {
            success = true,
            year,
            today = DateTime.Today.ToString("yyyy-MM-dd"),
            records
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using FinNex.Domain.Interfaces;
using FinNex.Domain.Entities.Structure;

namespace FinNex.UI.Controllers;

public class DepartmentController : Controller
{
    private readonly IUnitOfWork _uow;

    public DepartmentController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _uow.Repository<Department>().HamisiniGetirAsync();
        return View(list);
    }

    public IActionResult Yarat() => View();

    [HttpPost]
    public async Task<IActionResult> Yarat(Department model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _uow.Repository<Department>().YaratAsync(model);
        await _uow.YaddaSaxlaAsync();

        return RedirectToAction(nameof(Index));
    }
}

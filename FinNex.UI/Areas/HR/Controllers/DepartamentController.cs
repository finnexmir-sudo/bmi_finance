using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Vezife;
using FinNex.Application.DTOs.Structur;
using FinNex.Application.Interfaces.Structur;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    public class DepartamentController : Controller
    {
        private readonly IDepartmentService _departamentService;

        public DepartamentController(IDepartmentService departamentService)
        {
            _departamentService = departamentService;
        }

        public async Task< IActionResult> Index()
        {
            var result = await _departamentService.GetAllWithEmployeeCountAsync();
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new List<DepartmentListDto>());
            }
            return View(result.Data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(DepartmentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _departamentService.YaratAsync(dto);

            return RedirectToAction(nameof(Index));
        }

    }
}

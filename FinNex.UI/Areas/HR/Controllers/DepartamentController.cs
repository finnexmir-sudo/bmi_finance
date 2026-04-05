// Areas/HR/Controllers/DepartamentController.cs
using FinNex.Application.DTOs.Structur;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
    public class DepartamentController : Controller
    {
        private readonly IDepartmentService _departamentService;

        public DepartamentController(IDepartmentService departamentService)
        {
            _departamentService = departamentService;
        }

        // ── GET: /HR/Departament ─────────────────────────
        public async Task<IActionResult> Index()
        {
            var result = await _departamentService.GetAllWithEmployeeCountAsync();
            ViewData["Title"] = "Departamentlər";
            return View(result.Success ? result.Data : new List<DepartmentListDto>());
        }

        // ── GET: /HR/Departament/Create ──────────────────
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Yeni Departament";
            return View(new DepartmentCreateDto());
        }

        // ── POST: /HR/Departament/Create ─────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var result = await _departamentService.YaratAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── POST: /HR/Departament/Edit ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string Ad, string? Aciqlama)
        {
            if (string.IsNullOrWhiteSpace(Ad))
            {
                TempData["Error"] = "Departament adı boş ola bilməz.";
                return RedirectToAction(nameof(Index));
            }

            var dto = new DepartmentUpdateDto
            {
                Id = id,
                Ad = Ad.Trim(),
                Aciqlama = Aciqlama?.Trim()
            };

            var result = await _departamentService.YenileAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // ── POST: /HR/Departament/Delete ─────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // İşçi sayını yoxla
            var listResult = await _departamentService.GetAllWithEmployeeCountAsync();
            if (listResult.Success && listResult.Data != null)
            {
                var dept = listResult.Data.FirstOrDefault(x => x.Id == id);
                if (dept != null && dept.IsciSayi > 0)
                {
                    TempData["Error"] = $"\"{dept.Ad}\" departamentinə bağlı {dept.IsciSayi} işçi var. Əvvəlcə işçiləri köçürün.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var result = await _departamentService.SilAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
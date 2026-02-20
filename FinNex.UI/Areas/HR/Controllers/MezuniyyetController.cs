using FinNex.Application.DTOs.HR;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize] // Yalnız giriş edənlər üçün
    public class MezuniyyetController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;

        public MezuniyyetController(IMezuniyyetService mezuniyyetService)
        {
            _mezuniyyetService = mezuniyyetService;
        }

        #region Sayfalar (Views)

        // Bütün müraciətlərin siyahısı (Admin/HR üçün)
        public async Task<IActionResult> Index()
        {
            var result = await _mezuniyyetService.GetListAsync();
            return View(result.Data);
        }

        // Yeni məzuniyyət müraciəti səhifəsi
        public IActionResult Yarat()
        {
            return View();
        }

        // Müraciətin detalı səhifəsi
        public async Task<IActionResult> Detal(int id)
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success) return NotFound();

            return View(result.Data);
        }

        #endregion

        #region API İşlemleri (POST)

        // İşçinin müraciət göndərməsi
        [HttpPost]
        [ValidateAntiForgeryToken] // Təhlükəsizlik üçün
        public async Task<IActionResult> Yarat(MezuniyyetCreateDto dto)
        {
            if (!ModelState.IsValid)
                return Json(new { isSuccess = false, message = "Məlumatlar düzgün daxil edilməyib." });

            var result = await _mezuniyyetService.YaratAsync(dto);
            return Json(result);
        }

        // 1. Mərhələ: Şöbə rəisinin təsdiqi
        [HttpPost]
       

      
        // Müraciəti ləğv etmək (İşçi hələ təsdiq olunmamış fikrini dəyişərsə)
        [HttpPost]
        public async Task<IActionResult> Sil(int id)
        {
            var result = await _mezuniyyetService.SilAsync(id);
            return Json(result);
        }

        #endregion
    }
}
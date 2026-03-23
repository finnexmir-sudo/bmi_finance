using FinNex.Application.DTOs.HR;
using FinNex.Application.DTOs.HR.Isci;
using FinNex.Application.DTOs.HR.Mezuniyyet;
using FinNex.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize] // Yalnız giriş edənlər üçün
    public class MezuniyyetController : Controller
    {
        private readonly IMezuniyyetService _mezuniyyetService;
        private readonly IIsciService _isciService;

        public MezuniyyetController(IMezuniyyetService mezuniyyetService, IIsciService isciService)
        {
            _mezuniyyetService = mezuniyyetService;
            _isciService = isciService;
        }

        #region Sayfalar (Views)

        // Bütün müraciətlərin siyahısı (Admin/HR üçün)
        public async Task<IActionResult> Index()
        {
            var result = await _mezuniyyetService.GetListAsync();
            return View(result.Data);
        }

        // Yeni məzuniyyət müraciəti səhifəsi


        // Müraciətin detalı səhifəsi
        public async Task<IActionResult> Detal(int id)
        {
            var result = await _mezuniyyetService.IdIleGetirAsync(id);
            if (!result.Success) return NotFound();

            return View(result.Data);
        }

        #endregion

        #region API İşlemleri (POST)

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var isciler = await _isciService.HamisiniGetirAsync();
            if (isciler == null) return NotFound();

            ViewBag.IsciList = new SelectList(isciler?.Data ?? new List<IsciListDto>(), "Id", "TamAd");
            ViewBag.EvezEdenIsciList = new SelectList(isciler?.Data ?? new List<IsciListDto>(), "Id", "TamAd");

            

            return View();
        }

        // İşçinin müraciət göndərməsi
        [HttpPost]
        [ValidateAntiForgeryToken] // Təhlükəsizlik üçün
        public async Task<IActionResult> Create(MezuniyyetCreateDto dto)
        {
            if (!ModelState.IsValid)
                return Json(new { isSuccess = false, message = "Məlumatlar düzgün daxil edilməyib." });

            var result = await _mezuniyyetService.YaratAsync(dto);
            return Json(result);
        }

        // 1. Mərhələ: Şöbə rəisinin təsdiqi
        [HttpPost]
        public async Task<IActionResult> SobeReisiTesdiq(int id, bool status, string? qeyd)
        {
            // Köhnə SobeReisiTesdiqAsync əvəzinə yeni adı çağır:
            var result = await _mezuniyyetService.SobeReisiTesdiqAsync(id, status, qeyd);
            return Json(result);
        }



        // 2. Mərhələ: Rəhbərin təsdiqi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RehberTesdiq(int id, bool status, string? qeyd)
        {
            var result = await _mezuniyyetService.RehberTesdiqAsync(id, status, qeyd);
            return Json(result);
        }

        // 3. Mərhələ: HR-ın təsdiqi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HrTesdiq(int id, bool status, string? qeyd)
        {
            var result = await _mezuniyyetService.HrTesdiqAsync(id, status, qeyd);
            return Json(result);
        }

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
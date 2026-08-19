using FinNex.Application.DTOs.Avtopark;
using FinNex.Application.Interfaces.Avtopark;
using FinNex.Application.Interfaces.Structur;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.Avtopark.Controllers
{
    /// <summary>
    /// Maşın kartı (CRUD) — Admin / təsərrüfat.
    /// </summary>
    [Area("Avtopark")]
    [Authorize(Roles = RoleNames.Admin)]
    public class MasinController : AvtoparkControllerBase
    {
        private readonly IMasinService _masin;
        private readonly IMasinMuddetService _muddet;
        private readonly IDepartmentService _departament;
        private readonly IIsciService _isci;

        public MasinController(
            IMasinService masin,
            IMasinMuddetService muddet,
            IDepartmentService departament,
            IIsciService isci,
            UserManager<AppUser> userManager) : base(userManager)
        {
            _masin = masin;
            _muddet = muddet;
            _departament = departament;
            _isci = isci;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _masin.HamisiniGetirAsync();
            return View(list);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var m = await _masin.GetirAsync(id);
            if (m == null) return NotFound();

            // Bu maşının bütün müddət qeydləri — tarixçə ilə birlikdə
            // (`yalnizAktiv: false`), çünki kartda «nə vaxt uzadılıb» görünsün.
            ViewBag.Muddetler = await _muddet.HamisiniGetirAsync(id, yalnizAktiv: false);
            return View(m);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await DoldurAsync();
            return View(new MasinCreateDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MasinCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                await DoldurAsync();
                return View(dto);
            }

            var res = await _masin.YaratAsync(dto, GetUserId());
            if (!res.Success)
            {
                // TempData YOX — layout onu `.fn-alert` kimi göstərir və
                // `user-area.js` 4 saniyəyə silir; istifadəçi səbəbi oxumağa
                // macal tapmır (CLAUDE.md — «submit heç nə etmir» tələsi).
                // ModelState isə qalıcı validasiya xülasəsində qalır.
                ModelState.AddModelError(string.Empty, res.Message ?? "Əməliyyat alınmadı.");
                await DoldurAsync();
                return View(dto);
            }

            TempData["Success"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _masin.GetirAsync(id);
            if (m == null) return NotFound();

            await DoldurAsync();
            return View(new MasinCreateDto
            {
                Id = m.Id,
                DovletNomresi = m.DovletNomresi,
                Marka = m.Marka,
                Model = m.Model,
                BuraxilisIli = m.BuraxilisIli,
                Reng = m.Reng,
                Ban = m.Ban,
                Vin = m.Vin,
                Novu = m.Novu,
                DepartamentId = m.DepartamentId,
                TehkimSurucuId = m.TehkimSurucuId,
                Status = m.Status,
                Qeyd = m.Qeyd
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MasinCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                await DoldurAsync();
                return View(dto);
            }

            var res = await _masin.YenileAsync(dto, GetUserId());
            if (!res.Success)
            {
                // TempData YOX — layout onu `.fn-alert` kimi göstərir və
                // `user-area.js` 4 saniyəyə silir; istifadəçi səbəbi oxumağa
                // macal tapmır (CLAUDE.md — «submit heç nə etmir» tələsi).
                // ModelState isə qalıcı validasiya xülasəsində qalır.
                ModelState.AddModelError(string.Empty, res.Message ?? "Əməliyyat alınmadı.");
                await DoldurAsync();
                return View(dto);
            }

            TempData["Success"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _masin.SilAsync(id, GetUserId());
            TempData[res.Success ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Departament və sürücü açılan siyahıları.
        /// İşçi siyahısında layihə qaydası tətbiq olunur: aktiv işçilər,
        /// `Sira → Ad → Soyad` sırası (CLAUDE.md — «İşçi Siyahıları»).
        /// </summary>
        private async Task DoldurAsync()
        {
            var dept = await _departament.HamisiniGetirAsync();
            ViewBag.Departamentler = dept.Success && dept.Data != null
                ? dept.Data.Select(d => new SelectListItem(d.Ad, d.Id.ToString())).ToList()
                : new List<SelectListItem>();

            var isciler = await _isci.HamisiniGetirAsync(
                x => x.Status == IsciStatus.Aktiv && !x.Silinib, izlemeden: true);

            // `IsciListDto`-da ad/soyad ayrı sahə deyil — hazır `TamAd` var.
            ViewBag.Surucular = isciler.Success && isciler.Data != null
                ? isciler.Data
                    .OrderBy(x => x.Sira).ThenBy(x => x.TamAd)
                    .Select(x => new SelectListItem(x.TamAd, x.Id.ToString()))
                    .ToList()
                : new List<SelectListItem>();
        }
    }
}

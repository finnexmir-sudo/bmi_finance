using FinNex.Application.Interfaces.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class IsciAyliqQazancController : Controller
    {
        private readonly IIsciAyliqQazancService _service;
        private readonly IUnitOfWork _unitOfWork;

        public IsciAyliqQazancController(IIsciAyliqQazancService service, IUnitOfWork unitOfWork)
        {
            _service = service;
            _unitOfWork = unitOfWork;
        }

        // ── GET /HR/IsciAyliqQazanc?isciId=5 ─────────────────────
        public async Task<IActionResult> Index(int? isciId)
        {
            // İşçi siyahısını gətir
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .OrderBy(x => x.Soyad).ThenBy(x => x.Ad)
                .Select(x => new { x.Id, x.Ad, x.Soyad })
                .ToListAsync();

            ViewBag.Isciler = isciler.Select(x => new SelectListItem(
                $"{x.Soyad} {x.Ad}",
                x.Id.ToString(),
                x.Id == isciId)).ToList();

            ViewBag.SecilmisIsciId = isciId;

            if (!isciId.HasValue)
            {
                ViewBag.Qazanclar = new List<FinNex.Application.DTOs.HR.Maas.IsciAyliqQazancDto>();
                ViewBag.Cemi = 0m;
                ViewBag.SecilmisIsciAd = "";
                return View();
            }

            var result = await _service.GetByIsciAsync(isciId.Value);
            ViewBag.Qazanclar = result.Success ? result.Data : new List<FinNex.Application.DTOs.HR.Maas.IsciAyliqQazancDto>();
            ViewBag.Cemi = ((List<FinNex.Application.DTOs.HR.Maas.IsciAyliqQazancDto>)ViewBag.Qazanclar).Sum(x => x.Qazanc);

            var isci = isciler.FirstOrDefault(x => x.Id == isciId);
            ViewBag.SecilmisIsciAd = isci != null ? $"{isci.Soyad} {isci.Ad}" : "";

            ViewData["Title"] = "İşçi Aylıq Qazanc Tarixçəsi";
            return View();
        }

        // ── POST /HR/IsciAyliqQazanc/Save ────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int isciId, int il, int ay, decimal qazanc, string? qeyd)
        {
            if (qazanc < 0)
            {
                TempData["Error"] = "Qazanc mənfi ola bilməz.";
                return RedirectToAction(nameof(Index), new { isciId });
            }

            var result = await _service.AddOrUpdateAsync(isciId, il, ay, qazanc, elIle: true, qeyd: qeyd);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { isciId });
        }

        // ── POST /HR/IsciAyliqQazanc/Delete ──────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int isciId)
        {
            var result = await _service.DeleteAsync(id);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index), new { isciId });
        }
    }
}

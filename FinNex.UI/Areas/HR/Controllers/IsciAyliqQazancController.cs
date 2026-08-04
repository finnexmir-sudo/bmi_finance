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

        // ═════════════════ QAZANC MATRİSİ (il üzrə işçi × 12 ay) ═════════════════
        // Mühasibin Excel cədvəli ilə üz-üzə müqayisə: sətir = işçi (IsciID görünür),
        // sütun = ay. Xanaya klik → yerində düzəliş (əl ilə qeyd kimi saxlanır və
        // sistem sonradan avtomatik üstələmir). Excel çıxarışı ƏDƏDİ xanalarla.

        private async Task<FinNex.UI.Areas.HR.ViewModels.QazancMatrisVM> MatrisQurAsync(int il)
        {
            var qazanclar = await _unitOfWork.Repository<IsciAyliqQazanc>()
                .Query().AsNoTracking()
                .Where(x => !x.Silinib && x.Il == il)
                .Select(x => new { x.IsciId, x.Ay, x.Qazanc, x.ElIleDaxilEdilib, x.Qeyd })
                .ToListAsync();
            var qeydliIds = qazanclar.Select(x => x.IsciId).Distinct().ToHashSet();

            // Aktiv işçilər + həmin ildə qeydi olan (işdən çıxmışlar daxil) işçilər.
            // Sıralama — kanonik qayda (CLAUDE.md): Sira → Ad → Soyad.
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query().AsNoTracking()
                .Where(x => !x.Silinib &&
                            (x.Status == IsciStatus.Aktiv || qeydliIds.Contains(x.Id)))
                .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
                .Select(x => new { x.Id, x.Ad, x.Soyad, x.Status })
                .ToListAsync();

            var vm = new FinNex.UI.Areas.HR.ViewModels.QazancMatrisVM { Il = il };
            foreach (var i in isciler)
            {
                var setir = new FinNex.UI.Areas.HR.ViewModels.QazancMatrisSetirVM
                {
                    IsciId = i.Id,
                    AdSoyad = $"{i.Ad} {i.Soyad}",
                    Aktiv = i.Status == IsciStatus.Aktiv
                };
                foreach (var q in qazanclar.Where(x => x.IsciId == i.Id))
                    setir.Aylar[q.Ay] = new FinNex.UI.Areas.HR.ViewModels.QazancHucreVM
                    {
                        Qazanc = q.Qazanc,
                        ElIle = q.ElIleDaxilEdilib,
                        Qeyd = q.Qeyd
                    };
                vm.Setirler.Add(setir);
            }
            return vm;
        }

        // ── GET /HR/IsciAyliqQazanc/Matris?il=2026 ───────────────
        public async Task<IActionResult> Matris(int? il)
        {
            var secilmisIl = il ?? DateTime.Today.Year;
            ViewData["Title"] = $"Aylıq Qazanc Matrisi — {secilmisIl}";
            return View(await MatrisQurAsync(secilmisIl));
        }

        // ── POST /HR/IsciAyliqQazanc/MatrisSave (AJAX) ───────────
        // Xanadakı düzəliş: əl ilə qeyd kimi saxlanır (avtomatik sync üstələmir).
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MatrisSave(int isciId, int il, int ay, decimal qazanc)
        {
            if (ay < 1 || ay > 12)
                return Json(new { success = false, message = "Ay 1–12 aralığında olmalıdır." });
            if (qazanc < 0)
                return Json(new { success = false, message = "Qazanc mənfi ola bilməz." });

            var result = await _service.AddOrUpdateAsync(isciId, il, ay, qazanc,
                elIle: true, qeyd: $"Matris səhifəsindən düzəliş {DateTime.Now:dd.MM.yyyy HH:mm}");
            return Json(new { success = result.Success, message = result.Message });
        }

        // ── GET /HR/IsciAyliqQazanc/MatrisExcel?il=2026 ──────────
        // Rəqəmlər ƏDƏDİ xana kimi yazılır (ClosedXML double) — Excel-də
        // sum/düstur birbaşa işləyir, mətn problemi yoxdur.
        public async Task<IActionResult> MatrisExcel(int il)
        {
            var vm = await MatrisQurAsync(il);

            var basliqlar = new List<string> { "IsciID", "İşçi" };
            string[] ayAdlari = { "Yan", "Fev", "Mar", "Apr", "May", "İyn", "İyl", "Avq", "Sen", "Okt", "Noy", "Dek" };
            for (int a = 1; a <= 12; a++) basliqlar.Add($"{il}-{a:00} {ayAdlari[a - 1]}");
            basliqlar.Add("Cəmi");

            var setirler = new List<object?[]>();
            foreach (var s in vm.Setirler)
            {
                var r = new object?[15];
                r[0] = s.IsciId;
                r[1] = s.AdSoyad + (s.Aktiv ? "" : " (çıxıb)");
                for (int a = 1; a <= 12; a++)
                    r[1 + a] = s.Aylar.TryGetValue(a, out var h) ? h.Qazanc : (decimal?)null;
                r[14] = s.Cemi;
                setirler.Add(r);
            }

            // Ay üzrə CƏMİ sətri
            var cem = new object?[15];
            cem[1] = "CƏMİ";
            for (int a = 1; a <= 12; a++)
                cem[1 + a] = vm.Setirler.Sum(s => s.Aylar.TryGetValue(a, out var h) ? h.Qazanc : 0m);
            cem[14] = vm.Setirler.Sum(s => s.Cemi);
            setirler.Add(cem);

            var bytes = FinNex.UI.Helpers.ExcelExportHelper.Yarat($"Qazanc {il}", basliqlar.ToArray(), setirler);
            return File(bytes, FinNex.UI.Helpers.ExcelExportHelper.ContentType, $"Qazanc_Matris_{il}.xlsx");
        }
    }
}

using System.Text.Json;
using FinNex.Application.Services.HR;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    /// <summary>
    /// İşçilərin ümumi iş stajı idarəetməsi.
    /// Total staj = bu bankda staj (IsheQebulTarixi → bu gün) + əvvəlki iş dövrləri (kitabçə).
    /// Əvvəlki dövrlər Isci.EvvelkiStajPeriodlari (JSON) sahəsində saxlanılır.
    /// </summary>
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class IsciStajiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public IsciStajiController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET /HR/IsciStaji
        public async Task<IActionResult> Index(string? axtaris = null)
        {
            var q = _unitOfWork.Repository<Isci>().Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv);

            if (!string.IsNullOrWhiteSpace(axtaris))
            {
                var t = axtaris.Trim();
                q = q.Where(x => x.Ad.Contains(t) || x.Soyad.Contains(t)
                              || (x.FIN != null && x.FIN.Contains(t)));
            }

            var isciler = await q
                .Include(x => x.IsciTeyinatlari.Where(t => !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
                .ToListAsync();

            ViewBag.Axtaris = axtaris;
            ViewData["Title"] = "İşçi Stajı";
            return View(isciler);
        }

        // POST /HR/IsciStaji/Update
        // Əvvəlki iş dövrlərini (kitabçə) qəbul edir, JSON kimi saxlayır,
        // total stajı (bank + kitabçə) hesablayır və geriyə qaytarır.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int isciId, string? periodlar)
        {
            try
            {
                var isci = await _unitOfWork.Repository<Isci>().IdIleGetirAsync(isciId);
                if (isci == null) return Json(new { success = false, message = "İşçi tapılmadı." });

                // periodlar: JSON array `[{"s":"...","e":"..."}, ...]` və ya null/boş
                if (string.IsNullOrWhiteSpace(periodlar))
                {
                    isci.EvvelkiStajPeriodlari = null;
                }
                else
                {
                    // Validate: parse edilirmi
                    try
                    {
                        var liste = JsonSerializer.Deserialize<List<IsciStajHelper.Period>>(periodlar);
                        // Boş və ya yararsız dövrləri at
                        var temizlenmis = liste?
                            .Where(p =>
                                DateTime.TryParse(p.s, out var s) &&
                                DateTime.TryParse(p.e, out var en) &&
                                en > s)
                            .ToList() ?? new();

                        isci.EvvelkiStajPeriodlari = temizlenmis.Count > 0
                            ? JsonSerializer.Serialize(temizlenmis)
                            : null;
                    }
                    catch
                    {
                        return Json(new { success = false, message = "Periodlar yanlış formatdadır." });
                    }
                }

                isci.YenilenmeTarixi = DateTime.Now;
                await _unitOfWork.Repository<Isci>().YenileAsync(isci);
                await _unitOfWork.YaddaSaxlaAsync();

                var staj = IsciStajHelper.Hesabla(isci, DateTime.Today);

                return Json(new
                {
                    success = true,
                    message = "Staj yeniləndi.",
                    stajIl = staj.Il,
                    stajAy = staj.Ay,
                    stajGun = staj.Gun,
                    evvelkiIl = staj.EvvelkiIl,
                    evvelkiAy = staj.EvvelkiAy,
                    evvelkiGun = staj.EvvelkiGun,
                    bankIl = staj.BankIl,
                    bankAy = staj.BankAy,
                    bankGun = staj.BankGun,
                    faiz = staj.Faiz
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }
    }
}

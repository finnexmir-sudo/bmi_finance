using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.HR.ViewModels.Jeton;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    // İşçilərin jeton saat sıralaması — yalnız oxuma (hesabat).
    // Sıralama: cari xərclənə bilən müsbət balans (böyükdən kiçiyə).
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class JetonSaatlariController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public JetonSaatlariController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            // Aktiv işçilər + aktiv təyinat (departament/vəzifə)
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .AsNoTracking()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir && !t.Silinib))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.IsciTeyinatlari.Where(t => t.Aktivdir && !t.Silinib))
                    .ThenInclude(t => t.Vezife)
                .ToListAsync();

            // Bütün jetonlar (təyinatı ilə) — bir sorğu, sonra yaddaşda qruplaşdırılır
            var jetonlar = await _unitOfWork.Repository<IsciJetonu>()
                .Query()
                .AsNoTracking()
                .Include(x => x.JetonTeyinati)
                .Where(x => !x.Silinib)
                .ToListAsync();

            var jetonByIsci = jetonlar
                .GroupBy(x => x.IsciId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var model = isciler.Select(isci =>
            {
                var list = jetonByIsci.GetValueOrDefault(isci.Id) ?? new List<IsciJetonu>();
                var musbet = list.Where(x => x.JetonTeyinati.Nov == JetonNovu.Musbat).ToList();
                var aktivMusbet = musbet.Where(x => x.Status == IsciJetonuStatus.Aktiv).ToList();

                var teyinat = isci.IsciTeyinatlari.FirstOrDefault();

                return new IsciJetonSaatVM
                {
                    IsciId = isci.Id,
                    AdSoyad = $"{isci.Ad} {isci.Soyad}".Trim(),
                    Departament = teyinat?.Departament?.Ad ?? "—",
                    Vezife = teyinat?.Vezife?.Ad,
                    // Cari balans — AktivSaatBalansiAsync ilə eyni qayda (QalanSaat ?? tam)
                    CariBalansSaat = aktivMusbet.Sum(x => x.QalanSaat ?? x.JetonTeyinati.SaatDeyeri),
                    // Qazanılmış (ləğv olunanlar xaric)
                    ToplamQazanilmisSaat = musbet
                        .Where(x => x.Status != IsciJetonuStatus.Legvedildi)
                        .Sum(x => x.JetonTeyinati.SaatDeyeri),
                    AktivMusbetSayi = aktivMusbet.Count,
                    AktivQaraSayi = list.Count(x => x.JetonTeyinati.Nov == JetonNovu.Menfi
                                                    && x.Status == IsciJetonuStatus.Aktiv)
                };
            })
            .OrderByDescending(x => x.CariBalansSaat)
            .ThenByDescending(x => x.ToplamQazanilmisSaat)
            .ThenBy(x => x.AdSoyad)
            .ToList();

            ViewData["Title"] = "İşçi Jeton Saatları";
            return View(model);
        }
    }
}

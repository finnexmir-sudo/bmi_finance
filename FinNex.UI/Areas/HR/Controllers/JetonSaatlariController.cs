using FinNex.Application.DTOs.HR.Icaze;
using FinNex.Application.Interfaces;
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
        private readonly IJetonService _jetonService;
        private readonly IIcazeService _icazeService;

        public JetonSaatlariController(
            IUnitOfWork unitOfWork,
            IJetonService jetonService,
            IIcazeService icazeService)
        {
            _unitOfWork = unitOfWork;
            _jetonService = jetonService;
            _icazeService = icazeService;
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

            // ── İllik "36 Saat Hüququ" — ayrıca, bir dəfəlik sorğu ──
            // Rəhbər tələbi (14.10.2026): "kimdə nə qalıb, nə istifadə olunub".
            // İSTİFADƏ = IcazeService.GetIsciIzlemeAsync (cari il, TesdiqSaat) —
            // layihədə bu, icazə-sayılan-saat düsturunun 6 kanonik çağırış
            // yerindən biridir (CLAUDE.md "İcazə — Nahar Güzəşti"), yeni düstur
            // YAZILMADI. HÜQUQ (qalıq) isə hər işçi üçün ayrıca oxunur —
            // IllikHuquqQalanSaatAsync yoxdursa jetonu YARADIR (DashboardService-
            // in də etdiyi kimi), ona görə N+1 qaçılmazdır, amma işçi sayı
            // böyük olmadığı üçün (bu, HR-ın bir dəfəlik hesabat səhifəsidir)
            // qəbul edilən xərcdir.
            var cariIl = DateTime.Today.Year;
            var izlemeNeticesi = await _icazeService.GetIsciIzlemeAsync(new IcazeIzlemeFiltrDto
            {
                TarixFrom = new DateTime(cariIl, 1, 1),
                TarixTo = DateTime.Today
            });
            var istifadeByIsci = izlemeNeticesi.Success && izlemeNeticesi.Data != null
                ? izlemeNeticesi.Data.ToDictionary(x => x.IsciId, x => x.TesdiqSaat)
                : new Dictionary<int, double>();

            var model = new List<IsciJetonSaatVM>();
            foreach (var isci in isciler)
            {
                var list = jetonByIsci.GetValueOrDefault(isci.Id) ?? new List<IsciJetonu>();
                // 14.10.2026 düzəlişi: "36 Saat Hüququ" (Sistemli=true) BURADAN
                // ÇIXARILDI — o, mükafat balansı DEYİL, aşağıdaki ayrıca
                // IllikHuquqSaat/IllikIstifadeSaat sütunlarında göstərilir.
                // Əvvəl filtr yox idi və bu jeton səssizcə "Cari balans"a
                // qarışırdı (bax CLAUDE.md-dəki bu bölmənin tarixçəsi).
                var musbet = list.Where(x => x.JetonTeyinati.Nov == JetonNovu.Musbat
                                           && !x.JetonTeyinati.Sistemli).ToList();
                var aktivMusbet = musbet.Where(x => x.Status == IsciJetonuStatus.Aktiv).ToList();

                var teyinat = isci.IsciTeyinatlari.FirstOrDefault();

                model.Add(new IsciJetonSaatVM
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
                                                    && x.Status == IsciJetonuStatus.Aktiv),
                    IllikHuquqSaat = await _jetonService.IllikHuquqQalanSaatAsync(isci.Id, cariIl),
                    IllikIstifadeSaat = (decimal)istifadeByIsci.GetValueOrDefault(isci.Id, 0.0)
                });
            }

            var sirali = model
                .OrderByDescending(x => x.CariBalansSaat)
                .ThenByDescending(x => x.ToplamQazanilmisSaat)
                .ThenBy(x => x.AdSoyad)
                .ToList();

            ViewData["Title"] = "İşçi Jeton Saatları";
            return View(sirali);
        }
    }
}

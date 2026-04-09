using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Rehber)]
    public class MezuniyyetBalansController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public MezuniyyetBalansController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET /HR/MezuniyyetBalans?il=2026
        public async Task<IActionResult> Index(int? il)
        {
            var cariIl = il ?? DateTime.Now.Year;
            ViewBag.SecilmisIl = cariIl;

            // İllərin siyahısı (dropdown üçün)
            var illerSiyahisi = await _unitOfWork.Repository<MezuniyyetBalans>()
                .Query()
                .Where(b => !b.Silinib)
                .Select(b => b.Il)
                .Distinct()
                .OrderByDescending(i => i)
                .ToListAsync();

            if (!illerSiyahisi.Contains(cariIl))
                illerSiyahisi.Insert(0, cariIl);

            ViewBag.Iller = illerSiyahisi;

            // Aktiv işçilər və balansları
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(x => x.MezuniyyetBalanslari.Where(b => !b.Silinib && b.Il == cariIl))
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
                .OrderBy(x => x.Soyad)
                .ThenBy(x => x.Ad)
                .ToListAsync();

            return View(isciler);
        }

        // POST /HR/MezuniyyetBalans/Update
        [HttpPost]
        public async Task<IActionResult> Update(int id, int toplamGun)
        {
            try
            {
                var balans = await _unitOfWork.Repository<MezuniyyetBalans>()
                    .IdIleGetirAsync(id);

                if (balans == null)
                    return Json(new { success = false, message = "Balans tapılmadı." });

                if (toplamGun < 0)
                    return Json(new { success = false, message = "Toplam gün mənfi ola bilməz." });

                if (toplamGun < balans.IstifadeOlunanGun)
                    return Json(new { success = false, message = "Toplam gün istifadə olunan gündən az ola bilməz." });

                balans.ToplamGun = toplamGun;
                balans.YenilenmeTarixi = DateTime.Now;

                await _unitOfWork.Repository<MezuniyyetBalans>().YenileAsync(balans);
                await _unitOfWork.YaddaSaxlaAsync();

                return Json(new
                {
                    success = true,
                    message = "Balans yeniləndi.",
                    qaliqGun = balans.QaliqGun
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }

        // POST /HR/MezuniyyetBalans/CreateOrUpdate — fərdi işçi üçün balans yarat/yenilə
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate(int isciId, int il, int illikGun, int xestelikGun, int ezamiyyetGun)
        {
            try
            {
                var repo = _unitOfWork.Repository<MezuniyyetBalans>();
                var novler = new[]
                {
                    (nov: MezuniyyetNovu.Illik, gun: illikGun),
                    (nov: MezuniyyetNovu.Xestelik, gun: xestelikGun),
                    (nov: MezuniyyetNovu.Ezamiyyet, gun: ezamiyyetGun)
                };

                foreach (var (nov, gun) in novler)
                {
                    if (gun < 0) continue;

                    var movcud = await repo.GetirAsync(x =>
                        x.IsciId == isciId && x.Il == il && x.Nov == nov && !x.Silinib);

                    if (movcud != null)
                    {
                        if (gun < movcud.IstifadeOlunanGun)
                            return Json(new { success = false, message = $"{nov} üçün toplam gün istifadə olunandan az ola bilməz." });

                        movcud.ToplamGun = gun;
                        movcud.YenilenmeTarixi = DateTime.Now;
                        await repo.YenileAsync(movcud);
                    }
                    else
                    {
                        await repo.YaratAsync(new MezuniyyetBalans
                        {
                            IsciId = isciId,
                            Il = il,
                            Nov = nov,
                            ToplamGun = gun,
                            IstifadeOlunanGun = 0
                        });
                    }
                }

                await _unitOfWork.YaddaSaxlaAsync();
                return Json(new { success = true, message = "Balans yeniləndi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }

        // POST /HR/MezuniyyetBalans/YeniIlBalansYarat
        [HttpPost]
        public async Task<IActionResult> YeniIlBalansYarat(int il)
        {
            try
            {
                // Artıq həmin il üçün balans yaradılıbmı?
                var movcud = await _unitOfWork.Repository<MezuniyyetBalans>()
                    .AnyAsync(b => b.Il == il && !b.Silinib);

                if (movcud)
                    return Json(new { success = false, message = $"{il}-ci il üçün balans artıq mövcuddur." });

                // Bütün aktiv işçiləri gətir
                var aktivIsciler = await _unitOfWork.Repository<Isci>()
                    .Query()
                    .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                    .ToListAsync();

                if (!aktivIsciler.Any())
                    return Json(new { success = false, message = "Aktiv işçi tapılmadı." });

                var repo = _unitOfWork.Repository<MezuniyyetBalans>();

                // Əvvəlki ilin İllik balanslarını gətir (carry-over üçün)
                var evvelkiIlBalanslari = await repo.Query()
                    .Where(b => b.Il == il - 1 && !b.Silinib && b.Nov == MezuniyyetNovu.Illik)
                    .ToListAsync();
                var evvelkiBalansDict = evvelkiIlBalanslari.ToDictionary(b => b.IsciId, b => b);

                foreach (var isci in aktivIsciler)
                {
                    // İllik məzuniyyət (əvvəlki ildən maks 5 gün köçürmə ilə)
                    int kecirilecekGun = 0;
                    if (evvelkiBalansDict.TryGetValue(isci.Id, out var evvelkiBalans))
                    {
                        int qaliq = evvelkiBalans.ToplamGun - evvelkiBalans.IstifadeOlunanGun;
                        kecirilecekGun = Math.Min(Math.Max(qaliq, 0), 5);
                    }

                    await repo.YaratAsync(new MezuniyyetBalans
                    {
                        IsciId = isci.Id,
                        Il = il,
                        Nov = MezuniyyetNovu.Illik,
                        ToplamGun = 21 + kecirilecekGun,
                        IstifadeOlunanGun = 0
                    });

                    // Xəstəlik (limitsiz)
                    await repo.YaratAsync(new MezuniyyetBalans
                    {
                        IsciId = isci.Id,
                        Il = il,
                        Nov = MezuniyyetNovu.Xestelik,
                        ToplamGun = 0,
                        IstifadeOlunanGun = 0
                    });

                    // Ezamiyyət (limitsiz)
                    await repo.YaratAsync(new MezuniyyetBalans
                    {
                        IsciId = isci.Id,
                        Il = il,
                        Nov = MezuniyyetNovu.Ezamiyyet,
                        ToplamGun = 0,
                        IstifadeOlunanGun = 0
                    });
                }

                await _unitOfWork.YaddaSaxlaAsync();

                return Json(new
                {
                    success = true,
                    message = $"{il}-ci il üçün {aktivIsciler.Count} işçiyə balans yaradıldı."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }
    }
}

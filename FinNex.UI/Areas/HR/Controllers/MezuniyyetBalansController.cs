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

            // Aktiv işçilər — cari il balansları + əvvəlki illərin İllik qalıqları
            // (əvvəlki illərdən qalan günlərin tarixçəsini göstərmək üçün)
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && x.Status == IsciStatus.Aktiv)
                .Include(x => x.MezuniyyetBalanslari.Where(b => !b.Silinib && b.Il == cariIl))
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
                .OrderBy(x => x.Soyad)
                .ThenBy(x => x.Ad)
                .ToListAsync();

            // Əvvəlki illərin İllik qalıqları (yalnız qaldığı günlər > 0 olanlar) —
            // ayrıca dictionary kimi göndərilir ki, view tarixçəni göstərə bilsin
            var isciIds = isciler.Select(i => i.Id).ToList();
            var evvelkiBalanslar = await _unitOfWork.Repository<MezuniyyetBalans>()
                .Query()
                .Where(b => !b.Silinib
                         && b.Nov == MezuniyyetNovu.Illik
                         && b.Il < cariIl
                         && isciIds.Contains(b.IsciId))
                .OrderByDescending(b => b.Il)
                .ToListAsync();

            // { isciId: [ (il, qaliq), ... ] } — yalnız qaliq > 0 olanlar göstərilir.
            // ValueTuple istifadə edilir (anonymous type view-də dynamic ilə problemli olur).
            ViewBag.EvvelkiIller = evvelkiBalanslar
                .Where(b => (b.ToplamGun - b.IstifadeOlunanGun) > 0)
                .GroupBy(b => b.IsciId)
                .ToDictionary(g => g.Key, g => g.Select(b => (Il: b.Il, Qaliq: b.ToplamGun - b.IstifadeOlunanGun)).ToList());

            // Standart illik məzuniyyət dərəcəsi (cari il üçün) — göstəricinin
            // "N gün bu ildən + M gün keçmiş illərdən" ayrımını göstərmək üçün
            ViewBag.StandartIllikGun = 21;

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

        // GET /HR/MezuniyyetBalans/IsciBalanslari?isciId=5
        // Bir işçinin bütün illərin balansını qaytarır — modal redaktə üçün.
        [HttpGet]
        public async Task<IActionResult> IsciBalanslari(int isciId)
        {
            try
            {
                var isci = await _unitOfWork.Repository<Isci>().IdIleGetirAsync(isciId);
                if (isci == null) return Json(new { success = false, message = "İşçi tapılmadı." });

                var balanslar = await _unitOfWork.Repository<MezuniyyetBalans>()
                    .Query()
                    .Where(b => !b.Silinib && b.IsciId == isciId)
                    .OrderByDescending(b => b.Il)
                    .ThenBy(b => b.Nov)
                    .ToListAsync();

                var data = balanslar.Select(b => new
                {
                    id = b.Id,
                    il = b.Il,
                    nov = (int)b.Nov,
                    novAd = b.Nov.ToString(),
                    toplamGun = b.ToplamGun,
                    istifade = b.IstifadeOlunanGun,
                    qaliq = b.ToplamGun - b.IstifadeOlunanGun
                }).ToList();

                return Json(new
                {
                    success = true,
                    isciAd = $"{isci.Ad} {isci.Soyad}",
                    balanslar = data
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Xəta: {ex.Message}" });
            }
        }

        // POST /HR/MezuniyyetBalans/AddIllikBalans
        // Müəyyən il üçün İllik məzuniyyət balansı yaradır (və ya artıq varsa
        // Toplam günü üstə gəlir). Bu HR-a keçmiş illərdə unudulmuş və ya
        // əl ilə əlavə edilmək istənilən günləri yazmağa imkan verir.
        [HttpPost]
        public async Task<IActionResult> AddIllikBalans(int isciId, int il, int toplamGun)
        {
            try
            {
                if (toplamGun <= 0)
                    return Json(new { success = false, message = "Toplam gün 0-dan böyük olmalıdır." });
                if (il < 2000 || il > DateTime.Now.Year + 5)
                    return Json(new { success = false, message = "İl düzgün deyil." });

                var repo = _unitOfWork.Repository<MezuniyyetBalans>();
                var movcud = await repo.GetirAsync(x =>
                    x.IsciId == isciId && x.Il == il && x.Nov == MezuniyyetNovu.Illik && !x.Silinib);

                if (movcud != null)
                {
                    // Artıq var — üstə gəl
                    movcud.ToplamGun += toplamGun;
                    movcud.YenilenmeTarixi = DateTime.Now;
                    await repo.YenileAsync(movcud);
                    await _unitOfWork.YaddaSaxlaAsync();
                    return Json(new { success = true, message = $"{il}-ci il balansına {toplamGun} gün əlavə edildi. Cəmi: {movcud.ToplamGun} gün." });
                }

                // Yenisi
                await repo.YaratAsync(new MezuniyyetBalans
                {
                    IsciId = isciId,
                    Il = il,
                    Nov = MezuniyyetNovu.Illik,
                    ToplamGun = toplamGun,
                    IstifadeOlunanGun = 0
                });
                await _unitOfWork.YaddaSaxlaAsync();
                return Json(new { success = true, message = $"{il}-ci il üçün {toplamGun} günlük balans yaradıldı." });
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

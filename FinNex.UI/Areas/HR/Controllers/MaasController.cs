using FinNex.Domain;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Application.Interfaces.Communication;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Communication;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib + "," + RoleNames.Rehber)]
    public class MaasController : Controller
    {
        private readonly IMaasService _maasService;
        private readonly IMaasHesablamaService _hesablamaService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBildirisService _bildirisService;
        private readonly UserManager<AppUser> _userManager;

        public MaasController(
            IMaasService maasService,
            IMaasHesablamaService hesablamaService,
            IUnitOfWork unitOfWork,
            IBildirisService bildirisService,
            UserManager<AppUser> userManager)
        {
            _maasService = maasService;
            _hesablamaService = hesablamaService;
            _unitOfWork = unitOfWork;
            _bildirisService = bildirisService;
            _userManager = userManager;
        }

        // ── GET /HR/Maas ─────────────────────────────────────────
        // Əsas siyahı — hər işçi, hər sütun ayrı məbləğ
        public async Task<IActionResult> Index(int? il, int? ay, int? isciId, int? departamentId)
        {
            var cIl = il ?? DateTime.Now.Year;
            var cAy = ay ?? DateTime.Now.Month;

            ViewBag.SecilmisIl = cIl;
            ViewBag.SecilmisAy = cAy;
            ViewBag.SecilmisIsciId = isciId;
            ViewBag.SecilisDepartamentId = departamentId;

            // Filtr siyahıları
            await FilterSiyahilariniDoldur(cIl, cAy, isciId, departamentId);

            // Maaşları gətir — bütün detallarla
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    x.Il == cIl &&
                    x.Ay == cAy &&
                    (isciId == null || x.IsciId == isciId))
                .Include(x => x.Isci)
                    .ThenInclude(i => i.Maliye)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.Detallar)
                    .ThenInclude(d => d.MaasNovu)
                .OrderBy(x => x.Isci.Soyad)
                .ToListAsync();

            // Departament filteri (JOIN sonrası)
            if (departamentId.HasValue)
            {
                maaslar = maaslar
                    .Where(m => m.Isci.IsciTeyinatlari
                        .Any(t => t.DepartamentId == departamentId))
                    .ToList();
            }

            // DTO-ya çevir — hər sütun ayrı
            var listDto = maaslar.Select(m =>
            {
                var teyinat = m.Isci.IsciTeyinatlari.FirstOrDefault();

                decimal GetDetay(string ad) =>
                    m.Detallar.Where(d => d.MaasNovu?.Ad == ad).Sum(d => d.Mebleg);

                return new MaasListDto
                {
                    Id = m.Id,
                    IsciId = m.IsciId,
                    IsciAdSoyad = $"{m.Isci.Ad} {m.Isci.Soyad}",
                    DepartamentAd = teyinat?.Departament?.Ad ?? "—",
                    VezifeAd = teyinat?.Vezife?.Ad ?? "—",
                    BankHesabNo = m.Isci.Maliye?.BankHesabNo,
                    Il = m.Il,
                    Ay = m.Ay,
                    EsasMaas = GetDetay("Əsas Əməkhaqqı"),
                    BonusMeblegi = GetDetay("Bonus/Mükafat"),
                    MezuniyyetOdenisi = GetDetay("Məzuniyyət Ödənişi"),
                    MezuniyyetEsasMaasKesintisi = GetDetay("Məzuniyyət Kəsintisi"),
                    CerimeMeblegi = GetDetay("Gecikdirmə Cəriməsi"),
                    BrutMaas = m.Detallar
                        .Where(d => d.MaasNovu?.Tip == MaasDetayTipi.Gelir)
                        .Sum(d => d.Mebleg)
                        - m.Detallar
                        .Where(d => d.MaasNovu?.Tip == MaasDetayTipi.Tutulma &&
                                    (d.MaasNovu.Ad == "Məzuniyyət Kəsintisi" ||
                                     d.MaasNovu.Ad == "Gecikdirmə Cəriməsi"))
                        .Sum(d => d.Mebleg),
                    GelirVergisi = GetDetay("Gəlir Vergisi"),
                    DsmfIsci = GetDetay("DSMF (İşçi)"),
                    IssizlikIsci = GetDetay("İşsizlik Sığortası (İşçi)"),
                    Itss = GetDetay("İTSS"),
                    NetMebleg = m.NetMebleg,
                    Status = m.Status,
                    HesablanmaTarixi = m.HesablanmaTarixi,
                    TesdiqTarixi = m.TesdiqTarixi,
                    OdenisTarixi = m.OdenisTarixi
                };
            }).ToList();

            // Statistika
            ViewBag.UmumiNetMebleg = listDto.Sum(x => x.NetMebleg);
            ViewBag.LayiheSayi = listDto.Count(x => x.Status == MaasStatus.Layihe);
            ViewBag.TesdiqSayi = listDto.Count(x => x.Status == MaasStatus.Tesdiqlendi);
            ViewBag.OdenisSayi = listDto.Count(x => x.Status == MaasStatus.Odenildi);
            ViewBag.IsciSayi = listDto.Count;

            // Aktiv işçi sayı — Toplu Hesabla düyməsinin görünməsi üçün
            ViewBag.AktivIsciSayi = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .CountAsync();

            ViewData["Title"] = $"Əmək Haqqı — {cIl}/{cAy:D2}";
            return View(listDto);
        }

        // ── GET /HR/Maas/Hesabla ─────────────────────────────────
        [HttpGet]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Hesabla(int? isciId)
        {
            await HesablaFormSiyahilariDoldur();
            var vm = new FerdiHesablaInputDto
            {
                IsciId = isciId ?? 0,
                Il = DateTime.Now.Year,
                Ay = DateTime.Now.Month
            };
            ViewData["Title"] = "Maaş Hesabla";
            return View(vm);
        }

        // ── POST /HR/Maas/Hesabla ────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Hesabla(FerdiHesablaInputDto input)
        {
            if (!ModelState.IsValid)
            {
                await HesablaFormSiyahilariDoldur();
                return View(input);
            }

            // Tarix validasiyası — gələcək ay bloklanır, 12 aydan köhnə də
            var bugun = DateTime.Now;
            var cariAyBirinci = new DateTime(bugun.Year, bugun.Month, 1);
            var secilmisAyBirinci = new DateTime(input.Il, input.Ay, 1);
            var minTarix = cariAyBirinci.AddMonths(-12);

            if (secilmisAyBirinci > cariAyBirinci)
            {
                TempData["Error"] = "Gələcək ay üçün maaş hesablaması aparıla bilməz.";
                await HesablaFormSiyahilariDoldur();
                return View(input);
            }
            if (secilmisAyBirinci < minTarix)
            {
                TempData["Error"] = "Son 12 aydan daha köhnə aylar üçün hesablama aparıla bilməz.";
                await HesablaFormSiyahilariDoldur();
                return View(input);
            }

            var r = await _hesablamaService.FerdiHesablaAsync(input);
            if (!r.Success)
            {
                TempData["Error"] = r.Message;
                await HesablaFormSiyahilariDoldur();
                return View(input);
            }

            TempData["Success"] = $"{r.Data!.IsciAdSoyad} — NET: {r.Data.NetMaas:N2} AZN";
            return RedirectToAction(nameof(Detal), new { id = r.Data.MaasId });
        }

        // ── GET /HR/Maas/TopluHesabla ────────────────────────────
        [HttpGet]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> TopluHesabla(int? il, int? ay)
        {
            var cIl = il ?? DateTime.Now.Year;
            var cAy = ay ?? DateTime.Now.Month;

            // Tarix validasiyası — gələcək aylar bloklanır, 12 aydan köhnə də.
            var bugun = DateTime.Now;
            var cariAyBirinci = new DateTime(bugun.Year, bugun.Month, 1);
            var secilmisAyBirinci = new DateTime(cIl, cAy, 1);
            var minTarix = cariAyBirinci.AddMonths(-12);

            if (secilmisAyBirinci > cariAyBirinci)
            {
                TempData["Error"] = "Gələcək ay üçün maaş hesablaması aparıla bilməz.";
                return RedirectToAction(nameof(Index), new { il = bugun.Year, ay = bugun.Month });
            }
            if (secilmisAyBirinci < minTarix)
            {
                TempData["Error"] = "Son 12 aydan daha köhnə aylar üçün hesablama aparıla bilməz.";
                return RedirectToAction(nameof(Index), new { il = bugun.Year, ay = bugun.Month });
            }

            // Aktiv işçiləri gətir — bonus/cərimə daxil etmək üçün
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .Include(x => x.Maliye)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Vezife)
                .OrderBy(x => x.Soyad)
                .ToListAsync();

            // CariMaas — IsciMaliye-dən birbaşa sorğu (navigation-dan asılı olmayaraq)
            var isciIdler = isciler.Select(x => x.Id).ToList();
            var maliyeler = await _unitOfWork.Repository<IsciMaliye>()
                .Query()
                .Where(x => isciIdler.Contains(x.IsciId) && !x.Silinib)
                .ToListAsync();
            var cariMaasMap = maliyeler.ToDictionary(x => x.IsciId, x => x.CariMaas);
            var ibanMap = maliyeler.ToDictionary(x => x.IsciId, x => x.BankHesabNo);

            // Artıq hesablanmışları işarələ
            var hesablanmis = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.Il == cIl && x.Ay == cAy && !x.Silinib)
                .Select(x => x.IsciId)
                .ToListAsync();

            // Vergi pillələri — informativ kartlar üçün
            var hesabTarixi = new DateTime(cIl, cAy, 1);
            var pilleler = await _unitOfWork.Repository<VergiPille>()
                .Query()
                .Where(x =>
                    x.Aktivdir && !x.Silinib &&
                    x.BaslamaTarixi <= hesabTarixi &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi))
                .OrderBy(x => x.Nov).ThenBy(x => x.Sira)
                .ToListAsync();

            // Vergi güzəşti və minimum əmək haqqı (flat parametrlərdən)
            var flatParamlar = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .Where(x =>
                    x.Aktivdir && !x.Silinib &&
                    x.BaslamaTarixi <= hesabTarixi &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi))
                .OrderByDescending(x => x.BaslamaTarixi)
                .ToListAsync();
            var vergiGuzesti = flatParamlar.FirstOrDefault(x => x.Nov == MaasParametrNovu.VergiGuzestiMeblegi)?.Deyer ?? 200m;

            ViewBag.Il = cIl;
            ViewBag.Ay = cAy;
            ViewBag.Hesablanmis = hesablanmis;
            ViewBag.CariMaasMap = cariMaasMap;
            ViewBag.IbanMap = ibanMap;
            ViewBag.VergiPilleleri = pilleler;
            ViewBag.VergiGuzesti = vergiGuzesti;
            ViewBag.Iller = IlSiyahisi(cIl);
            ViewBag.Aylar = AySiyahisi(cAy);

            ViewData["Title"] = "Toplu Maaş Hesablaması";
            return View(isciler);
        }

        // ── POST /HR/Maas/TopluHesablaEt ─────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> TopluHesablaEt(
            int il, int ay,
            [FromForm] List<FerdiElaveDto> ferdiElaveler)
        {
            // Tarix validasiyası — POST zamanı təkrar yoxlama
            var bugun = DateTime.Now;
            var cariAyBirinci = new DateTime(bugun.Year, bugun.Month, 1);
            var secilmisAyBirinci = new DateTime(il, ay, 1);
            var minTarix = cariAyBirinci.AddMonths(-12);

            if (secilmisAyBirinci > cariAyBirinci)
            {
                TempData["Error"] = "Gələcək ay üçün maaş hesablaması aparıla bilməz.";
                return RedirectToAction(nameof(Index), new { il = bugun.Year, ay = bugun.Month });
            }
            if (secilmisAyBirinci < minTarix)
            {
                TempData["Error"] = "Son 12 aydan daha köhnə aylar üçün hesablama aparıla bilməz.";
                return RedirectToAction(nameof(Index), new { il = bugun.Year, ay = bugun.Month });
            }

            var input = new TopluHesablaInputDto
            {
                Il = il,
                Ay = ay,
                FerdiElaveler = ferdiElaveler.Where(x =>
                    x.BonusMeblegi > 0 || x.CerimeMeblegi > 0).ToList()
            };

            var r = await _hesablamaService.TopluHesablaAsync(input);
            if (!r.Success)
            {
                TempData["Error"] = r.Message;
                return RedirectToAction(nameof(TopluHesabla), new { il, ay });
            }

            var d = r.Data!;
            TempData["Success"] =
                $"Toplu hesablama: {d.UgurluSayi} uğurlu, " +
                $"{d.AtlananSayi} atlandı, {d.XetaliSayi} xətalı. " +
                $"Ümumi NET: {d.UmumiNetMebleg:N2} AZN";

            if (d.Xetalar.Any())
                TempData["Xetalar"] = string.Join("|", d.Xetalar);

            // Bildiriş: bütün Rəhbər/Admin istifadəçilərə təsdiq sorğusu göndər
            if (d.UgurluSayi > 0)
            {
                await BildirisGonderRehberlereAsync(il, ay, d.UgurluSayi);
            }

            return RedirectToAction(nameof(Index), new { il, ay });
        }

        // ── HELPER: Bütün Rəhbər/Admin istifadəçilərə bildiriş göndər ──
        private async Task BildirisGonderRehberlereAsync(int il, int ay, int ugurluSayi)
        {
            try
            {
                var ayAdlar = new[] { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                                      "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };
                var dovr = $"{ayAdlar[ay]} {il}";
                var redirectUrl = Url.Action("Index", "Maas", new { area = "HR", il, ay });

                // Rəhbər və Admin rolu olan bütün istifadəçiləri tap
                var rehberler = await _userManager.GetUsersInRoleAsync(RoleNames.Rehber);
                var adminler = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                var alicilar = rehberler.Concat(adminler)
                    .Where(u => u.IsciId.HasValue)
                    .GroupBy(u => u.IsciId!.Value)
                    .Select(g => g.First())
                    .ToList();

                foreach (var u in alicilar)
                {
                    await _bildirisService.YaratAsync(
                        isciId: u.IsciId!.Value,
                        nov: BildirisNovu.TesdiqSorgusu,
                        bashliq: $"Maaş təsdiqi gözləyir — {dovr}",
                        metn: $"{ugurluSayi} işçi üçün {dovr} maaşı hesablandı, təsdiqinizi gözləyir.",
                        redirectUrl: redirectUrl
                    );
                }
            }
            catch
            {
                // Bildiriş göndərmə xətası əsas əməliyyatı pozmasın
            }
        }

        // ── POST /HR/Maas/TopluOdeniş ────────────────────────────
        // Bütün təsdiqlənmiş maaşları bir kliklə "Ödənildi" işarələ
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Muhasib + "," + RoleNames.Admin)]
        public async Task<IActionResult> TopluOdenis(int il, int ay)
        {
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.Il == il && x.Ay == ay && !x.Silinib && x.Status == MaasStatus.Tesdiqlendi)
                .ToListAsync();

            if (!maaslar.Any())
            {
                TempData["Error"] = "Ödəniş üçün təsdiqlənmiş maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            int ugurlu = 0, xeta = 0;
            foreach (var m in maaslar)
            {
                var r = await _maasService.StatusDeyisAsync(m.Id, MaasStatus.Odenildi);
                if (r.Success) ugurlu++;
                else xeta++;
            }

            TempData[xeta > 0 ? "Error" : "Success"] =
                $"Toplu ödəniş: {ugurlu} maaş 'Ödənildi' işarələndi" + (xeta > 0 ? $", {xeta} xətalı." : ".");
            return RedirectToAction(nameof(Index), new { il, ay });
        }

        // ── POST /HR/Maas/TopluTesdiqle ──────────────────────────
        // Bütün Layihə statuslu maaşları bir kliklə təsdiqlə
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> TopluTesdiqle(int il, int ay)
        {
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.Il == il && x.Ay == ay && !x.Silinib && x.Status == MaasStatus.Layihe)
                .ToListAsync();

            if (!maaslar.Any())
            {
                TempData["Error"] = "Təsdiqlənəcək layihə statusunda maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            int ugurlu = 0, xeta = 0;
            foreach (var m in maaslar)
            {
                var r = await _maasService.StatusDeyisAsync(m.Id, MaasStatus.Tesdiqlendi);
                if (r.Success) ugurlu++;
                else xeta++;
            }

            // Mühasibə bildiriş göndər
            if (ugurlu > 0)
            {
                await BildirisGonderMuhasibleriAsync(il, ay, ugurlu);
            }

            TempData[xeta > 0 ? "Error" : "Success"] =
                $"Toplu təsdiq: {ugurlu} maaş təsdiqləndi" + (xeta > 0 ? $", {xeta} xətalı." : ".");
            return RedirectToAction(nameof(Index), new { il, ay });
        }

        // ── HELPER: Mühasiblərə bildiriş göndər ──
        private async Task BildirisGonderMuhasibleriAsync(int il, int ay, int sayi)
        {
            try
            {
                var ayAdlar = new[] { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                                      "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };
                var dovr = $"{ayAdlar[ay]} {il}";
                var redirectUrl = Url.Action("Index", "Maas", new { area = "HR", il, ay });

                var muhasibler = await _userManager.GetUsersInRoleAsync(RoleNames.Muhasib);
                var adminler = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                var alicilar = muhasibler.Concat(adminler)
                    .Where(u => u.IsciId.HasValue)
                    .GroupBy(u => u.IsciId!.Value)
                    .Select(g => g.First())
                    .ToList();

                foreach (var u in alicilar)
                {
                    await _bildirisService.YaratAsync(
                        isciId: u.IsciId!.Value,
                        nov: BildirisNovu.TesdiqSorgusu,
                        bashliq: $"Maaş ödənişə hazırdır — {dovr}",
                        metn: $"{sayi} işçi üçün {dovr} maaşı təsdiqləndi, ödəniş gözləyir.",
                        redirectUrl: redirectUrl
                    );
                }
            }
            catch { }
        }

        // ── GET /HR/Maas/Detal/5 ────────────────────────────────
        public async Task<IActionResult> Detal(int id)
        {
            var maas = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.Id == id && !x.Silinib)
                .Include(x => x.Isci).ThenInclude(i => i.Maliye)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Departament)
                .Include(x => x.Isci)
                    .ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                        .ThenInclude(t => t.Vezife)
                .Include(x => x.Detallar).ThenInclude(d => d.MaasNovu)
                .FirstOrDefaultAsync();

            if (maas == null)
            {
                TempData["Error"] = "Maaş tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var teyinat = maas.Isci.IsciTeyinatlari.FirstOrDefault();

            var dto = new MaasDto
            {
                Id = maas.Id,
                IsciId = maas.IsciId,
                IsciAdSoyad = $"{maas.Isci.Ad} {maas.Isci.Soyad}",
                DepartamentAd = teyinat?.Departament?.Ad ?? "—",
                VezifeAd = teyinat?.Vezife?.Ad ?? "—",
                BankHesabNo = maas.Isci.Maliye?.BankHesabNo,
                Il = maas.Il,
                Ay = maas.Ay,
                NetMebleg = maas.NetMebleg,
                Status = maas.Status,
                HesablanmaTarixi = maas.HesablanmaTarixi,
                TesdiqTarixi = maas.TesdiqTarixi,
                OdenisTarixi = maas.OdenisTarixi,
                Detallar = maas.Detallar.Select(d => new MaasDetayDto
                {
                    Id = d.Id,
                    MaasNovuAd = d.MaasNovu?.Ad ?? "—",
                    Tip = d.MaasNovu?.Tip ?? MaasDetayTipi.Gelir,
                    Mebleg = d.Mebleg,
                    Aciqlama = d.Aciqlama
                }).ToList()
            };

            ViewData["Title"] = $"Maaş Detalı — {maas.Isci.Ad} {maas.Isci.Soyad}";
            return View(dto);
        }

        // ── GET /HR/Maas/IsciTarixce/5 ──────────────────────────
        // Bir işçinin bütün aylara görə maaş tarixi
        public async Task<IActionResult> IsciTarixce(int isciId)
        {
            var isci = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Id == isciId)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Vezife)
                .FirstOrDefaultAsync();

            if (isci == null) return NotFound();

            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.IsciId == isciId && !x.Silinib)
                .Include(x => x.Detallar).ThenInclude(d => d.MaasNovu)
                .OrderByDescending(x => x.Il * 12 + x.Ay)
                .ToListAsync();

            ViewBag.IsciAdSoyad = $"{isci.Ad} {isci.Soyad}";
            ViewBag.VezifeAd = isci.IsciTeyinatlari.FirstOrDefault()?.Vezife?.Ad ?? "—";
            ViewData["Title"] = $"Maaş Tarixi — {isci.Ad} {isci.Soyad}";

            return View(maaslar.Select(m => new MaasListDto
            {
                Id = m.Id,
                IsciId = m.IsciId,
                Il = m.Il,
                Ay = m.Ay,
                EsasMaas = m.Detallar.Where(d => d.MaasNovu?.Ad == "Əsas Əməkhaqqı").Sum(d => d.Mebleg),
                BonusMeblegi = m.Detallar.Where(d => d.MaasNovu?.Ad == "Bonus/Mükafat").Sum(d => d.Mebleg),
                MezuniyyetOdenisi = m.Detallar.Where(d => d.MaasNovu?.Ad == "Məzuniyyət Ödənişi").Sum(d => d.Mebleg),
                GelirVergisi = m.Detallar.Where(d => d.MaasNovu?.Ad == "Gəlir Vergisi").Sum(d => d.Mebleg),
                DsmfIsci = m.Detallar.Where(d => d.MaasNovu?.Ad == "DSMF (İşçi)").Sum(d => d.Mebleg),
                IssizlikIsci = m.Detallar.Where(d => d.MaasNovu?.Ad == "İşsizlik Sığortası (İşçi)").Sum(d => d.Mebleg),
                Itss = m.Detallar.Where(d => d.MaasNovu?.Ad == "İTSS").Sum(d => d.Mebleg),
                NetMebleg = m.NetMebleg,
                Status = m.Status,
                HesablanmaTarixi = m.HesablanmaTarixi,
                TesdiqTarixi = m.TesdiqTarixi,
                OdenisTarixi = m.OdenisTarixi
            }).ToList());
        }

        // ── POST /HR/Maas/StatusDeyis ────────────────────────────
        // İş axını:
        //   1) HR/Admin hesablayır → Layihə statusunda yaradılır
        //   2) Rəhbər/Admin təsdiq edir → Təsdiqləndi
        //   3) Mühasib/Admin ödənişi yerinə yetirir → Ödənildi
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusDeyis(int id, MaasStatus yeniStatus, int il, int ay)
        {
            // Rol-əsaslı icazə yoxlaması
            var isAdmin = User.IsInRole(RoleNames.Admin);
            var isRehber = User.IsInRole(RoleNames.Rehber);
            var isMuhasib = User.IsInRole(RoleNames.Muhasib);
            var isHR = User.IsInRole(RoleNames.HR);

            if (yeniStatus == MaasStatus.Tesdiqlendi)
            {
                // Yalnız Rəhbər və ya Admin təsdiq edə bilər
                if (!isRehber && !isAdmin)
                {
                    TempData["Error"] = "Maaşı yalnız Rəhbər və ya Admin təsdiqləyə bilər.";
                    return RedirectToAction(nameof(Index), new { il, ay });
                }
            }
            else if (yeniStatus == MaasStatus.Odenildi)
            {
                // Yalnız Mühasib və ya Admin ödənildi statusuna keçirə bilər
                if (!isMuhasib && !isAdmin)
                {
                    TempData["Error"] = "Maaşı yalnız Mühasib və ya Admin 'Ödənildi' edə bilər.";
                    return RedirectToAction(nameof(Index), new { il, ay });
                }
                // Qeyd: IBAN yoxlaması çıxarıldı — bank əməliyyatı sistemxarici
                // aparılır. Mühasib öncədən bank köçürməsini həyata keçirir,
                // sonra burada təsdiqləyir. Bəzi işçilərin IBAN-ı bazada
                // olmaya bilər (məs. nağd ödəniş alanlar).
            }
            else if (yeniStatus == MaasStatus.LegvEdildi)
            {
                // Yalnız Admin ləğv edə bilər
                if (!isAdmin)
                {
                    TempData["Error"] = "Maaşı yalnız Admin ləğv edə bilər.";
                    return RedirectToAction(nameof(Index), new { il, ay });
                }
            }

            var r = await _maasService.StatusDeyisAsync(id, yeniStatus);
            TempData[r.Success ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Index), new { il, ay });
        }

        // ── POST /HR/Maas/Sil ────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin)]
        public async Task<IActionResult> Sil(int id, int il, int ay)
        {
            var maas = await _unitOfWork.Repository<Maas>().IdIleGetirAsync(id);
            if (maas?.Status != MaasStatus.Layihe)
            {
                TempData["Error"] = "Yalnız 'Layihə' statusundakı maaşı silmək olar.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            var r = await _maasService.SilAsync(id);
            TempData[r.Success ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Index), new { il, ay });
        }

        // ── GET /HR/Maas/BankFayliYukle ──────────────────────────
        // Tam Excel ixracı (ClosedXML ilə) — bütün məlumatlarla səliqəli
        [Authorize(Roles = RoleNames.Muhasib + "," + RoleNames.Admin + "," + RoleNames.HR + "," + RoleNames.Rehber)]
        public async Task<IActionResult> BankFayliYukle(int il, int ay)
        {
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib && x.Il == il && x.Ay == ay)
                .Include(x => x.Isci).ThenInclude(i => i.Maliye)
                .Include(x => x.Isci).ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.Isci).ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Vezife)
                .Include(x => x.Detallar).ThenInclude(d => d.MaasNovu)
                .OrderBy(x => x.Isci.Soyad).ThenBy(x => x.Isci.Ad)
                .ToListAsync();

            if (!maaslar.Any())
            {
                TempData["Error"] = "Bu dövr üçün maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            var ayAdlar = new[] { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                                  "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add($"Əmək Haqqı {ayAdlar[ay]} {il}");

            // ── Başlıq sətri (mərge) ─────────────────────────────
            ws.Cell("A1").Value = $"ƏMƏK HAQQI HESABLAMASI — {ayAdlar[ay]} {il}";
            ws.Range("A1:Q1").Merge();
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;
            ws.Cell("A1").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Cell("A1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1a2332");
            ws.Cell("A1").Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            ws.Row(1).Height = 28;

            // Yaradılma tarixi
            ws.Cell("A2").Value = $"Yaradılma tarixi: {DateTime.Now:dd.MM.yyyy HH:mm}";
            ws.Range("A2:Q2").Merge();
            ws.Cell("A2").Style.Font.Italic = true;
            ws.Cell("A2").Style.Font.FontSize = 10;
            ws.Cell("A2").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Cell("A2").Style.Font.FontColor = ClosedXML.Excel.XLColor.FromHtml("#7a8599");

            // ── Sütun başlıqları ─────────────────────────────────
            var headers = new[] {
                "№", "Ad Soyad", "Departament", "Vəzifə", "FİN", "IBAN",
                "Əsas Maaş", "Bonus", "Məz. Ödəniş", "Cərimə",
                "BRÜT", "Gəlir Vergisi", "DSMF (İşçi)", "İşsizlik (İşçi)", "İTSS (İşçi)",
                "NET MAAŞ", "Status"
            };

            int headerRow = 4;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#c9900a");
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
            }
            ws.Row(headerRow).Height = 32;

            decimal Get(Maas m, string ad) =>
                m.Detallar.Where(d => d.MaasNovu?.Ad == ad).Sum(d => d.Mebleg);

            // ── Data sətirləri ─────────────────────────────────
            int row = headerRow + 1;
            int sira = 1;
            foreach (var m in maaslar)
            {
                var teyinat = m.Isci.IsciTeyinatlari.FirstOrDefault();
                var iban = m.Isci.Maliye?.BankHesabNo ?? "";
                var fin = m.Isci.FIN ?? "";
                var dept = teyinat?.Departament?.Ad ?? "—";
                var vezife = teyinat?.Vezife?.Ad ?? "—";

                var esas = Get(m, "Əsas Əməkhaqqı");
                var bonus = Get(m, "Bonus/Mükafat");
                var mezOd = Get(m, "Məzuniyyət Ödənişi");
                var cerime = Get(m, "Gecikdirmə Cəriməsi") + Get(m, "Davamiyyət Kəsintisi");
                var gelirV = Get(m, "Gəlir Vergisi");
                var dsmf = Get(m, "DSMF (İşçi)");
                var iss = Get(m, "İşsizlik Sığortası (İşçi)");
                var itss = Get(m, "İTSS");

                var statusText = m.Status switch
                {
                    MaasStatus.Layihe => "Layihə",
                    MaasStatus.Tesdiqlendi => "Təsdiqləndi",
                    MaasStatus.Odenildi => "Ödənildi",
                    MaasStatus.LegvEdildi => "Ləğv edildi",
                    _ => m.Status.ToString()
                };

                ws.Cell(row, 1).Value = sira;
                ws.Cell(row, 2).Value = $"{m.Isci.Ad} {m.Isci.Soyad}";
                ws.Cell(row, 3).Value = dept;
                ws.Cell(row, 4).Value = vezife;
                ws.Cell(row, 5).Value = fin;
                ws.Cell(row, 6).Value = iban;
                ws.Cell(row, 7).Value = esas;
                ws.Cell(row, 8).Value = bonus;
                ws.Cell(row, 9).Value = mezOd;
                ws.Cell(row, 10).Value = cerime;
                ws.Cell(row, 11).Value = m.BrutMebleg;
                ws.Cell(row, 12).Value = gelirV;
                ws.Cell(row, 13).Value = dsmf;
                ws.Cell(row, 14).Value = iss;
                ws.Cell(row, 15).Value = itss;
                ws.Cell(row, 16).Value = m.NetMebleg;
                ws.Cell(row, 17).Value = statusText;

                // Number formatting (kolonlar 7-16)
                for (int c = 7; c <= 16; c++)
                {
                    ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00 \"₼\"";
                    ws.Cell(row, c).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
                }
                ws.Cell(row, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 17).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                // NET kolonu vurğula
                ws.Cell(row, 16).Style.Font.Bold = true;
                ws.Cell(row, 16).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#fffbeb");
                // BRÜT kolonu da vurğula
                ws.Cell(row, 11).Style.Font.Bold = true;

                // Sətir border-ı
                ws.Range(row, 1, row, headers.Length).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, headers.Length).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                row++;
                sira++;
            }

            // ── CƏMI sətri ─────────────────────────────────────
            int totalRow = row;
            ws.Cell(totalRow, 1).Value = "";
            ws.Cell(totalRow, 2).Value = $"CƏMİ — {maaslar.Count} işçi";
            ws.Range(totalRow, 1, totalRow, 6).Merge();

            ws.Cell(totalRow, 7).FormulaA1 = $"SUM(G{headerRow + 1}:G{row - 1})";
            ws.Cell(totalRow, 8).FormulaA1 = $"SUM(H{headerRow + 1}:H{row - 1})";
            ws.Cell(totalRow, 9).FormulaA1 = $"SUM(I{headerRow + 1}:I{row - 1})";
            ws.Cell(totalRow, 10).FormulaA1 = $"SUM(J{headerRow + 1}:J{row - 1})";
            ws.Cell(totalRow, 11).FormulaA1 = $"SUM(K{headerRow + 1}:K{row - 1})";
            ws.Cell(totalRow, 12).FormulaA1 = $"SUM(L{headerRow + 1}:L{row - 1})";
            ws.Cell(totalRow, 13).FormulaA1 = $"SUM(M{headerRow + 1}:M{row - 1})";
            ws.Cell(totalRow, 14).FormulaA1 = $"SUM(N{headerRow + 1}:N{row - 1})";
            ws.Cell(totalRow, 15).FormulaA1 = $"SUM(O{headerRow + 1}:O{row - 1})";
            ws.Cell(totalRow, 16).FormulaA1 = $"SUM(P{headerRow + 1}:P{row - 1})";

            for (int c = 7; c <= 16; c++)
            {
                ws.Cell(totalRow, c).Style.NumberFormat.Format = "#,##0.00 \"₼\"";
                ws.Cell(totalRow, c).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Right;
            }
            ws.Range(totalRow, 1, totalRow, headers.Length).Style.Font.Bold = true;
            ws.Range(totalRow, 1, totalRow, headers.Length).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1a2332");
            ws.Range(totalRow, 1, totalRow, headers.Length).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            ws.Range(totalRow, 1, totalRow, headers.Length).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
            ws.Row(totalRow).Height = 26;

            // ── Sütun enini avtomatik nizamla ─────────────────
            ws.Columns().AdjustToContents();
            ws.Column(2).Width = Math.Max(ws.Column(2).Width, 25); // Ad Soyad
            ws.Column(3).Width = Math.Max(ws.Column(3).Width, 18); // Departament
            ws.Column(4).Width = Math.Max(ws.Column(4).Width, 18); // Vəzifə

            // Freeze header row
            ws.SheetView.FreezeRows(headerRow);

            // ── Stream qaytarma ───────────────────────────────
            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Maas_{ayAdlar[ay]}_{il}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ── GET /HR/Maas/BankKocurme ─────────────────────────────
        // IBAN;FullName;NetAmount;Currency;Description formatında bank köçürmə faylı
        [HttpGet]
        [Authorize(Roles = RoleNames.Muhasib + "," + RoleNames.Admin)]
        public async Task<IActionResult> BankKocurme(int il, int ay)
        {
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib && x.Il == il && x.Ay == ay &&
                            x.Status == MaasStatus.Tesdiqlendi)
                .Include(x => x.Isci).ThenInclude(i => i.Maliye)
                .OrderBy(x => x.Isci.Soyad)
                .ThenBy(x => x.Isci.Ad)
                .ToListAsync();

            if (!maaslar.Any())
            {
                TempData["Error"] = "Təsdiqlənmiş maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            var satirlar = new List<string> { "IBAN;Ad Soyad;Məbləğ;Valyuta;İzah" };
            foreach (var m in maaslar)
            {
                var iban = m.Isci.Maliye?.BankHesabNo ?? "";
                var adSoyad = $"{m.Isci.Ad} {m.Isci.Soyad}";
                satirlar.Add(
                    $"{iban};{adSoyad};{m.NetMebleg:F2};AZN;" +
                    $"{il}/{ay:D2} əmək haqqı köçürməsi");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", satirlar));
            return File(bytes, "text/csv", $"bank_kocurme_{il}_{ay:D2}.csv");
        }

        // ── Köməkçilər ───────────────────────────────────────────
        private async Task FilterSiyahilariniDoldur(
            int cIl, int cAy, int? isciId, int? deptId)
        {
            ViewBag.Iller = IlSiyahisi(cIl);
            ViewBag.Aylar = AySiyahisi(cAy);

            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderBy(x => x.Soyad)
                .ToListAsync();

            ViewBag.Isciler = isciler
                .Select(x => new SelectListItem(
                    $"{x.Soyad} {x.Ad}", x.Id.ToString(), x.Id == isciId))
                .ToList();

            var deptler = await _unitOfWork.Repository<Departament>()
                .Query()
                .Where(x => !x.Silinib)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            ViewBag.Departamentler = deptler
                .Select(x => new SelectListItem(x.Ad, x.Id.ToString(), x.Id == deptId))
                .ToList();
        }

        private async Task HesablaFormSiyahilariDoldur()
        {
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => x.Status == IsciStatus.Aktiv && !x.Silinib)
                .OrderBy(x => x.Soyad)
                .ToListAsync();

            ViewBag.Isciler = isciler
                .Select(x => new SelectListItem($"{x.Soyad} {x.Ad}", x.Id.ToString()))
                .ToList();

            ViewBag.Iller = IlSiyahisi(DateTime.Now.Year);
            ViewBag.Aylar = AySiyahisi(DateTime.Now.Month);
        }

        private List<SelectListItem> IlSiyahisi(int secili) =>
            Enumerable.Range(DateTime.Now.Year - 2, 4)
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), x == secili))
                .ToList();

        private List<SelectListItem> AySiyahisi(int secili) =>
            Enumerable.Range(1, 12)
                .Select(x => new SelectListItem(
                    new DateTime(2000, x, 1).ToString("MMMM"),
                    x.ToString(), x == secili))
                .ToList();
    }
}
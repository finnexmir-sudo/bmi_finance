using FinNex.Domain;
using FinNex.Application.DTOs.HR.Maas;
using FinNex.Application.Interfaces.HR;
using FinNex.Application.Interfaces.Maas_If;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Entities.Structure;
using FinNex.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
    public class MaasController : Controller
    {
        private readonly IMaasService _maasService;
        private readonly IMaasHesablamaService _hesablamaService;
        private readonly IUnitOfWork _unitOfWork;

        public MaasController(
            IMaasService maasService,
            IMaasHesablamaService hesablamaService,
            IUnitOfWork unitOfWork)
        {
            _maasService = maasService;
            _hesablamaService = hesablamaService;
            _unitOfWork = unitOfWork;
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

            ViewData["Title"] = $"Əmək Haqqı — {cIl}/{cAy:D2}";
            return View(listDto);
        }

        // ── GET /HR/Maas/Hesabla ─────────────────────────────────
        [HttpGet]
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
        public async Task<IActionResult> Hesabla(FerdiHesablaInputDto input)
        {
            if (!ModelState.IsValid)
            {
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
        public async Task<IActionResult> TopluHesabla(int? il, int? ay)
        {
            var cIl = il ?? DateTime.Now.Year;
            var cAy = ay ?? DateTime.Now.Month;

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

            ViewBag.Il = cIl;
            ViewBag.Ay = cAy;
            ViewBag.Hesablanmis = hesablanmis;
            ViewBag.CariMaasMap = cariMaasMap;
            ViewBag.IbanMap = ibanMap;
            ViewBag.Iller = IlSiyahisi(cIl);
            ViewBag.Aylar = AySiyahisi(cAy);

            ViewData["Title"] = "Toplu Maaş Hesablaması";
            return View(isciler);
        }

        // ── POST /HR/Maas/TopluHesablaEt ─────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> TopluHesablaEt(
            int il, int ay,
            [FromForm] List<FerdiElaveDto> ferdiElaveler)
        {
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

            return RedirectToAction(nameof(Index), new { il, ay });
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
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusDeyis(int id, MaasStatus yeniStatus, int il, int ay)
        {
            if (yeniStatus == MaasStatus.Odenildi)
            {
                var m = await _unitOfWork.Repository<Maas>()
                    .Query()
                    .Where(x => x.Id == id)
                    .Include(x => x.Isci).ThenInclude(i => i.Maliye)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(m?.Isci?.Maliye?.BankHesabNo))
                {
                    TempData["Error"] = "İşçinin IBAN məlumatı yoxdur.";
                    return RedirectToAction(nameof(Index), new { il, ay });
                }
            }

            var r = await _maasService.StatusDeyisAsync(id, yeniStatus);
            TempData[r.Success ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Index), new { il, ay });
        }

        // ── POST /HR/Maas/Sil ────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
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
        public async Task<IActionResult> BankFayliYukle(int il, int ay)
        {
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib && x.Il == il && x.Ay == ay &&
                            x.Status == MaasStatus.Tesdiqlendi)
                .Include(x => x.Isci).ThenInclude(i => i.Maliye)
                .ToListAsync();

            if (!maaslar.Any())
            {
                TempData["Error"] = "Təsdiqlənmiş maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            var satirlar = new List<string> { "Ad Soyad;IBAN;Məbləğ;İzah;Tarix" };
            foreach (var m in maaslar)
            {
                var iban = m.Isci.Maliye?.BankHesabNo ?? "";
                satirlar.Add(
                    $"{m.Isci.Ad} {m.Isci.Soyad};{iban};{m.NetMebleg:F2};" +
                    $"{il}/{ay:D2} əmək haqqı;{DateTime.Now:dd.MM.yyyy}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(string.Join("\n", satirlar));
            return File(bytes, "text/csv", $"maas_{il}_{ay:D2}.csv");
        }

        // ── GET /HR/Maas/BankKocurme ─────────────────────────────
        // IBAN;FullName;NetAmount;Currency;Description formatında bank köçürmə faylı
        [HttpGet]
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
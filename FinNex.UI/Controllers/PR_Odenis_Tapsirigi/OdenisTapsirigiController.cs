using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Application.Services.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Extensions;
using FinNex.Domain.Interfaces;
using FinNex.UI.Services.PR_Odenis_Tapsirigi;
using FinNex.UI.ViewModels.PR_Odenis_Tapsirigi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Controllers
{
    [Authorize]
    public class OdenisTapsirigiController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IOdenisTapsirigiNomreService _odenisNomreService;
        public OdenisTapsirigiController(IUnitOfWork uow, IOdenisTapsirigiNomreService odenisNomreService)
        {
            _uow = uow;
            _odenisNomreService = odenisNomreService;
        }

        // Ana hub sehifesi
        public IActionResult Index()
        {
            return View();

        }

        // Odenis tapsiriglarinin siyahisi
        public async Task<IActionResult> Siyahi()
        {
            var list = await _uow
                .Repository<OdenisTapsirigi>()
                .HamisiniGetirAsync(
                    include: q => q
                        .Include(x => x.OduyenMusteri)
                        .Include(x => x.AlanMusteri)
                );

            return View(list);
        }


        // ➕ YENİ FORM
        public async Task<IActionResult> Create()
        {
            var vm = new OdenisTapsirigiCreateVM
            {
                Banklar = (await _uow.Repository<Bank>()
                                .HamisiniGetirAsync())
                                .ToList(),

                Musteriler = (await _uow.Repository<Musteri>()
                                    .HamisiniGetirAsync())
                                    .ToList()
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> Create(OdenisTapsirigiCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                // dropdown-lar boş qalmasın deyə
                vm.Banklar = (await _uow.Repository<Bank>().HamisiniGetirAsync()).ToList();
                vm.Musteriler = (await _uow.Repository<Musteri>().HamisiniGetirAsync()).ToList();
                return View(vm);
            }

            await _uow.Repository<OdenisTapsirigi>()
                      .YaratAsync(vm.Odenis);

            await _uow.YaddaSaxlaAsync();

            return RedirectToAction(nameof(Index));
        }


        // API: Bank kodu ile axtar
        [HttpGet]
        public async Task<IActionResult> BankiKodlaAxtar(string kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
                return Json(new { tapildi = false });

            var bank = await _uow.Repository<Bank>()
                .GetirAsync(b => b.Kod == kod.Trim());

            if (bank == null)
                return Json(new { tapildi = false });

            return Json(new
            {
                tapildi = true,
                id = bank.Id,
                ad = bank.Ad,
                kod = bank.Kod,
                voen = bank.Voen,
                muxHesab = bank.MuxHesab,
                swiftBic = bank.SwiftBic
            });
        }

        // API: Musteri VOEN ile axtar
        [HttpGet]
        public async Task<IActionResult> MusteriVoenleAxtar(string voen)
        {
            if (string.IsNullOrWhiteSpace(voen))
                return Json(new { tapildi = false });

            var musteriler = await _uow.Repository<Musteri>()
                .HamisiniGetirAsync(
                    predicate: m => m.Voen == voen.Trim(),
                    include: q => q.Include(m => m.MusteriHesablari)
                );

            if (!musteriler.Any())
                return Json(new { tapildi = false });

            var musteri = musteriler.First();

            return Json(new
            {
                tapildi = true,
                id = musteri.Id,
                ad = musteri.Ad,
                voen = musteri.Voen,
                hesablar = musteri.MusteriHesablari.Select(h => new
                {
                    id = h.Id,
                    iban = h.Iban,
                    valyuta = h.Valyuta
                }).ToList()
            });
        }

        // API: Meblegi soze cevir
        [HttpGet]
        public IActionResult MeblegiSoze(string mebleg)
        {
            if (!decimal.TryParse(mebleg?.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal deger))
                return Json(new { metn = "" });

            return Json(new { metn = deger.AzcaSozle() });
        }

        [HttpPost]
        public async Task<IActionResult> GenerateWord([FromBody] OdenisTapsirigiWordDto dto)
        {
            int nomre = await _odenisNomreService.NovbetiNomreAlAsync();

            dto.Nomre = nomre.ToString("D6"); // 000001
            dto.Tarix = DateTime.Now.ToString("dd MMMM yyyy");

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "Files",
                "Word",
                "Odenis_tapsirigi.docx"
            );

            var bytes = OdenisTapsirigiWordService.GenerateFromTemplate(templatePath, dto);

            var fileName = $"OdenisTapsirigi_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName
            );
        }


    }

}

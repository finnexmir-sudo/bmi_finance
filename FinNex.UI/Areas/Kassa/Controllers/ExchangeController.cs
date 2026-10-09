using FinNex.Application.DTOs.Kassa;
using FinNex.Application.Helpers.Kredit;
using FinNex.Application.Interfaces.Kassa;
using FinNex.Application.Interfaces.Kurval;
using FinNex.Application.Services.Kassa;
using FinNex.Domain;
using FinNex.Domain.Entities.HR;
using FinNex.UI.Areas.Kassa.ViewModels;
using FinNex.UI.Services.Kredit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FinNex.UI.Areas.Kassa.Controllers
{
    /// <summary>
    /// Kassanın günlük valyuta alış-satış kursunu daxil etdiyi ekran — BMI
    /// "frmExchange" modulunun köçürülməsi (09.10.2026). Yalnız RoleNames.Kassa
    /// roluna sahib işçilər (+ Admin) buraya giriş yaza bilər — Avtopark →
    /// Açar jurnalı ilə EYNİ rol, istifadəçinin öz qərarı (09.10.2026).
    /// </summary>
    [Area("Kassa")]
    [Authorize(Roles = RoleNames.Kassa + "," + RoleNames.Admin)]
    public class ExchangeController : Controller
    {
        private readonly IKassaKursService _kassaKursService;
        private readonly IBmiValyutaService _bmiValyutaService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _config;

        // kurval kodları (IBmiValyutaService.Ehtiyat ilə eyni) — "01" USD, "02" AVRO.
        private const string UsdKodu = "01";
        private const string AvroKodu = "02";

        public ExchangeController(
            IKassaKursService kassaKursService,
            IBmiValyutaService bmiValyutaService,
            UserManager<AppUser> userManager,
            IConfiguration config)
        {
            _kassaKursService = kassaKursService;
            _bmiValyutaService = bmiValyutaService;
            _userManager = userManager;
            _config = config;
        }

        // GET /Kassa/Exchange?tarix=2026-10-09
        public async Task<IActionResult> Index(DateTime? tarix)
        {
            ViewData["Title"] = "Exchange — Valyuta kursu";

            var secilenTarix = (tarix ?? DateTime.Today).Date;
            var bugun = DateTime.Today;

            // ⚠️ HAMISI ARDICIL ÇAĞIRILIR — paralel (Task başladıb sonra await
            // etmək) bütün bu çağırışlar EYNİ scoped IUnitOfWork/DbContext-i
            // paylaşdığı üçün "A second operation was started on this context
            // instance…" ilə sındı (09.10.2026, real hadisə — bax CLAUDE.md
            // "Bildirişlər — Paralel Yazı" ilə EYNİ tələ, bu dəfə oxumada).
            // MB (CBAR) kursu HƏMİŞƏ bugünkü gün üçündür (istifadəçi tələbi:
            // "cari günə") — seçilmiş tarixdən ASILI DEYİL.
            var vm = new ExchangeIndexVM
            {
                Gunluk = await _kassaKursService.GunlukGetirAsync(secilenTarix),
                SonQeydler = await _kassaKursService.SonBeyannameleriGetirAsync(),
                UsdMbKurs = await _bmiValyutaService.KursAsync(UsdKodu, bugun),
                AvroMbKurs = await _bmiValyutaService.KursAsync(AvroKodu, bugun),
                MbTarix = bugun
            };

            return View(vm);
        }

        // POST /Kassa/Exchange/Saxla
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Saxla(ExchangeSaxlaVM vm)
        {
            var isciId = await GetCurrentIsciIdAsync();
            if (isciId == null)
            {
                TempData["Error"] = "İşçi profiliniz tapılmadı.";
                return RedirectToAction(nameof(Index), new { tarix = vm.Tarix });
            }

            var dto = new KassaKursSaxlaDto
            {
                Tarix = vm.Tarix,
                Setirler = new List<KassaKursSetriDto>
                {
                    new() { Valyuta = "USD",  NagdAlis = vm.USD_NA,  NagdSatis = vm.USD_NS,  QeyriNagdAlis = vm.USD_QNA,  QeyriNagdSatis = vm.USD_QNS },
                    new() { Valyuta = "AVRO", NagdAlis = vm.AVRO_NA, NagdSatis = vm.AVRO_NS, QeyriNagdAlis = vm.AVRO_QNA, QeyriNagdSatis = vm.AVRO_QNS },
                    new() { Valyuta = "IRR",  NagdAlis = vm.IRR_NA,  NagdSatis = vm.IRR_NS,  QeyriNagdAlis = vm.IRR_QNA,  QeyriNagdSatis = vm.IRR_QNS },
                    new() { Valyuta = "AED",  NagdAlis = vm.AED_NA,  NagdSatis = vm.AED_NS,  QeyriNagdAlis = vm.AED_QNA,  QeyriNagdSatis = vm.AED_QNS },
                    new() { Valyuta = "RUB",  NagdAlis = vm.RUB_NA,  NagdSatis = vm.RUB_NS,  QeyriNagdAlis = vm.RUB_QNA,  QeyriNagdSatis = vm.RUB_QNS },
                }
            };

            var sonuc = await _kassaKursService.SaxlaAsync(dto, isciId.Value);
            // User-area ortaq layout TempData["Success"]/["Error"] gözləyir
            // (_UserLayout.cshtml) — "StatusMessage" YAZMA, görünməz qalar.
            if (sonuc.Success)
                TempData["Success"] = sonuc.Message;
            else
                TempData["Error"] = sonuc.Message;

            return RedirectToAction(nameof(Index), new { tarix = vm.Tarix });
        }

        // GET /Kassa/Exchange/Word?beyannameId=5
        //
        // BMI "frmExchange"-in "WordPrint" düyməsinin köçürülməsi (10.10.2026).
        // BMI lokal Word-ü Process.Start ilə açırdı — veb mühitdə qarşılığı
        // brauzerə yükləmədir (digər bütün KreditWordService çağırışları ilə
        // eyni naxış). "WordPrint" bazada heç bir sütun/bayraq deyil (yoxlanıldı:
        // BMI-də belə bir sütun ümumiyyətlə yoxdur) — ona görə burada da
        // "artıq yaradılıb" statusu saxlanmır, hər çağırışda yenidən yaradılır.
        public async Task<IActionResult> Word(int beyannameId)
        {
            var gun = await _kassaKursService.BeyannameGetirAsync(beyannameId);
            if (gun == null)
            {
                TempData["Error"] = "Beyannamə tapılmadı.";
                return RedirectToAction(nameof(Index));
            }

            var sablon = SablonYolu();
            if (!System.IO.File.Exists(sablon))
            {
                TempData["Error"] = $"Exchange Word şablonu tapılmadı: {sablon}";
                return RedirectToAction(nameof(Index), new { tarix = gun.Tarix });
            }

            var tokenler = new Dictionary<string, string?>
            {
                // Şablonda İKİ dəfə keçir — ikisi də eyni mətni götürür (BMI-dəki kimi).
                ["{tar}"] = KreditSozeCevir.TarixiSoze(gun.Tarix)
            };

            // ⚠️ `WordSablonSirasi` (USD→AVRO→RUB→AED→IRR) işlədilir, EKRANDAKI
            // `Valyutalar` sırası (USD→AVRO→IRR→AED→RUB) YOX — şablonun {v1}…{v25}
            // yer tutucuları BMI-nin öz sorğu sırasına bağlıdır (bax servisdəki şərh).
            var setirlerByValyuta = gun.Setirler.ToDictionary(s => s.Valyuta);
            var sayac = 1;
            foreach (var v in KassaKursService.WordSablonSirasi)
            {
                var s = setirlerByValyuta.TryGetValue(v, out var setir) ? setir : new KassaKursSetriDto { Valyuta = v };
                tokenler[$"{{v{sayac++}}}"] = v;
                tokenler[$"{{v{sayac++}}}"] = Mezenne(s.NagdAlis);
                tokenler[$"{{v{sayac++}}}"] = Mezenne(s.NagdSatis);
                tokenler[$"{{v{sayac++}}}"] = Mezenne(s.QeyriNagdAlis);
                tokenler[$"{{v{sayac++}}}"] = Mezenne(s.QeyriNagdSatis);
            }

            // Şablonda köhnə Azəri simvol-şriftləri (Azeri_Bookman_Lat, Times Latin)
            // var — bunlar müasir Unicode Azəri hərflərini (ə/ı/ö/ü/ş/ç, məs. il
            // sıra şəkilçisində: "-cı"/"-cü") qopuq göstərir. `unicodeSrift` yalnız
            // ASCII-dən kənar hərf olan dəyərlərdə run şriftini əvəz edir (bax
            // KreditWordService-in öz izahı) — rəqəmlər (ASCII) toxunulmur.
            var bayt = KreditWordService.Doldur(sablon, tokenler, unicodeSrift: "Times New Roman");
            var ad = $"Exchange_{gun.Tarix:yyyyMMdd}_{beyannameId}.docx";
            return File(bayt, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ad);
        }

        // Şablon DMS-dədir (CLAUDE.md — fayllar wwwroot-a YAZILMIR/oxunmur).
        // Repodakı nüsxə: docs/sablon/kassa/Exchange.docx
        private string SablonYolu()
        {
            var dms = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
            return Path.Combine(dms, "hesabat-sablonlari", "kassa", "Exchange.docx");
        }

        // İnsan oxuyan Word sənədinə yazılır — server mədəniyyəti (az-AZ) ilə
        // vergüllü format düzgündür (CLAUDE.md "Razor → CSS/JS Rəqəm" qaydası
        // yalnız maşın-oxuyan CSS/JS üçündür, bura aid deyil).
        private static string Mezenne(decimal? d) => (d ?? 0).ToString("0.0000");

        private async Task<int?> GetCurrentIsciIdAsync()
        {
            var appUser = await _userManager.GetUserAsync(User);
            return appUser?.IsciId;
        }
    }
}

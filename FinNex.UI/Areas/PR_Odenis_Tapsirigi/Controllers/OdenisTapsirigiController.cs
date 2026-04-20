using DocumentFormat.OpenXml.Bibliography;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Musteri;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Extensions;
using FinNex.Domain.Interfaces;
using FinNex.UI.Areas.PR_Odenis_Tapsirigi.ViewModels;
using FinNex.UI.Services.PR_Odenis_Tapsirigi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static FinNex.Application.DTOs.PR_Odenis_Tapsirigi.Bank.BankUpdateDto;

namespace FinNex.UI.Areas.PR_Odenis_Tapsirigi.Controllers
{
    [Authorize]
    [Area("PR_Odenis_Tapsirigi")]
    public class OdenisTapsirigiController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IOdenisTapsirigiNomreService _odenisNomreService;
        private readonly IOdenisTapsirigiService _odenisService;
        private readonly FinNex.DataAccess.Contexts.AppDbContext _db;
        public OdenisTapsirigiController(IUnitOfWork uow, IOdenisTapsirigiNomreService odenisNomreService, IOdenisTapsirigiService odenisTapsirigiService, FinNex.DataAccess.Contexts.AppDbContext db)
        {
            _uow = uow;
            _odenisNomreService = odenisNomreService;
            _odenisService = odenisTapsirigiService;
            _db = db;
        }

        // Hub artıq lazım deyil - birbaşa siyahıya yönləndirilir
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Siyahi));
        }

        // Odenis tapsiriglarinin siyahisi
        [HttpGet]
        public async Task<IActionResult> Siyahi(
     string? nomre = null, string? oduyenMusteri = null, string? alanMusteri = null, string? valyuta = null,
     DateTime? tarixden = null, DateTime? tarixe = null, int sehife = 1)
        {
            const int sehifeOlcusu = 15;

            // Bazada filtrlə — memory-ə hamısını yükləmə
            var sorgu = _db.OdenisTapsiriqlari
                .AsNoTracking()
                .Where(x => !x.Silinib)
                .Include(x => x.OduyenMusteri)
                .Include(x => x.AlanMusteri)
                .Include(x => x.Valyuta)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(nomre))
                sorgu = sorgu.Where(x => x.Nomre.Contains(nomre.Trim()));

            if (!string.IsNullOrWhiteSpace(oduyenMusteri))
                sorgu = sorgu.Where(x =>
                    x.OduyenMusteri != null &&
                    x.OduyenMusteri.Ad.Contains(oduyenMusteri.Trim()));

            if (!string.IsNullOrWhiteSpace(alanMusteri))
                sorgu = sorgu.Where(x =>
                    x.AlanMusteri != null &&
                    x.AlanMusteri.Ad.Contains(alanMusteri.Trim()));

            if (!string.IsNullOrWhiteSpace(valyuta))
                sorgu = sorgu.Where(x => x.Valyuta.Ad == valyuta);

            if (tarixden.HasValue)
                sorgu = sorgu.Where(x => x.Tarix >= tarixden.Value.Date);

            if (tarixe.HasValue)
                sorgu = sorgu.Where(x => x.Tarix <= tarixe.Value.Date.AddDays(1).AddTicks(-1));

            // ── Sıralama + Səhifələmə (bazada) ──
            var umumiSay = await sorgu.CountAsync();
            var sehifeSayi = (int)Math.Ceiling((double)umumiSay / sehifeOlcusu);

            if (sehife < 1) sehife = 1;
            if (sehife > sehifeSayi && sehifeSayi > 0) sehife = sehifeSayi;

            var melumtlar = await sorgu
                .OrderByDescending(x => x.Tarix)
                .Skip((sehife - 1) * sehifeOlcusu)
                .Take(sehifeOlcusu)
                .ToListAsync();

            var vm = new OdenisTapsirigiSiyahiVM
            {
                Melumtlar = melumtlar,
                UmumiSay = umumiSay,
                CariSehife = sehife,
                SehifeOlcusu = sehifeOlcusu,
                Filter = new OdenisTapsirigiFilterDto
                {
                    Nomre = nomre,
                    OduyenMusteri = oduyenMusteri,
                    AlanMusteri = alanMusteri,
                    Valyuta = valyuta,
                    Tarixden = tarixden,
                    TarixeQeder = tarixe
                }
            };

            return View(vm);
        }


        // ➕ YENİ FORM
        [HttpGet]
        public async Task<IActionResult> Create(string? ad = null, string? voen = null, string? hesab = null)
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

            // Gələn müştəri məlumatlarını doldur
            if (!string.IsNullOrEmpty(ad) || !string.IsNullOrEmpty(voen))
            {
                vm.MusteriCreateDto = new MusteriCreateDto
                {
                    Ad = ad,
                    Voen = voen,
                    Hesab = hesab
                };
            }

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
                .GetirAsync(b => b.Kod == kod.Trim(),
                include: q => q.Include(a => a.BankHesablari));

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
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GenerateWord([FromBody] OdenisTapsirigiWordDto dto)
        {
            try
            {
                int nomre = await _odenisNomreService.NovbetiNomreAlAsync();
                dto.Nomre = nomre.ToString("D6");
                dto.Tarix = FormatTarix(DateTime.Now);

                var templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot", "Files", "Word", "Odenis_tapsirigi.docx"
                );

                var bytes = OdenisTapsirigiWordService.GenerateFromTemplate(templatePath, dto);
                var fileName = $"OdenisTapsirigi_{DateTime.Now:yyyyMMdd_HHmmss}.docx";

                // ID validasiyası
                if (!int.TryParse(dto.OduyenBankId, out int obId) || obId <= 0 ||
                    !int.TryParse(dto.AlanBankId, out int abId) || abId <= 0 ||
                    !int.TryParse(dto.OduyenMusteriId, out int omId) || omId <= 0 ||
                    !int.TryParse(dto.OduyenHesabId, out int ohId) || ohId <= 0 ||
                    !int.TryParse(dto.AlanMusteriId, out int amId) || amId <= 0 ||
                    !int.TryParse(dto.AlanHesabId, out int ahId) || ahId <= 0 ||
                    !int.TryParse(dto.ValyutaId, out int vId) || vId <= 0)
                {
                    return BadRequest("Bank, müştəri və ya hesab məlumatları natamam. Formu yenidən doldurun.");
                }

                var odenis = new OdenisTapsirigi
                {
                    Nomre = dto.Nomre,
                    Tarix = DateTime.Now,
                    OduyenBankId = obId,
                    AlanBankId = abId,
                    OduyenMusteriId = omId,
                    OduyenHesabId = ohId,
                    AlanMusteriId = amId,
                    AlanHesabId = ahId,
                    ValyutaId = vId,
                    Mebleg = decimal.Parse(dto.Mebleg, System.Globalization.CultureInfo.InvariantCulture),
                    MeblegYazi = dto.MeblegYazi,
                    Teyinat = dto.Teyinat,
                    ElaveInformasiya = dto.ElaveInfo,
                    BudceTesnifatininKodu = dto.BudceTesnifatininKodu,
                    BudceSeviyyesininKodu = dto.BudceSeviyyesininKodu
                };

                await _uow.Repository<OdenisTapsirigi>().YaratAsync(odenis);
                await _uow.YaddaSaxlaAsync();

                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Word sənədi yaradılarkən xəta baş verdi. Zəhmət olmasa yenidən cəhd edin.");
            }
        }

        private static string FormatTarix(DateTime tarix)
        {
            string[] aylar = {
        "yanvar", "fevral", "mart", "aprel", "may", "iyun",
        "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr"
    };

            int il = tarix.Year;
            string ay = aylar[tarix.Month - 1];
            int gun = tarix.Day;

            // İlin son rəqəminə görə şəkilçi
            int sonReqem = il % 10;
            string sekilci = sonReqem switch
            {
                1 => "ci",
                2 => "ci",
                3 => "cü",
                4 => "cü",
                5 => "ci",
                6 => "cı",
                7 => "ci",
                8 => "ci",
                9 => "cu",
                0 => "cu",
                _ => "ci"
            };

            return $"{gun} {ay} {il}-{sekilci} il";
        }

        // Bank yoxla və ya yarat
        // İstifadəçi manual yaza bilər — bazada varsa tapılır, yoxsa yeni bank
        // və hesab yaradılır. Hansı sahə doldurulubsa ona görə axtarırıq
        // (kod > voen > swift > ad).
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BankYoxlaVeYarat([FromBody] BankYoxlaDto dto)
        {
            if (dto == null)
                return BadRequest("Bank məlumatı göndərilmədi.");

            var kod      = (dto.Kod ?? "").Trim();
            var ad       = (dto.Ad ?? "").Trim();
            var voen     = (dto.Voen ?? "").Trim();
            var swift    = (dto.SwiftBic ?? "").Trim();
            var muxHesab = (dto.MuxHesab ?? "").Trim();

            // Ən azı bir identifikator olmalıdır — yoxsa DB-də izləmə mənasız
            if (string.IsNullOrEmpty(kod) && string.IsNullOrEmpty(voen)
                && string.IsNullOrEmpty(swift) && string.IsNullOrEmpty(ad))
            {
                return BadRequest("Bank məlumatları boşdur. Ad, kod, VÖEN və ya SWIFT-dən ən azı biri olmalıdır.");
            }

            // Çoxmetrlik axtarış — ilk uyğun sahəyə görə tap
            var banklar = await _uow.Repository<Bank>()
                .HamisiniGetirAsync(
                    predicate: b =>
                        (!string.IsNullOrEmpty(kod) && b.Kod == kod) ||
                        (!string.IsNullOrEmpty(voen) && b.Voen == voen) ||
                        (!string.IsNullOrEmpty(swift) && b.SwiftBic == swift) ||
                        (!string.IsNullOrEmpty(ad) && b.Ad == ad),
                    include: q => q.Include(b => b.BankHesablari)
                );

            Bank bank;
            bool tapildi = banklar.Any();

            if (tapildi)
            {
                bank = banklar.First();
            }
            else
            {
                bank = new Bank
                {
                    Ad = ad,
                    Kod = kod,
                    Voen = voen,
                    SwiftBic = swift
                };
                await _uow.Repository<Bank>().YaratAsync(bank);
                await _uow.YaddaSaxlaAsync();
            }

            // Hesabı tap və ya yarat — muxHesab boş ola bilər
            BankHesabi hesab;
            if (!string.IsNullOrEmpty(muxHesab))
            {
                hesab = bank.BankHesablari.FirstOrDefault(h => h.Iban == muxHesab)
                     ?? new BankHesabi { BankId = bank.Id, Iban = muxHesab, ValyutaId = 1 };
                if (hesab.Id == 0)
                {
                    await _uow.Repository<BankHesabi>().YaratAsync(hesab);
                    await _uow.YaddaSaxlaAsync();
                }
            }
            else
            {
                // IBAN yoxdur — ilk mövcud hesabı götür, yoxdursa boş hesab yarat
                hesab = bank.BankHesablari.FirstOrDefault()
                     ?? new BankHesabi { BankId = bank.Id, Iban = "", ValyutaId = 1 };
                if (hesab.Id == 0)
                {
                    await _uow.Repository<BankHesabi>().YaratAsync(hesab);
                    await _uow.YaddaSaxlaAsync();
                }
            }

            return Json(new
            {
                tapildi,
                id = bank.Id,
                hesabId = hesab.Id
            });
        }

        // Musteri yoxla və ya yarat
        // Axtar ilə tapılmayan müştərilər manual yazılır — bu metod onları
        // bazaya əlavə edir (VÖEN/Ad üzrə axtarır, hər ikisi boşdursa xəta verir).
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MusteriYoxlaVeYarat([FromBody] MusteriYoxlaDto dto)
        {
            if (dto == null)
                return BadRequest("Müştəri məlumatı göndərilmədi.");

            var voen  = (dto.Voen ?? "").Trim();
            var ad    = (dto.Ad ?? "").Trim();
            var hesab = (dto.Hesab ?? "").Trim();

            if (string.IsNullOrEmpty(voen) && string.IsNullOrEmpty(ad))
                return BadRequest("Müştərinin ən azı adı və ya VÖEN-i daxil edilməlidir.");

            // VÖEN varsa onunla tap; yoxdursa ada görə tap
            var musteriler = await _uow.Repository<Musteri>()
                .HamisiniGetirAsync(
                    predicate: m =>
                        (!string.IsNullOrEmpty(voen) && m.Voen == voen) ||
                        (string.IsNullOrEmpty(voen) && !string.IsNullOrEmpty(ad) && m.Ad == ad),
                    include: q => q.Include(m => m.MusteriHesablari)
                );

            Musteri movcud;
            bool tapildi = musteriler.Any();

            if (tapildi)
            {
                movcud = musteriler.First();
            }
            else
            {
                movcud = new Musteri { Ad = ad, Voen = voen };
                await _uow.Repository<Musteri>().YaratAsync(movcud);
                await _uow.YaddaSaxlaAsync();
            }

            // IBAN üzrə hesab tap və ya yarat. IBAN boşdursa, ilk mövcud hesabı
            // qaytar — yoxdursa boş hesab yarat (sərt validasiya məzənnədə yox
            // client-side formYoxla-da gedir).
            MusteriHesabi musteriHesabi;
            if (!string.IsNullOrEmpty(hesab))
            {
                musteriHesabi = movcud.MusteriHesablari.FirstOrDefault(h => h.Iban == hesab)
                             ?? new MusteriHesabi { MusteriId = movcud.Id, Iban = hesab, ValyutaId = 1 };
                if (musteriHesabi.Id == 0)
                {
                    await _uow.Repository<MusteriHesabi>().YaratAsync(musteriHesabi);
                    await _uow.YaddaSaxlaAsync();
                }
            }
            else
            {
                musteriHesabi = movcud.MusteriHesablari.FirstOrDefault()
                             ?? new MusteriHesabi { MusteriId = movcud.Id, Iban = "", ValyutaId = 1 };
                if (musteriHesabi.Id == 0)
                {
                    await _uow.Repository<MusteriHesabi>().YaratAsync(musteriHesabi);
                    await _uow.YaddaSaxlaAsync();
                }
            }

            return Json(new
            {
                tapildi,
                id = movcud.Id,
                hesabId = musteriHesabi.Id,
                hesabIban = musteriHesabi.Iban
            });
        }

        [HttpGet]
        public async Task<IActionResult> BankHesablariGetir(int bankId)
        {
            var hesablar = await _uow.Repository<BankHesabi>()
                .HamisiniGetirAsync(predicate: h => h.BankId == bankId);

            return Json(hesablar.Select(h => new { id = h.Id, iban = h.Iban }));
        }

        // Detail
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var odenis = await _uow.Repository<OdenisTapsirigi>()
                .GetirAsync(
                    x => x.Id == id,
                    include: q => q
                        .Include(x => x.OduyenBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.AlanBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.OduyenMusteri).ThenInclude(m => m.MusteriHesablari)
                        .Include(x => x.AlanMusteri).ThenInclude(m => m.MusteriHesablari)
                        .Include(x => x.OduyenHesab)
                        .Include(x => x.AlanHesab)
                        .Include(x => x.Valyuta)
                );

            if (odenis == null) return NotFound();

            var vm = new OdenisTapsirigiDetailVM
            {
                Odenis = odenis,
                BugunDuzeli = odenis.Tarix.Date == DateTime.Today
            };

            return View(vm);
        }

        // Edit — inline POST
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Edit([FromBody] OdenisTapsirigiEditDto dto)
        {
            var odenis = await _uow.Repository<OdenisTapsirigi>()
                .GetirAsync(x => x.Id == dto.Id);

            if (odenis == null)
                return Json(new { ugurlu = false, xeta = "Sənəd tapılmadı" });

            if (odenis.Tarix.Date != DateTime.Today)
                return Json(new { ugurlu = false, xeta = "Yalnız bu gün yaradılmış sənədlər dəyişdirilə bilər" });

            // Sahələri yenilə
            odenis.Mebleg = dto.Mebleg;
            odenis.MeblegYazi = dto.MeblegYazi;
            odenis.Teyinat = dto.Teyinat;
            odenis.ElaveInformasiya = dto.ElaveInformasiya;
            odenis.BudceTesnifatininKodu = dto.BudceTesnifatininKodu;
            odenis.BudceSeviyyesininKodu = dto.BudceSeviyyesininKodu;

            await _uow.Repository<OdenisTapsirigi>().YenileAsync(odenis);
            await _uow.YaddaSaxlaAsync();

            return Json(new { ugurlu = true });
        }

        // Yenidən yaz
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> YenidenYaz([FromBody] int id)
        {
            var kohne = await _uow.Repository<OdenisTapsirigi>()
                .GetirAsync(
                    x => x.Id == id,
                    include: q => q
                        .Include(x => x.OduyenBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.AlanBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.OduyenMusteri)
                        .Include(x => x.AlanMusteri)
                        .Include(x => x.OduyenHesab)
                        .Include(x => x.AlanHesab)
                        .Include(x => x.Valyuta)
                );

            if (kohne == null)
                return Json(new { ugurlu = false, xeta = "Sənəd tapılmadı" });

            // Yeni boş sənəd yarat (kopyası)
            var yeni = new OdenisTapsirigi
            {
                Nomre = "",   // GenerateWord zamanı verilir
                Tarix = DateTime.Now,
                OduyenBankId = kohne.OduyenBankId,
                AlanBankId = kohne.AlanBankId,
                OduyenMusteriId = kohne.OduyenMusteriId,
                OduyenHesabId = kohne.OduyenHesabId,
                AlanMusteriId = kohne.AlanMusteriId,
                AlanHesabId = kohne.AlanHesabId,
                ValyutaId = kohne.ValyutaId,
                Mebleg = kohne.Mebleg,
                MeblegYazi = kohne.MeblegYazi,
                Teyinat = kohne.Teyinat,
                ElaveInformasiya = kohne.ElaveInformasiya,
                BudceTesnifatininKodu = kohne.BudceTesnifatininKodu,
                BudceSeviyyesininKodu = kohne.BudceSeviyyesininKodu
            };

            await _uow.Repository<OdenisTapsirigi>().YaratAsync(yeni);
            await _uow.YaddaSaxlaAsync();

            return Json(new { ugurlu = true, yeniId = yeni.Id });
        }

        [HttpGet]
        public async Task<IActionResult> YenidenYazForm(int id)
        {
            var kohne = await _uow.Repository<OdenisTapsirigi>()
                .GetirAsync(
                    x => x.Id == id,
                    include: q => q
                        .Include(x => x.OduyenBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.AlanBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.OduyenMusteri)
                        .Include(x => x.AlanMusteri)
                        .Include(x => x.OduyenHesab)
                        .Include(x => x.AlanHesab)
                        .Include(x => x.Valyuta)
                );

            if (kohne == null) return NotFound();

            var vm = new OdenisTapsirigiCreateVM
            {
                Banklar = (await _uow.Repository<Bank>().HamisiniGetirAsync()).ToList(),
                Musteriler = (await _uow.Repository<Musteri>().HamisiniGetirAsync()).ToList(),

                Odenis = new OdenisTapsirigi
                {
                    OduyenBankId = kohne.OduyenBankId,
                    AlanBankId = kohne.AlanBankId,
                    OduyenMusteriId = kohne.OduyenMusteriId,
                    OduyenHesabId = kohne.OduyenHesabId,
                    AlanMusteriId = kohne.AlanMusteriId,
                    AlanHesabId = kohne.AlanHesabId,
                    ValyutaId = kohne.ValyutaId,
                    Mebleg = kohne.Mebleg,
                    MeblegYazi = kohne.MeblegYazi,
                    Teyinat = kohne.Teyinat,
                    ElaveInformasiya = kohne.ElaveInformasiya,
                    BudceTesnifatininKodu = kohne.BudceTesnifatininKodu,
                    BudceSeviyyesininKodu = kohne.BudceSeviyyesininKodu
                },

                YenidenYazDto = new YenidenYazPrefillDto
                {
                    // Ödüyən bank
                    OduyenBankId = kohne.OduyenBankId,
                    OduyenBankAd = kohne.OduyenBank?.Ad,
                    OduyenBankKod = kohne.OduyenBank?.Kod,
                    OduyenBankVoen = kohne.OduyenBank?.Voen,
                    OduyenBankSwift = kohne.OduyenBank?.SwiftBic,
                    OduyenHesabId = kohne.OduyenHesabId,
                    OduyenHesabIban = kohne.OduyenHesab?.Iban,
                    // Ödüyən müştəri
                    OduyenMusteriId = kohne.OduyenMusteriId,
                    OduyenMusteriAd = kohne.OduyenMusteri?.Ad,
                    OduyenMusteriVoen = kohne.OduyenMusteri?.Voen,
                    // Alan bank
                    AlanBankId = kohne.AlanBankId,
                    AlanBankAd = kohne.AlanBank?.Ad,
                    AlanBankKod = kohne.AlanBank?.Kod,
                    AlanBankVoen = kohne.AlanBank?.Voen,
                    AlanBankSwift = kohne.AlanBank?.SwiftBic,
                    AlanHesabId = kohne.AlanHesabId,
                    AlanHesabIban = kohne.AlanHesab?.Iban,
                    // Alan müştəri
                    AlanMusteriId = kohne.AlanMusteriId,
                    AlanMusteriAd = kohne.AlanMusteri?.Ad,
                    AlanMusteriVoen = kohne.AlanMusteri?.Voen,
                    AlanMusteriHesabIban = kohne.AlanHesab?.Iban,   // ← əlavə field
                                                                    // Məbləğ
                    Valyuta = kohne.Valyuta?.Ad,
                    Mebleg = kohne.Mebleg,
                    MeblegYazi = kohne.MeblegYazi,
                    // Teyinat
                    Teyinat = kohne.Teyinat,
                    ElaveInformasiya = kohne.ElaveInformasiya,
                    BudceTesnifatininKodu = kohne.BudceTesnifatininKodu,
                    BudceSeviyyesininKodu = kohne.BudceSeviyyesininKodu
                }
            };

            return View("CreateKohne", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var odenis = await _uow.Repository<OdenisTapsirigi>()
                .GetirAsync(
                    x => x.Id == id,
                    include: q => q
                        .Include(x => x.OduyenBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.AlanBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.OduyenMusteri).ThenInclude(m => m.MusteriHesablari)
                        .Include(x => x.AlanMusteri).ThenInclude(m => m.MusteriHesablari)
                        .Include(x => x.OduyenHesab)
                        .Include(x => x.AlanHesab)
                        .Include(x => x.Valyuta));

            if (odenis == null) return NotFound();

            var vm = new OdenisTapsirigiCreateVM
            {
                Banklar = (await _uow.Repository<Bank>().HamisiniGetirAsync()).ToList(),
                Musteriler = (await _uow.Repository<Musteri>().HamisiniGetirAsync()).ToList(),
                OduyenBankHesablari = (await _uow.Repository<BankHesabi>()
                    .HamisiniGetirAsync(predicate: h => h.BankId == odenis.OduyenBankId)).ToList(),
                AlanBankHesablari = (await _uow.Repository<BankHesabi>()
                    .HamisiniGetirAsync(predicate: h => h.BankId == odenis.AlanBankId)).ToList(),

                Odenis = odenis,

                YenidenYazDto = new YenidenYazPrefillDto
                {
                    // Ödüyən bank
                    OduyenBankId = odenis.OduyenBankId,
                    OduyenBankAd = odenis.OduyenBank?.Ad,
                    OduyenBankKod = odenis.OduyenBank?.Kod,
                    OduyenBankVoen = odenis.OduyenBank?.Voen,
                    OduyenBankSwift = odenis.OduyenBank?.SwiftBic,
                    OduyenHesabId = odenis.OduyenHesabId,
                    OduyenHesabIban = odenis.OduyenHesab?.Iban,
                    // Ödüyən müştəri
                    OduyenMusteriId = odenis.OduyenMusteriId,
                    OduyenMusteriAd = odenis.OduyenMusteri?.Ad,
                    OduyenMusteriVoen = odenis.OduyenMusteri?.Voen,
                    // Alan bank
                    AlanBankId = odenis.AlanBankId,
                    AlanBankAd = odenis.AlanBank?.Ad,
                    AlanBankKod = odenis.AlanBank?.Kod,
                    AlanBankVoen = odenis.AlanBank?.Voen,
                    AlanBankSwift = odenis.AlanBank?.SwiftBic,
                    AlanHesabId = odenis.AlanHesabId,
                    AlanHesabIban = odenis.AlanHesab?.Iban,
                    // Alan müştəri
                    AlanMusteriId = odenis.AlanMusteriId,
                    AlanMusteriAd = odenis.AlanMusteri?.Ad,
                    AlanMusteriVoen = odenis.AlanMusteri?.Voen,
                    AlanMusteriHesabIban = odenis.AlanHesab?.Iban,
                    // Məbləğ
                    Valyuta = odenis.Valyuta?.Ad,
                    Mebleg = odenis.Mebleg,
                    MeblegYazi = odenis.MeblegYazi,
                    // Teyinat
                    Teyinat = odenis.Teyinat,
                    ElaveInformasiya = odenis.ElaveInformasiya,
                    BudceTesnifatininKodu = odenis.BudceTesnifatininKodu,
                    BudceSeviyyesininKodu = odenis.BudceSeviyyesininKodu
                }
            };

            return View(vm); // Update.cshtml açılır
        }

        [HttpGet]
        public async Task<IActionResult> EditVeWord(int id)
        {
            var odenis = await _uow.Repository<OdenisTapsirigi>()
                .GetirAsync(
                    x => x.Id == id,
                    include: q => q
                        .Include(x => x.OduyenBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.AlanBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.OduyenMusteri).ThenInclude(m => m.MusteriHesablari)
                        .Include(x => x.AlanMusteri).ThenInclude(m => m.MusteriHesablari)
                        .Include(x => x.OduyenHesab)
                        .Include(x => x.AlanHesab)
                        .Include(x => x.Valyuta));

            if (odenis == null) return NotFound();

            // Word hazırla
            var wordDto = new OdenisTapsirigiWordDto
            {
                Nomre = odenis.Nomre,
                Tarix = FormatTarix(odenis.Tarix),
                // ... bütün fieldlər Detail-dəki kimi
                OduyenBankAd = odenis.OduyenBank?.Ad,
                OduyenBankKod = odenis.OduyenBank?.Kod,
                // ... qalan fieldlər
            };

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "Files", "Word", "Odenis_tapsirigi.docx");

            var bytes = OdenisTapsirigiWordService.GenerateFromTemplate(templatePath, wordDto);
            var fileName = $"OdenisTapsirigi_{odenis.Nomre}.docx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditVeWord([FromBody] OdenisTapsirigiEditDto dto)
        {
            var odenis = await _uow.Repository<OdenisTapsirigi>()
                .GetirAsync(x => x.Id == dto.Id,
                    include: q => q
                        .Include(x => x.OduyenBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.AlanBank).ThenInclude(b => b.BankHesablari)
                        .Include(x => x.OduyenMusteri).ThenInclude(m => m.MusteriHesablari)
                        .Include(x => x.AlanMusteri).ThenInclude(m => m.MusteriHesablari)
                        .Include(x => x.OduyenHesab)
                        .Include(x => x.AlanHesab)
                        .Include(x => x.Valyuta));

            if (odenis == null)
                return Json(new { ugurlu = false, xeta = "Sənəd tapılmadı" });

            var bugun = await _odenisService.DbTarixiniGetirAsync();

            if (odenis.Tarix.Date != bugun.Date)
                return Json(new { ugurlu = false, xeta = "Yalnız bu gün yaradılmış sənədlər dəyişdirilə bilər" });

            // DB yenilə
            odenis.Mebleg = dto.Mebleg;
            odenis.MeblegYazi = dto.MeblegYazi;
            odenis.Teyinat = dto.Teyinat;
            odenis.ElaveInformasiya = dto.ElaveInformasiya;
            odenis.BudceTesnifatininKodu = dto.BudceTesnifatininKodu;
            odenis.BudceSeviyyesininKodu = dto.BudceSeviyyesininKodu;

            await _uow.Repository<OdenisTapsirigi>().YenileAsync(odenis);
            await _uow.YaddaSaxlaAsync();

            // Word hazırla
            var wordDto = new OdenisTapsirigiWordDto
            {
                Nomre = odenis.Nomre,
                Tarix = FormatTarix(odenis.Tarix),
                OduyenBankId = odenis.OduyenBankId.ToString(),
                OduyenBankAd = odenis.OduyenBank?.Ad,
                OduyenBankKod = odenis.OduyenBank?.Kod,
                OduyenBankVoen = odenis.OduyenBank?.Voen,
                OduyenBankSwift = odenis.OduyenBank?.SwiftBic,
                OduyenBankMuxbirHesab = odenis.OduyenHesab?.Iban,
                OduyenMusteriId = odenis.OduyenMusteriId.ToString(),
                OduyenMusteriAd = odenis.OduyenMusteri?.Ad,
                OduyenMusteriVoen = odenis.OduyenMusteri?.Voen,
                OduyenMusteriHesab = odenis.OduyenHesab?.Iban,
                AlanBankId = odenis.AlanBankId.ToString(),
                AlanBankAd = odenis.AlanBank?.Ad,
                AlanBankKod = odenis.AlanBank?.Kod,
                AlanBankVoen = odenis.AlanBank?.Voen,
                AlanBankSwift = odenis.AlanBank?.SwiftBic,
                AlanBankMuxbirHesab = odenis.AlanHesab?.Iban,
                AlanMusteriId = odenis.AlanMusteriId.ToString(),
                AlanMusteriAd = odenis.AlanMusteri?.Ad,
                AlanMusteriVoen = odenis.AlanMusteri?.Voen,
                AlanMusteriHesab = odenis.AlanHesab?.Iban,
                ValyutaId = odenis.ValyutaId.ToString(),
                Valyuta = odenis.Valyuta?.Ad,
                Mebleg = odenis.Mebleg.ToString(System.Globalization.CultureInfo.InvariantCulture),
                MeblegYazi = odenis.MeblegYazi,
                Teyinat = odenis.Teyinat,
                ElaveInfo = odenis.ElaveInformasiya,
                BudceTesnifatininKodu = odenis.BudceTesnifatininKodu,
                BudceSeviyyesininKodu = odenis.BudceSeviyyesininKodu
            };

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "Files", "Word", "Odenis_tapsirigi.docx");

            var bytes = OdenisTapsirigiWordService.GenerateFromTemplate(templatePath, wordDto);
            var fileName = $"OdenisTapsirigi_{odenis.Nomre}.docx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }
    }

}

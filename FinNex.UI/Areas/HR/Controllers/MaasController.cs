using System.Text.Json;
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
using Microsoft.AspNetCore.Hosting;
using FinNex.Application.Services.HR;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System.IO;

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
        private readonly IBildirisRouter _bildirisRouter;
        private readonly UserManager<AppUser> _userManager;
        private readonly IAyliqElaveService _ayliqElaveService;
        private readonly IMuhasibatHesabService _muhasibatHesabService;
        private readonly IWebHostEnvironment _env;

        public MaasController(
            IMaasService maasService,
            IMaasHesablamaService hesablamaService,
            IUnitOfWork unitOfWork,
            IBildirisService bildirisService,
            IBildirisRouter bildirisRouter,
            UserManager<AppUser> userManager,
            IAyliqElaveService ayliqElaveService,
            IMuhasibatHesabService muhasibatHesabService,
            IWebHostEnvironment env)
        {
            _maasService = maasService;
            _hesablamaService = hesablamaService;
            _unitOfWork = unitOfWork;
            _bildirisService = bildirisService;
            _bildirisRouter = bildirisRouter;
            _userManager = userManager;
            _ayliqElaveService = ayliqElaveService;
            _muhasibatHesabService = muhasibatHesabService;
            _env = env;
        }

        // ── Əmək haqqı əməliyyat yazılışı (provodka) — .xls ──────────
        // Seçilmiş ay üzrə hesablanmış maaşlardan tam əməliyyat yazılışı:
        //   A) Hesablanma/xərc — rezident/qeyri-rezident (bank hesabı 41015 → qeyri-rezident)
        //   B) İşəgötürən sosial ayırmalar (MDSS/işsizlik/icbari tibbi)
        //   C) İşçi tutulmaları (MDSS/işsizlik/icbari tibbi/gəlir vergisi)
        //   E) İşçi başına net ödəniş (Kredit = bank hesabı)
        // Sabit hesablar Mühasibat Hesabları (MuhasibatHesabi) ayarından gəlir.
        // QEYD: status filtri yoxdur — ay üzrə bütün hesablanmış (silinməmiş) maaşlar.
        public async Task<IActionResult> PravodkaExport(int il, int ay)
        {
            if (ay < 1 || ay > 12)
            {
                TempData["Error"] = "Ay düzgün deyil.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            // Sabit hesabları lüğətə yüklə (Açar → nömrə)
            var hamiHesab = await _muhasibatHesabService.HamisiAsync();
            var hesabMap = hamiHesab
                .Where(h => h.Aktiv && !h.Silinib && !string.IsNullOrWhiteSpace(h.HesabNomresi))
                .ToDictionary(h => h.Acar, h => h.HesabNomresi!.Trim(), StringComparer.OrdinalIgnoreCase);
            string Hesab(string acar) => hesabMap.TryGetValue(acar, out var v) ? v : "";

            var kliring = Hesab("MaasKliring");
            if (string.IsNullOrWhiteSpace(kliring))
            {
                TempData["Error"] = "Klirinq hesabı (MaasKliring) təyin edilməyib — Mühasibat Hesabları səhifəsindən təyin edin.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            // Ay üzrə hesablanmış maaşlar (detallarla) — Index ilə eyni mənbə
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib && x.Il == il && x.Ay == ay)
                .Include(x => x.Isci).ThenInclude(i => i.Maliye)
                .Include(x => x.Detallar).ThenInclude(d => d.MaasNovu)
                .OrderBy(x => x.Isci.Sira).ThenBy(x => x.Isci.Soyad).ThenBy(x => x.Isci.Ad)
                .ToListAsync();

            if (maaslar.Count == 0)
            {
                TempData["Error"] = $"{il}/{ay:D2} üçün hesablanmış maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            // Köməkçilər
            decimal Detay(Maas m, string ad) => m.Detallar.Where(d => d.MaasNovu?.Ad == ad).Sum(d => d.Mebleg);
            bool QeyriRez(Maas m) => (m.Isci?.Maliye?.BankHesabNo?.Trim() ?? "").StartsWith("41015");
            decimal CemRez(string ad, bool qeyri) => maaslar.Where(m => QeyriRez(m) == qeyri).Sum(m => Detay(m, ad));
            decimal Cem(string ad) => maaslar.Sum(m => Detay(m, ad));

            string[] ayAdlar = { "", "yanvar", "fevral", "mart", "aprel", "may", "iyun",
                                 "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr" };
            string suf = IlSuffiks(il);
            string Q(string s) => $"{il}-{suf} il {ayAdlar[ay]} ayı üzrə {s}";

            // Avansların cəmi — net-dən faktiki tutulan "Avans Kəsintisi" detalı ilə EYNİ mənbə.
            // (DB-dən ayrıca oxunsa, hesablama vaxtı ilə uyğunsuzluq klirinq balansını pozur.)
            var avansCemi = Cem("Avans Kəsintisi");

            // Qabaqcadan ödənilmiş məzuniyyət — maaşın saxlanmış izahından
            // "Mezuniyyet (qabaqcadan ödənildi)" sətrinin (= net ödənilmiş) cəmi.
            // Bilərəkdən izahdan oxunur ki, maaş detalında göstərilən dəyərlə dəqiq uyğun gəlsin.
            decimal mezQabaqcadanCemi = 0m;
            foreach (var m in maaslar)
            {
                if (string.IsNullOrWhiteSpace(m.HesablamaIzahi)) continue;
                try
                {
                    var izahlar = JsonSerializer.Deserialize<List<HesablamaIzahiDto>>(m.HesablamaIzahi);
                    if (izahlar != null)
                        mezQabaqcadanCemi += izahlar
                            .Where(z => z.Addim == "Mezuniyyet (qabaqcadan ödənildi)")
                            .Sum(z => z.Mebleg);
                }
                catch { /* pozuq izah JSON — keç */ }
            }

            // Xəstəlik — şirkətin ödədiyi müavinət (SirketOdenis, brütə əlavə olunan) ay üzrə cəmi
            var xestelikSirketCemi = await _unitOfWork.Repository<XestelikOdenis>()
                .Query()
                .Where(x => !x.Silinib && x.Il == il && x.Ay == ay)
                .SumAsync(x => x.SirketOdenis);

            // Provodka sətirləri: (Debet, Kredit, Məbləğ, Qeyd)
            var setirler = new List<(string Debet, string Kredit, decimal Mebleg, string Qeyd)>
            {
                // A) Hesablanma — xərc (Debet xərc, Kredit klirinq)
                // Maaş xərci = işlənmiş günə düşən (hesablanmış) məbləğ:
                //   "Əsas Əməkhaqqı" (tam baza) − "Davamiyyət Kəsintisi" (qayıb/məzuniyyət/xəstəlik günləri).
                //   Məzuniyyət/xəstəlik haqqı ayrıca sətirlərdə (aşağıda) gəlir.
                (Hesab("MaasXercRezident"),         kliring, CemRez("Əsas Əməkhaqqı", false) - CemRez("Davamiyyət Kəsintisi", false), Q("rezident işçilərə əmək haqqı")),
                (Hesab("MaasXercQeyriRezident"),    kliring, CemRez("Əsas Əməkhaqqı", true)  - CemRez("Davamiyyət Kəsintisi", true),  Q("qeyri-rezident işçilərə əmək haqqı")),
                (Hesab("MukafatXercRezident"),      kliring, CemRez("Bonus/Mükafat", false),      Q("rezident işçilərə mükafat")),
                (Hesab("MukafatXercQeyriRezident"), kliring, CemRez("Bonus/Mükafat", true),       Q("qeyri-rezident işçilərə mükafat")),
                // Əlavə əmək haqqı xərci = Overtime + IH-07 əlavə təminat (hər ikisi ayrı detaldır).
                (Hesab("ElaveXercRezident"),        kliring, CemRez("Overtime", false) + CemRez("IH-07 Əlavə Təminat", false), Q("rezident işçiyə əlavə əmək haqqı xərci")),
                (Hesab("ElaveXercQeyriRezident"),   kliring, CemRez("Overtime", true)  + CemRez("IH-07 Əlavə Təminat", true),  Q("qeyri-rezident işçiyə əlavə əmək haqqı xərci")),
                (Hesab("MezuniyyetXercRezident"),   kliring, CemRez("Məzuniyyət Ödənişi", false), Q("rezident işçilərə məzuniyyət haqqı")),
                (Hesab("MezuniyyetXercQeyriRezident"), kliring, CemRez("Məzuniyyət Ödənişi", true), Q("qeyri-rezident işçilərə məzuniyyət haqqı")),

                // Hesablanma — xəstəlik müavinəti (Debet müavinət xərci, Kredit klirinq)
                (Hesab("MuavinetXerc"), kliring, xestelikSirketCemi, Q("sığortaedən tərəfindən ödənilən müavinət haqqları")),

                // Bağlanma — avansların cəmi (Debet klirinq, Kredit avans hesabı = AvansDebet)
                (kliring, Hesab("AvansDebet"), avansCemi, Q("avansların bağlanılması")),
                // Bağlanma — qabaqcadan ödənilmiş məzuniyyət (Debet klirinq, Kredit prepaid öhdəlik)
                (kliring, Hesab("MezuniyyetQabaqcadanKredit"), mezQabaqcadanCemi, Q("qabaqcadan ödənilmiş məzuniyyət haqqının bağlanılması")),

                // B) İşəgötürən sosial ayırmalar (Debet xərc 90022, Kredit öhdəlik)
                (Hesab("MdssEdenXercRezident"),      Hesab("MdssKredit"),         CemRez("DSMF (İşəgötürən)", false), Q("rezident işçilər üçün sığortaedənin hesabına ödənilən MDSS haqqı")),
                (Hesab("MdssEdenXercQeyriRezident"), Hesab("MdssKredit"),         CemRez("DSMF (İşəgötürən)", true),  Q("qeyri-rezident işçilər üçün sığortaedənin hesabına ödənilən MDSS haqqı")),
                (Hesab("IssizlikEdenXerc"),          Hesab("IssizlikEdenKredit"), Cem("İşsizlik Sığortası (İşəgötürən)"), Q("sığortaedənin hesabına ödənilən işsizlik üzrə sığorta haqqı")),
                (Hesab("TibbiEdenXerc"),             Hesab("TibbiEdenKredit"),    Cem("İTSS (İşəgötürən)"),           Q("sığortaedənin hesabına ödənilən icbari tibbi sığorta haqqı")),

                // C) İşçi tutulmaları (Debet klirinq, Kredit öhdəlik)
                (kliring, Hesab("MdssOlunanKredit"),     Cem("DSMF (İşçi)"),               Q("sığortaolunanların hesabına ödənilən MDSS haqqı")),
                (kliring, Hesab("IssizlikOlunanKredit"), Cem("İşsizlik Sığortası (İşçi)"), Q("sığortaolunanların hesabına ödənilən işsizlik üzrə sığorta haqqı")),
                (kliring, Hesab("TibbiOlunanKredit"),    Cem("İTSS"),                      Q("sığortaolunanların hesabına ödənilən icbari tibbi sığorta haqqı")),
                (kliring, Hesab("GelirVergisiKredit"),   Cem("Gəlir Vergisi"),             Q("ödənilmiş məbləğdən gəlir vergisi")),
            };

            // D) HYS (Həyat Yığım Sığortası) — şirkət üzrə (IsciHYS-dən, ay tarix-aralığı ilə).
            //    Hesablar şirkət adına görə: "HysOhdelik:{Şirkət}" (Kt öhdəlik), "HysEdenXerc:{Şirkət}" (Dt işəgötürən xərc).
            //    İşçidən tutulan (sığortaolunan):  Dt klirinq / Kt öhdəlik       = Σ Mebleg.
            //    İşəgötürən payı (sığortaedən):    Dt işəgötürən-xərc / Kt öhdəlik = Σ(hər sətir × faiz).
            //    Sətir-sətir yuvarlaqlaşma maaş hesablaması ilə eyni nəticə verir.
            var hysAyBas = new DateTime(il, ay, 1);
            var hysAyBit = hysAyBas.AddMonths(1).AddDays(-1);

            var hysParam = await _unitOfWork.Repository<MaasParametri>()
                .Query()
                .Where(x => x.Aktivdir && !x.Silinib
                         && x.Nov == MaasParametrNovu.HysIsegoturenFaizi
                         && x.BaslamaTarixi <= hysAyBit
                         && (x.BitmeTarixi == null || x.BitmeTarixi >= hysAyBas))
                .OrderByDescending(x => x.BaslamaTarixi)
                .FirstOrDefaultAsync();
            decimal hysIsvFaiz = hysParam?.Deyer ?? 15m;

            var hysList = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x => !x.Silinib
                         && x.Sirket != null && x.Sirket != ""
                         && x.BaslamaTarixi <= hysAyBit
                         && (x.BitmeTarixi == null || x.BitmeTarixi >= hysAyBas))
                .ToListAsync();

            var hysSirketsiz = new List<string>();
            foreach (var grp in hysList.GroupBy(x => x.Sirket!.Trim()).OrderBy(g => g.Key))
            {
                var sirket = grp.Key;
                if (string.IsNullOrWhiteSpace(sirket)) continue;
                decimal olunan = grp.Sum(x => x.Mebleg);
                decimal eden   = grp.Sum(x => Math.Round(x.Mebleg * (hysIsvFaiz / 100m), 2));
                var ohdelik  = Hesab($"HysOhdelik:{sirket}");
                var edenXerc = Hesab($"HysEdenXerc:{sirket}");
                if (string.IsNullOrWhiteSpace(ohdelik) || string.IsNullOrWhiteSpace(edenXerc))
                {
                    hysSirketsiz.Add(sirket);
                    continue;
                }
                if (olunan > 0)
                    setirler.Add((kliring, ohdelik, olunan,
                        Q($"{sirket} sığorta şirkətinə ödənilən həyatın yığım sığortası haqqı (sığortaolunan)")));
                if (eden > 0)
                    setirler.Add((edenXerc, ohdelik, eden,
                        Q($"{sirket} sığorta şirkətinə ödənilən həyatın yığım sığortası haqqı (sığortaedən)")));
            }
            if (hysSirketsiz.Count > 0)
            {
                TempData["Error"] = "HYS hesabları təyin olunmayan şirkət(lər): " + string.Join(", ", hysSirketsiz)
                    + ". Mühasibat Hesabları səhifəsindən bu açarları əlavə edin → "
                    + string.Join("; ", hysSirketsiz.Select(s => $"HysOhdelik:{s} + HysEdenXerc:{s}"));
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            // İşçi başına net — cari (bank) hesabı olmayan işçi provodkanı pozar (boş Kredit).
            // Sessiz keçmə yoxdur — bank üçün kritikdir, data düzəldilməlidir.
            var banksiz = maaslar
                .Where(m => string.IsNullOrWhiteSpace(m.Isci?.Maliye?.BankHesabNo))
                .Select(m => $"{m.Isci?.Ad} {m.Isci?.Soyad}".Trim())
                .ToList();
            if (banksiz.Count > 0)
            {
                TempData["Error"] = "Cari (bank) hesabı təyin olunmayan işçilər var — provodka yaradıla bilməz: "
                                    + string.Join(", ", banksiz);
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            // E) İşçi başına net ödəniş (Debet klirinq, Kredit bank hesabı)
            foreach (var m in maaslar)
            {
                var bank = m.Isci!.Maliye!.BankHesabNo!.Trim();
                setirler.Add((kliring, bank, m.NetMebleg, Q("əmək haqqı")));
            }

            // ── Şablonu aç və doldur (avans ilə eyni üsul) ──
            var templatePath = Path.Combine(_env.ContentRootPath, "App_Data", "Muhasibat", "Emek_haqqi_hesablama.xls");
            if (!System.IO.File.Exists(templatePath))
            {
                TempData["Error"] = "Şablon tapılmadı: App_Data/Muhasibat/Emek_haqqi_hesablama.xls";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            HSSFWorkbook wb;
            using (var tfs = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
                wb = new HSSFWorkbook(tfs);

            var sheet = wb.GetSheet("Ipoteka") ?? wb.GetSheetAt(0);

            var ornek = sheet.GetRow(1);
            var colStil = new ICellStyle[10];
            for (int c = 0; c < 10; c++) colStil[c] = ornek?.GetCell(c)?.CellStyle;
            var textStyle = wb.CreateCellStyle(); textStyle.DataFormat = wb.CreateDataFormat().GetFormat("@");
            var pulStyle = wb.CreateCellStyle();  pulStyle.DataFormat  = wb.CreateDataFormat().GetFormat("0.00");

            for (int rr = sheet.LastRowNum; rr >= 1; rr--)
            {
                var rw = sheet.GetRow(rr);
                if (rw != null) sheet.RemoveRow(rw);
            }

            int r = 1;
            foreach (var s in setirler)
            {
                var row = sheet.CreateRow(r);
                var c0 = row.CreateCell(0); c0.SetCellValue(r);               if (colStil[0] != null) c0.CellStyle = colStil[0]; // № (sıra)
                var c3 = row.CreateCell(3); c3.SetCellValue(s.Debet);         c3.CellStyle = colStil[3] ?? textStyle;             // Debet
                var c5 = row.CreateCell(5); c5.SetCellValue(s.Kredit);        c5.CellStyle = colStil[5] ?? textStyle;             // Kredit
                var c6 = row.CreateCell(6); c6.SetCellValue((double)s.Mebleg); c6.CellStyle = colStil[6] ?? pulStyle;            // Məbləğ
                var c9 = row.CreateCell(9); c9.SetCellValue(s.Qeyd);          if (colStil[9] != null) c9.CellStyle = colStil[9];  // Qeyd
                r++;
            }

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                wb.Write(ms, true);
                bytes = ms.ToArray();
            }
            return File(bytes, "application/vnd.ms-excel", $"Emek_haqqi_hesablama_{il}_{ay:D2}.xls");
        }

        private static string IlSuffiks(int il) => (il % 10) switch
        {
            1 or 2 or 5 or 7 or 8 => "ci",
            3 or 4                => "cü",
            9                     => "cu",
            _                     => "cı"   // 0, 6
        };

        // ── Əmək haqqı cədvəli (mühasib formatı) — .xlsx ──────────────
        // "Excel İxrac" düyməsi: seçilmiş ay üzrə hesablanmış maaşları
        // mühasibin istifadə etdiyi cədvəllə EYNİ sütun strukturunda ixrac edir.
        // Mənbə Index/PravodkaExport ilə eynidir (silinməmiş, ay üzrə bütün maaşlar).
        public async Task<IActionResult> ExcelIxrac(int il, int ay)
        {
            if (ay < 1 || ay > 12)
            {
                TempData["Error"] = "Ay düzgün deyil.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => !x.Silinib && x.Il == il && x.Ay == ay)
                .Include(x => x.Isci).ThenInclude(i => i.Maliye)
                .Include(x => x.Isci).ThenInclude(i => i.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Vezife)
                .Include(x => x.Detallar).ThenInclude(d => d.MaasNovu)
                .OrderBy(x => x.Isci.Sira).ThenBy(x => x.Isci.Soyad).ThenBy(x => x.Isci.Ad)
                .ToListAsync();

            if (maaslar.Count == 0)
            {
                TempData["Error"] = $"{il}/{ay:D2} üçün hesablanmış maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            // Detal məbləği — MaasNovu adına görə (calc-dakı DetayEkle adları ilə eyni).
            decimal Detay(Maas m, string ad) => m.Detallar.Where(d => d.MaasNovu?.Ad == ad).Sum(d => d.Mebleg);

            string[] ayAdlar = { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                                 "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };

            // Sütun başlıqları — mühasib cədvəli ilə eyni (32 sütun: A..AF)
            string[] basliqlar = {
                "№", "S.A.A.", "Vəzifəsi",
                "Müqavilə üzrə aylıq əmək haqqı",
                "Hesablanmış aylıq əmək haqqı",
                "18.02.2016-cı il tarixli IH-07 saylı əmrlə əlavə təminat",
                "İstifadə edilməmiş əmək məzuniyyəti günlərinə görə kompessasiya ödənişi",
                "Orta əmək haqqı saxlanılan günlər üçün hesablanmış orta əmək haqqı",
                "Məzuniyyət haqqı", "Mükafat", "Bayram hədiyyəsi",
                "Müsabiqə qalibinə verilən hədiyyə", "Xəstəlik vərəqəsi",
                "Yalnız mdss haqqı hesablanan digər gəlirlər",
                "VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər",
                "VM-nin 98.2.3-cü maddəsinə əsasən vergiyə cəlb olunan gəlirlər",
                "Əlavə əmək haqqı",
                "HYS müqavilələri üzrə 3 il tamam olmamış qayıdan məbləğlər",
                "Cəmi hesablanmış aylıq ödənişlər",
                "Ödənilmiş həyatın yığım sığortası haqqları",
                "İşgötürən tərəfindən ödənilən həyatın yığım sığortası haqqları",
                "Tutulmuş m.d.s.s. haqları (10%)",
                "Tutulmuş işsizlikdən sığorta haqqları (0.5%)",
                "Gəlir vergisi", "Icbari Tibbi Sığorta (2 %)", "Avans",
                "Güvənli Sığorta üzrə çıxılmalar", "Tutulmuşdur", "Ödənilməlidir",
                "İşəgötürən tərəfindən ödənilən MDSS haqqı",
                "İşəgötürən tərəfindən ödənilən İTS haqqı",
                "Cari hesablar"
            };
            // Mühasib cədvəlindəki məntiqi nömrələmə (başlıqların altındakı sətir)
            string[] nomreler = {
                "1","2","3","4","5","6","7","8","9","10","","","11","12","13","14","15",
                "","16","17","","18","18-1","19","","20","21","22","23","","","23"
            };
            int colCount = basliqlar.Length;

            var wb = new XSSFWorkbook();
            var sheet = wb.CreateSheet($"{ayAdlar[ay]} {il}");
            short pulFmt = wb.CreateDataFormat().GetFormat("#,##0.00");

            IFont Font(bool bold, double size) { var f = wb.CreateFont(); f.IsBold = bold; f.FontHeightInPoints = size; return f; }
            void Cerceve(ICellStyle s) { s.BorderTop = BorderStyle.Thin; s.BorderBottom = BorderStyle.Thin; s.BorderLeft = BorderStyle.Thin; s.BorderRight = BorderStyle.Thin; }

            var titleStyle = wb.CreateCellStyle(); titleStyle.SetFont(Font(true, 11)); titleStyle.Alignment = HorizontalAlignment.Center; titleStyle.VerticalAlignment = VerticalAlignment.Center; titleStyle.WrapText = true;
            var headStyle = wb.CreateCellStyle(); headStyle.SetFont(Font(true, 8)); headStyle.Alignment = HorizontalAlignment.Center; headStyle.VerticalAlignment = VerticalAlignment.Center; headStyle.WrapText = true; Cerceve(headStyle); headStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index; headStyle.FillPattern = FillPattern.SolidForeground;
            var numStyle = wb.CreateCellStyle(); numStyle.SetFont(Font(false, 8)); numStyle.Alignment = HorizontalAlignment.Center; Cerceve(numStyle);
            var textStyle = wb.CreateCellStyle(); textStyle.SetFont(Font(false, 9)); textStyle.VerticalAlignment = VerticalAlignment.Center; textStyle.WrapText = true; Cerceve(textStyle);
            var moneyStyle = wb.CreateCellStyle(); moneyStyle.SetFont(Font(false, 9)); moneyStyle.DataFormat = pulFmt; Cerceve(moneyStyle);
            var totalText = wb.CreateCellStyle(); totalText.SetFont(Font(true, 9)); totalText.Alignment = HorizontalAlignment.Right; Cerceve(totalText);
            var totalMoney = wb.CreateCellStyle(); totalMoney.SetFont(Font(true, 9)); totalMoney.DataFormat = pulFmt; Cerceve(totalMoney);

            // Başlıq sətri (merged)
            var titleRow = sheet.CreateRow(0); titleRow.HeightInPoints = 34;
            var tcell = titleRow.CreateCell(0);
            tcell.SetCellValue($"\"Bank Melli İran\" Bakı filialının işçilərinin {il}-{IlSuffiks(il)} ilin {ayAdlar[ay]} ayı üzrə əmək haqqı və ona bərabər tutulan ödənişlər cədvəli");
            tcell.CellStyle = titleStyle;
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, colCount - 1));

            // Sütun adları
            var headRow = sheet.CreateRow(1); headRow.HeightInPoints = 72;
            for (int c = 0; c < colCount; c++) { var cell = headRow.CreateCell(c); cell.SetCellValue(basliqlar[c]); cell.CellStyle = headStyle; }

            // Nömrələmə sətri
            var numRow = sheet.CreateRow(2);
            for (int c = 0; c < colCount; c++) { var cell = numRow.CreateCell(c); cell.SetCellValue(c < nomreler.Length ? nomreler[c] : ""); cell.CellStyle = numStyle; }

            // Data sətirləri
            int r = 3, sira = 0;
            var cemler = new decimal[colCount];
            foreach (var m in maaslar)
            {
                sira++;
                decimal esas = Detay(m, "Əsas Əməkhaqqı");
                decimal davam = Detay(m, "Davamiyyət Kəsintisi");
                var vez = m.Isci.IsciTeyinatlari.FirstOrDefault()?.Vezife?.Ad ?? "—";

                var pul = new decimal[colCount];
                pul[3]  = esas;                                   // Müqavilə üzrə aylıq əmək haqqı
                pul[4]  = esas - davam;                           // Hesablanmış (işlənmiş) əmək haqqı
                pul[5]  = Detay(m, "IH-07 Əlavə Təminat");
                pul[6]  = 0m;                                     // məzuniyyət kompensasiyası — ayrıca maaş detalı yoxdur
                pul[7]  = 0m;                                     // orta əmək haqqı saxlanılan günlər — detal yoxdur
                pul[8]  = Detay(m, "Məzuniyyət Ödənişi");
                pul[9]  = Detay(m, "Bonus/Mükafat");
                pul[10] = 0m;                                     // bayram hədiyyəsi
                pul[11] = 0m;                                     // müsabiqə hədiyyəsi
                pul[12] = Detay(m, "Xəstəlik Ödənişi");
                pul[13] = Detay(m, "Fərqli Gəlir");               // yalnız MDSS hesablanan digər gəlir
                pul[14] = Detay(m, "VM 98.2.1 Gəlirləri");
                pul[15] = 0m;                                     // VM 98.2.3 — detal yoxdur
                pul[16] = Detay(m, "Overtime");                   // əlavə əmək haqqı
                pul[17] = 0m;                                     // HYS qayıdan məbləğlər — detal yoxdur
                pul[18] = m.BrutMebleg;                           // cəmi hesablanmış
                pul[19] = Detay(m, "HYS (İşçi)");
                pul[20] = Detay(m, "HYS (İşəgötürən)");
                pul[21] = Detay(m, "DSMF (İşçi)");
                pul[22] = Detay(m, "İşsizlik Sığortası (İşçi)");
                pul[23] = Detay(m, "Gəlir Vergisi");
                pul[24] = Detay(m, "İTSS");                       // işçi icbari tibbi
                pul[25] = Detay(m, "Avans Kəsintisi");
                pul[26] = 0m;                                     // güvənli sığorta — detal yoxdur
                pul[27] = m.BrutMebleg - m.NetMebleg;             // tutulmuşdur (cəmi)
                pul[28] = m.NetMebleg;                            // ödənilməlidir (net)
                pul[29] = Detay(m, "DSMF (İşəgötürən)");
                pul[30] = Detay(m, "İTSS (İşəgötürən)");

                var row = sheet.CreateRow(r);
                var c0 = row.CreateCell(0); c0.SetCellValue(sira); c0.CellStyle = numStyle;
                var c1 = row.CreateCell(1); c1.SetCellValue($"{m.Isci.Soyad} {m.Isci.Ad} {m.Isci.AtaAdi}".Trim()); c1.CellStyle = textStyle;
                var c2 = row.CreateCell(2); c2.SetCellValue(vez); c2.CellStyle = textStyle;
                for (int c = 3; c <= 30; c++) { var cell = row.CreateCell(c); cell.SetCellValue((double)pul[c]); cell.CellStyle = moneyStyle; cemler[c] += pul[c]; }
                var c31 = row.CreateCell(31); c31.SetCellValue(m.Isci.Maliye?.BankHesabNo ?? ""); c31.CellStyle = textStyle;
                r++;
            }

            // Cəmi sətri
            var totalRow = sheet.CreateRow(r);
            for (int c = 0; c < colCount; c++)
            {
                var cell = totalRow.CreateCell(c);
                if (c == 1) { cell.SetCellValue("CƏMİ"); cell.CellStyle = totalText; }
                else if (c >= 3 && c <= 30) { cell.SetCellValue((double)cemler[c]); cell.CellStyle = totalMoney; }
                else { cell.SetCellValue(""); cell.CellStyle = totalText; }
            }

            // Sütun enləri (1/256 simvol)
            sheet.SetColumnWidth(0, 5 * 256);
            sheet.SetColumnWidth(1, 28 * 256);
            sheet.SetColumnWidth(2, 18 * 256);
            for (int c = 3; c <= 30; c++) sheet.SetColumnWidth(c, 13 * 256);
            sheet.SetColumnWidth(31, 24 * 256);
            sheet.CreateFreezePane(3, 3);

            byte[] bytes;
            using (var ms = new MemoryStream()) { wb.Write(ms, true); bytes = ms.ToArray(); }
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Emek_haqqi_cedveli_{il}_{ay:D2}.xlsx");
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
                .OrderBy(x => x.Isci.Sira).ThenBy(x => x.Isci.Ad).ThenBy(x => x.Isci.Soyad)
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
                    OvertimeMeblegi = GetDetay("Overtime"),
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

            // Qabaqcadan ödənilmiş məzuniyyət avansının vergiləri — maaş siyahısında birləşdirilmiş göstərmək üçün
            var ayBasOdenis = new DateTime(cIl, cAy, 1);
            var ayBitOdenis = ayBasOdenis.AddMonths(1).AddDays(-1);
            var odənilmişAvanslar = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x => !x.Silinib
                    && x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis
                    && x.OdenisStatus == MezuniyyetOdenisStatus.Odenilib
                    && x.BaslamaTarixi >= ayBasOdenis && x.BaslamaTarixi <= ayBitOdenis)
                .ToListAsync();

            foreach (var avans in odənilmişAvanslar)
            {
                var dto = listDto.FirstOrDefault(x => x.IsciId == avans.IsciId);
                if (dto == null) continue;

                // OdenenMeblegBrut mövcud ödənişdə null ola bilər — canlı hesablama ilə əvəzlə
                decimal avansBrut;
                if (avans.OdenenMeblegBrut.HasValue && avans.OdenenMeblegBrut > 0)
                {
                    avansBrut = avans.OdenenMeblegBrut.Value;
                }
                else
                {
                    var hesab = await _hesablamaService.MezuniyyetOdenisiDetalliHesablaAsync(
                        avans.IsciId, avans.BaslamaTarixi, avans.BitmeTarixi);
                    avansBrut = hesab.CemiOdenis;
                }

                var tarix = new DateTime(cIl, cAy, 1);
                var salaryTax  = await _hesablamaService.TutulmalariHesablaAsync(dto.BrutMaas, tarix, avans.IsciId);
                var combinedTax = await _hesablamaService.TutulmalariHesablaAsync(dto.BrutMaas + avansBrut, tarix, avans.IsciId);

                dto.AvansBrut         = avansBrut;
                dto.AvansNet          = avans.OdenenMebleg ?? 0;
                dto.AvansGelirVergisi = combinedTax.GelirVergisi - salaryTax.GelirVergisi;
                dto.AvansDsmfIsci     = combinedTax.DsmfIsci     - salaryTax.DsmfIsci;
                dto.AvansIssizlikIsci = combinedTax.IssizlikIsci - salaryTax.IssizlikIsci;
                dto.AvansItss         = combinedTax.Itss         - salaryTax.Itss;
            }

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
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
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
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
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

        // ── GET /HR/Maas/Context ─────────────────────────────────
        // Fərdi hesabla formunda işçi + il + ay seçiləndə AJAX ilə
        // faktiki vəziyyəti qaytarır — HR dəyişiklik olub-olmadığını
        // görə bilsin.
        [HttpGet]
        public async Task<IActionResult> Context(int isciId, int il, int ay)
        {
            try
            {
                var isci = await _unitOfWork.Repository<Isci>().Query()
                    .Where(x => x.Id == isciId)
                    .Include(x => x.Maliye)
                    .FirstOrDefaultAsync();
                if (isci == null) return Json(new { success = false, message = "İşçi tapılmadı." });

                // Cari əsas maaş
                decimal cariMaas = isci.Maliye?.CariMaas ?? 0;

                // Staj (informativ — yeni xəstəlik düsturu staj faizini istifadə etmir,
                // lakin UI-da göstərmək üçün hesablanır). Bank + əmək kitabçası.
                var staj = FinNex.Application.Services.HR.IsciStajHelper.Hesabla(isci, new DateTime(il, ay, 1));
                int stajIl = staj.Il;
                int stajAy = staj.Ay;
                int stajFaiz = staj.Faiz;

                var ayBaslangic = new DateTime(il, ay, 1);
                var ayBitis = ayBaslangic.AddMonths(1).AddDays(-1);

                // Bu ayda təsdiqlənmiş məzuniyyətlər
                var mez = await _unitOfWork.Repository<Mezuniyyet>().Query()
                    .Where(m => !m.Silinib && m.IsciId == isciId
                             && m.Status == MezuniyyetStatus.Tesdiqlenib
                             && m.BaslamaTarixi <= ayBitis && m.BitmeTarixi >= ayBaslangic)
                    .Select(m => new
                    {
                        m.Id,
                        m.Nov,
                        m.BaslamaTarixi,
                        m.BitmeTarixi,
                        m.IsGunlerininSayi,
                        m.OdenenMebleg
                    })
                    .ToListAsync();

                // Bu ayda xəstəlik bülletənləri + ödənişləri
                var xesOdenisler = await _unitOfWork.Repository<XestelikOdenis>().Query()
                    .Where(o => !o.Silinib && o.IsciId == isciId && o.Il == il && o.Ay == ay)
                    .Include(o => o.Xestelik)
                    .Select(o => new
                    {
                        o.Id,
                        BulletenNo = o.Xestelik.BulletenNomresi,
                        o.SirketGunSayi,
                        o.DsmfGunSayi,
                        o.SirketOdenis,
                        o.DsmfOdenis
                    })
                    .ToListAsync();

                // Bu ayda avans müraciətləri (təsdiqlənib və ya ödənilib)
                var avans = await _unitOfWork.Repository<Avans>().Query()
                    .Where(a => !a.Silinib && a.IsciId == isciId && a.Il == il && a.Ay == ay
                             && (a.Status == AvansStatus.Tesdiqlenib
                              || a.Status == AvansStatus.Odenilib))
                    .Select(a => new { a.Id, a.Mebleg, Status = a.Status.ToString() })
                    .ToListAsync();

                // Bu ay üçün əvvəlki maaş hesablaması varmı?
                var movcudMaas = await _unitOfWork.Repository<Maas>().Query()
                    .Where(m => !m.Silinib && m.IsciId == isciId && m.Il == il && m.Ay == ay)
                    .Select(m => new { m.Id, m.BrutMebleg, m.NetMebleg })
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    cariMaas,
                    staj = new { il = stajIl, ay = stajAy, faiz = stajFaiz },
                    mezuniyyetler = mez.Select(m => new
                    {
                        id = m.Id,
                        nov = m.Nov.ToString(),
                        baslama = m.BaslamaTarixi.ToString("dd.MM.yyyy"),
                        bitme = m.BitmeTarixi.ToString("dd.MM.yyyy"),
                        isGunu = m.IsGunlerininSayi,
                        odenen = m.OdenenMebleg
                    }),
                    xestelikler = xesOdenisler.Select(o => new
                    {
                        bulletenNo = o.BulletenNo,
                        sirketGun = o.SirketGunSayi,
                        dsmfGun = o.DsmfGunSayi,
                        sirketOdenis = o.SirketOdenis,
                        dsmfOdenis = o.DsmfOdenis
                    }),
                    avanslar = avans.Select(a => new { id = a.Id, mebleg = a.Mebleg, status = a.Status }),
                    movcudHesablama = movcudMaas == null ? null : new
                    {
                        id = movcudMaas.Id,
                        brut = movcudMaas.BrutMebleg,
                        net = movcudMaas.NetMebleg
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── GET /HR/Maas/TopluHesabla ────────────────────────────
        [HttpGet]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Muhasib + "," + RoleNames.Rehber + "," + RoleNames.Admin)]
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

            // Aktiv işçilər + həmin ayda işdən çıxmış işçilər (cari ayın əmək haqqı hesablanmalıdır)
            var isciler = await _unitOfWork.Repository<Isci>()
                .Query()
                .Where(x => !x.Silinib && (
                    x.Status == IsciStatus.Aktiv ||
                    (x.Status == IsciStatus.IshtenCixib &&
                     x.IsdenAyrilmaTarixi.HasValue &&
                     x.IsdenAyrilmaTarixi.Value.Year == cIl &&
                     x.IsdenAyrilmaTarixi.Value.Month == cAy)))
                .Include(x => x.Maliye)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Departament)
                .Include(x => x.IsciTeyinatlari.Where(t => t.BitmeTarixi == null))
                    .ThenInclude(t => t.Vezife)
                .OrderBy(x => x.Sira).ThenBy(x => x.Ad).ThenBy(x => x.Soyad)
                .ToListAsync();

            // CariMaas — IsciMaliye-dən birbaşa sorğu (navigation-dan asılı olmayaraq)
            var isciIdler = isciler.Select(x => x.Id).ToList();
            var maliyeler = await _unitOfWork.Repository<IsciMaliye>()
                .Query()
                .Where(x => isciIdler.Contains(x.IsciId) && !x.Silinib)
                .ToListAsync();
            var cariMaasMap = maliyeler.ToDictionary(x => x.IsciId, x => x.CariMaas);
            var ibanMap = maliyeler.ToDictionary(x => x.IsciId, x => x.BankHesabNo);

            // Ləğv edilmiş qeydlər soft-delete olduğu üçün !x.Silinib şərti kifayətdir
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

            // Birinci pillə üst həddi (standart 200 AZN güzəşti yalnız bu sərhədə qədər tətbiq olunur)
            var firstBracketMax = pilleler
                .Where(x => x.Nov == MaasParametrNovu.GelirVergisiFaizi)
                .OrderBy(x => x.AsagiHedd)
                .Select(x => x.YuxariHedd)
                .FirstOrDefault() ?? 2500m;

            // Hər işçi üçün aktiv dövrü olan güzəştlərin ən böyüyü (JS preview üçün).
            // Server-tərəfi hesablama zatən FerdiHesablaAsync-də düzgün tətbiq edir;
            // burada yalnız toplu ekran preview-u üçün lazımdır.
            var ayBitis = hesabTarixi.AddMonths(1).AddDays(-1);
            var isciGuzestler = await _unitOfWork.Repository<IsciGuzest>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    isciIdler.Contains(x.IsciId) &&
                    x.BaslamaTarixi <= ayBitis &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi))
                .Include(x => x.Guzest)
                .Where(x => x.Guzest != null && !x.Guzest.Silinib && x.Guzest.Aktivdir)
                .ToListAsync();

            var isciGuzestMap = isciGuzestler
                .GroupBy(x => x.IsciId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.Guzest.Mebleg).First());

            // HYS (Həyat Yığım Sığortası) — hər işçi üçün aktiv HYS-ı tap
            var isciHysList = await _unitOfWork.Repository<IsciHYS>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    isciIdler.Contains(x.IsciId) &&
                    x.BaslamaTarixi <= ayBitis &&
                    (x.BitmeTarixi == null || x.BitmeTarixi >= hesabTarixi))
                .ToListAsync();

            // İşçi bir neçə şirkətdə HYS aça bilər — hər işçi üçün cəm məbləği
            var isciHysMap = isciHysList
                .GroupBy(x => x.IsciId)
                .ToDictionary(g => g.Key, g => g.Sum(h => h.Mebleg));

            // HYS işəgötürən faizi (parametrdən)
            var hysIsvFaiz = flatParamlar.FirstOrDefault(x => x.Nov == MaasParametrNovu.HysIsegoturenFaizi)?.Deyer ?? 15m;

            // Avans — hər işçi üçün bu aydakı təsdiqlənmiş avans məbləği
            var avanslar = await _unitOfWork.Repository<Avans>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    isciIdler.Contains(x.IsciId) &&
                    x.Il == cIl && x.Ay == cAy &&
                    (x.Status == AvansStatus.Tesdiqlenib || x.Status == AvansStatus.Odenilib))
                .ToListAsync();

            var isciAvansMap = avanslar
                .GroupBy(x => x.IsciId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Mebleg));

            ViewBag.Il = cIl;
            ViewBag.Ay = cAy;

            // Ayın iş günü — preview-də kəsinti hesablaması üçün lazımdır
            int ayIsGunu = await _hesablamaService.AyIsGunSayiniHesablaAsync(cIl, cAy);

            // Məzuniyyət preview — FerdiHesablaAsync ilə eyni məntiq:
            //  - IsGun: həmin aya düşən təsdiqli məzuniyyət iş günləri (hər 2 tipdə)
            //  - Odenis: yalnız AySonuOdenis qeydlər üçün 2026 düsturu ilə
            //  - Kesinti: esasMaas / ayIsGunu × IsGun
            var isciMezuniyyetMap = new Dictionary<int, (int gun, decimal odenis, decimal kesinti)>();
            foreach (var id in isciIdler)
            {
                var (mezIsGun, mezKesinti, mezOdenis) = await _hesablamaService.MezuniyyetPreviewAsync(id, cIl, cAy);
                if (mezIsGun > 0 || mezOdenis > 0 || mezKesinti > 0)
                    isciMezuniyyetMap[id] = (mezIsGun, mezOdenis, mezKesinti);
            }

            // Xəstəlik ödənişləri (XestelikOdenis-dən ay üzrə, şirkət payı gross-a əlavə olunur,
            // xəstəlik günlərinə görə əsas maaşdan kəsinti də tətbiq olunur)
            var xestelikOdenisList = await _unitOfWork.Repository<XestelikOdenis>().Query()
                .Where(o => !o.Silinib && isciIdler.Contains(o.IsciId)
                         && o.Il == cIl && o.Ay == cAy)
                .ToListAsync();
            var isciXestelikMap = xestelikOdenisList
                .GroupBy(o => o.IsciId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        int sirketGun = g.Sum(o => o.SirketGunSayi);
                        int dsmfGun = g.Sum(o => o.DsmfGunSayi);
                        decimal esas = cariMaasMap.TryGetValue(g.Key, out var m) ? m : 0m;
                        decimal kesinti = (ayIsGunu > 0 && (sirketGun + dsmfGun) > 0)
                            ? Math.Round(esas / ayIsGunu * (sirketGun + dsmfGun), 2)
                            : 0m;
                        return (
                            sirketGun: sirketGun,
                            dsmfGun: dsmfGun,
                            sirketOdenis: g.Sum(o => o.SirketOdenis),
                            dsmfOdenis: g.Sum(o => o.DsmfOdenis),
                            kesinti: kesinti
                        );
                    });

            // Qayıb kəsintisi preview — MaasdanKes=true olan qayıb günlərini say
            var qayibQeydler = await _unitOfWork.Repository<Davamiyyet>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    isciIdler.Contains(x.IsciId) &&
                    x.Status == DavamiyyetStatus.Qayib &&
                    x.MaasdanKes &&
                    x.Tarix.Year == cIl && x.Tarix.Month == cAy)
                .ToListAsync();

            var isciQayibMap = qayibQeydler
                .GroupBy(x => x.IsciId)
                .ToDictionary(g => g.Key, g =>
                {
                    int gun = g.Count();
                    decimal esas = cariMaasMap.TryGetValue(g.Key, out var m) ? m : 0m;
                    decimal kesinti = ayIsGunu > 0
                        ? Math.Round(esas / ayIsGunu * gun, 2)
                        : 0m;
                    return (gun, kesinti);
                });

            // Məzuniyyət avansı (QabaqcadanOdenis, ödənilmiş) — vergi dağılımı ilə birlikdə
            var mezuniyyetAvanslar = await _unitOfWork.Repository<Mezuniyyet>()
                .Query()
                .Where(x =>
                    !x.Silinib &&
                    isciIdler.Contains(x.IsciId) &&
                    x.OdenisTipi == MezuniyyetOdenisTipi.QabaqcadanOdenis &&
                    x.OdenisStatus == MezuniyyetOdenisStatus.Odenilib &&
                    x.OdenilmeTarixi.HasValue &&
                    x.OdenilmeTarixi.Value.Year == cIl &&
                    x.OdenilmeTarixi.Value.Month == cAy)
                .ToListAsync();

            var isciMezAvansMap = new Dictionary<int, (decimal brut, decimal gelirV, decimal dsmf, decimal iss, decimal itss, decimal net)>();
            foreach (var mav in mezuniyyetAvanslar)
            {
                decimal avBrut = mav.OdenenMeblegBrut ?? 0m;
                if (avBrut <= 0)
                {
                    try
                    {
                        var dHesab = await _hesablamaService.MezuniyyetOdenisiDetalliHesablaAsync(
                            mav.IsciId, mav.BaslamaTarixi, mav.BitmeTarixi);
                        avBrut = dHesab.CemiOdenis;
                    }
                    catch { avBrut = mav.OdenenMebleg ?? 0m; }
                }
                if (avBrut <= 0) continue;

                // Birləşdirilmiş vergi: (maaş + avans) − (yalnız maaş) = avansın vergi payı.
                // Ayrıca hesablamaq 200 AZN standart güzəştini səhv tətbiq edir.
                decimal cM = cariMaasMap.TryGetValue(mav.IsciId, out var cm_) ? cm_ : 0m;
                int mezIsGun = isciMezuniyyetMap.TryGetValue(mav.IsciId, out var mi_) ? mi_.gun : 0;
                int workedDays = Math.Max(0, ayIsGunu - mezIsGun);
                decimal im = (cM > 0 && ayIsGunu > 0) ? Math.Round(cM / ayIsGunu * workedDays, 2) : 0m;

                var ftax = await _hesablamaService.TutulmalariHesablaAsync(im + avBrut, hesabTarixi, mav.IsciId);
                var itax = await _hesablamaService.TutulmalariHesablaAsync(im,          hesabTarixi, mav.IsciId);

                decimal avGelirV = ftax.GelirVergisi  - itax.GelirVergisi;
                decimal avDsmf   = ftax.DsmfIsci       - itax.DsmfIsci;
                decimal avIss    = ftax.IssizlikIsci   - itax.IssizlikIsci;
                decimal avItss   = ftax.Itss           - itax.Itss;
                // Faktiki ödənilmiş NET (saxlanılmışsa), yoxsa hesablanmış
                decimal avNet    = (mav.OdenenMebleg > 0)
                    ? mav.OdenenMebleg.Value
                    : avBrut - (ftax.UmumiTutulma - itax.UmumiTutulma);

                if (isciMezAvansMap.TryGetValue(mav.IsciId, out var ex))
                    isciMezAvansMap[mav.IsciId] = (ex.brut + avBrut,
                        ex.gelirV + avGelirV, ex.dsmf + avDsmf, ex.iss + avIss,
                        ex.itss + avItss, ex.net + avNet);
                else
                    isciMezAvansMap[mav.IsciId] = (avBrut, avGelirV, avDsmf, avIss, avItss, avNet);
            }
            ViewBag.IsciMezAvansMap = isciMezAvansMap;

            // Əvvəlki ay korreksiyası preview — mühasib öncədən görsün deyə
            // (post-maaş daxil edilmiş xəstəlik/məzuniyyət üçün cari aya tətbiq olunacaq düzəliş)
            var isciKorreksiyaMap = new Dictionary<int, (decimal kesinti, decimal gelir, string? aciq)>();
            foreach (var id in isciIdler)
            {
                if (hesablanmis.Contains(id)) continue; // artıq hesablanıb — preview lazım deyil
                var (k, g, a) = await _hesablamaService.EvvelkiAyKorreksiyasiPreviewAsync(id, cIl, cAy);
                if (k > 0 || g > 0) isciKorreksiyaMap[id] = (k, g, a);
            }
            ViewBag.IsciKorreksiyaMap = isciKorreksiyaMap;

            // Aylıq əlavə qeydləri (Bonus + Overtime) — maaş hesablama bunlardan oxuyur.
            // Mühasib təsdiqdən əvvəl səhifədə görsün deyə dictionary kimi göndəririk.
            // Servis vasitəsilə — controller bilavasitə Repository istifadə etmir.
            var (bonusMap, overtimeMap) = await _ayliqElaveService.GetAyMapAsync(cIl, cAy);
            ViewBag.AyliqElaveBonusMap    = bonusMap;
            ViewBag.AyliqElaveOvertimeMap = overtimeMap;

            ViewBag.Hesablanmis = hesablanmis;
            ViewBag.CariMaasMap = cariMaasMap;
            ViewBag.IbanMap = ibanMap;
            ViewBag.VergiPilleleri = pilleler;
            ViewBag.VergiGuzesti = vergiGuzesti;
            ViewBag.FirstBracketMax = firstBracketMax;
            ViewBag.IsciGuzestMap = isciGuzestMap;
            ViewBag.IsciHysMap = isciHysMap;
            ViewBag.HysIsvFaiz = hysIsvFaiz;
            ViewBag.IsciAvansMap = isciAvansMap;
            ViewBag.IsciMezuniyyetMap = isciMezuniyyetMap;
            ViewBag.IsciXestelikMap = isciXestelikMap;
            ViewBag.IsciQayibMap = isciQayibMap;
            ViewBag.Iller = IlSiyahisi(cIl);
            ViewBag.Aylar = AySiyahisi(cAy);

            // Konfiqurasiyalı manual gəlir növləri — toplu hesablamada hər işçi üçün dinamik textbox
            ViewBag.ManualGelirNovleri = await _unitOfWork.Repository<MaasNovu>()
                .Query()
                .Where(x => x.Aktivdir && !x.Silinib && x.ManualGelir)
                .OrderBy(x => x.Ad)
                .ToListAsync();

            ViewData["Title"] = "Toplu Maaş Hesablaması";
            return View(isciler);
        }

        // ── POST /HR/Maas/TopluHesablaEt ─────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Muhasib + "," + RoleNames.Rehber + "," + RoleNames.Admin)]
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
                    x.BonusMeblegi > 0 || x.CerimeMeblegi > 0 || x.IH07Meblegi > 0 || x.VM9821Meblegi > 0
                    || (x.ElaveGelirler != null && x.ElaveGelirler.Any(e => e.Mebleg > 0))).ToList()
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

            var ayAdlar = new[] { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                                  "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };
            var dovr = $"{ayAdlar[ay]} {il}";
            var isciRedirect = Url.Action("Index", "Maas", new { area = "User" });

            int ugurlu = 0, xeta = 0;
            foreach (var m in maaslar)
            {
                var r = await _maasService.StatusDeyisAsync(m.Id, MaasStatus.Odenildi);
                if (r.Success)
                {
                    ugurlu++;
                    // İşçiyə ödəniş bildirişi
                    try
                    {
                        await _bildirisService.YaratAsync(
                            isciId: m.IsciId,
                            nov: BildirisNovu.MaasOdenildi,
                            bashliq: $"Əmək haqqı ödənildi — {dovr}",
                            metn: $"{dovr} üçün əmək haqqınız ({m.NetMebleg:N2} ₼) ödənildi.",
                            redirectUrl: isciRedirect);
                    }
                    catch { /* bildiriş əsas əməliyyatı pozmasın */ }
                }
                else xeta++;
            }

            TempData[xeta > 0 ? "Error" : "Success"] =
                $"Toplu ödəniş: {ugurlu} maaş 'Ödənildi' işarələndi" + (xeta > 0 ? $", {xeta} xətalı." : ".");
            return RedirectToAction(nameof(Index), new { il, ay });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
        public async Task<IActionResult> TopluLegvEt(int il, int ay)
        {
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.Il == il && x.Ay == ay && !x.Silinib && x.Status == MaasStatus.Layihe)
                .ToListAsync();

            if (!maaslar.Any())
            {
                TempData["Error"] = "Ləğv ediləcək layihə statusunda maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            int ugurlu = 0, xeta = 0;
            foreach (var m in maaslar)
            {
                var r = await _maasService.StatusDeyisAsync(m.Id, MaasStatus.LegvEdildi);
                if (r.Success) ugurlu++;
                else xeta++;
            }

            TempData[xeta > 0 ? "Error" : "Success"] =
                $"Toplu ləğv: {ugurlu} maaş ləğv edildi" + (xeta > 0 ? $", {xeta} xətalı." : ".");
            return RedirectToAction(nameof(Index), new { il, ay });
        }

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

        // ── POST /HR/Maas/TopluGeriGonder ───────────────────────
        // Rəhbər hesablamanı geri qaytarır: Layihə → LəğvEdildi + Mühasibə bildiriş
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Rehber + "," + RoleNames.Admin)]
        public async Task<IActionResult> TopluGeriGonder(int il, int ay, string? sebeb)
        {
            var maaslar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.Il == il && x.Ay == ay && !x.Silinib && x.Status == MaasStatus.Layihe)
                .ToListAsync();

            if (!maaslar.Any())
            {
                TempData["Error"] = "Geri göndəriləcək layihə statusunda maaş tapılmadı.";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            int ugurlu = 0, xeta = 0;
            foreach (var m in maaslar)
            {
                var r = await _maasService.StatusDeyisAsync(m.Id, MaasStatus.LegvEdildi);
                if (r.Success) ugurlu++;
                else xeta++;
            }

            if (ugurlu > 0)
                await BildirisGonderHesablamaUcunAsync(il, ay, ugurlu, sebeb);

            TempData[xeta > 0 ? "Error" : "Success"] =
                $"Hesablama geri qaytarıldı: {ugurlu} maaş ləğv edildi, mühasib yenidən hesablamağa dəvət edildi"
                + (xeta > 0 ? $", {xeta} xətalı." : ".");
            return RedirectToAction(nameof(Index), new { il, ay });
        }

        // ── HELPER: Mühasib/HR-ə yenidən hesablama bildirişi göndər ──
        private async Task BildirisGonderHesablamaUcunAsync(int il, int ay, int sayi, string? sebeb)
        {
            try
            {
                var ayAdlar = new[] { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                                      "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };
                var dovr = $"{ayAdlar[ay]} {il}";
                var redirectUrl = Url.Action("TopluHesabla", "Maas", new { area = "HR", il, ay });

                var muhasibler = await _userManager.GetUsersInRoleAsync(RoleNames.Muhasib);
                var hrler      = await _userManager.GetUsersInRoleAsync(RoleNames.HR);
                var adminler   = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                var alicilar   = muhasibler.Concat(hrler).Concat(adminler)
                    .Where(u => u.IsciId.HasValue)
                    .GroupBy(u => u.IsciId!.Value)
                    .Select(g => g.First())
                    .ToList();

                var metn = string.IsNullOrWhiteSpace(sebeb)
                    ? $"{sayi} işçi üçün {dovr} maaşı yenidən hesablanması tələb edildi."
                    : $"{sayi} işçi üçün {dovr} maaşı yenidən hesablanması tələb edildi. Səbəb: {sebeb}";

                foreach (var u in alicilar)
                {
                    await _bildirisService.YaratAsync(
                        isciId: u.IsciId!.Value,
                        nov: BildirisNovu.TesdiqSorgusu,
                        bashliq: $"Maaş hesablaması geri qaytarıldı — {dovr}",
                        metn: metn,
                        redirectUrl: redirectUrl);
                }
            }
            catch { }
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

            // Hesablama addımları (JSON kimi saxlanır) — mühasib üçün audit izahatı
            if (!string.IsNullOrWhiteSpace(maas.HesablamaIzahi))
            {
                try
                {
                    dto.Izahatlar = JsonSerializer.Deserialize<List<HesablamaIzahiDto>>(maas.HesablamaIzahi)
                                    ?? new List<HesablamaIzahiDto>();
                }
                catch { /* köhnə yazılarda JSON korlansa boş göstərir */ }
            }

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
                // Admin həmişə ləğv edə bilər.
                // HR yalnız Layihe statusundakı maaşı ləğv edə bilər
                // (hesablamada səhv olduqda rəhbərliyə sorğu getməzdən geri ala bilsin).
                if (!isAdmin && !isHR)
                {
                    TempData["Error"] = "Maaşı yalnız Admin və ya HR ləğv edə bilər.";
                    return RedirectToAction(nameof(Index), new { il, ay });
                }
                if (isHR && !isAdmin)
                {
                    var movcut = await _unitOfWork.Repository<Maas>()
                        .Query()
                        .FirstOrDefaultAsync(x => x.Id == id && !x.Silinib);
                    if (movcut == null || movcut.Status != MaasStatus.Layihe)
                    {
                        TempData["Error"] = "HR yalnız 'Layihə' statusundakı maaşı ləğv edə bilər.";
                        return RedirectToAction(nameof(Index), new { il, ay });
                    }
                }
            }

            var r = await _maasService.StatusDeyisAsync(id, yeniStatus);

            // Fərdi status dəyişmə — aidiyyəti rollara/işçiyə bildiriş
            if (r.Success)
            {
                await NotifyForStatusChangeAsync(id, yeniStatus, il, ay);
            }

            TempData[r.Success ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Index), new { il, ay });
        }

        private async Task NotifyForStatusChangeAsync(int maasId, MaasStatus yeniStatus, int il, int ay)
        {
            try
            {
                var maas = await _unitOfWork.Repository<Maas>()
                    .GetirAsync(x => x.Id == maasId, izlemeden: true);
                if (maas == null) return;

                var ayAdlar = new[] { "", "Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun",
                                      "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr" };
                var dovr = $"{ayAdlar[ay]} {il}";

                switch (yeniStatus)
                {
                    case MaasStatus.Tesdiqlendi:
                        await _bildirisRouter.NotifyRolesAsync(
                            new[] { RoleNames.Muhasib, RoleNames.Admin },
                            BildirisNovu.TesdiqSorgusu,
                            $"Maaş təsdiqləndi — {dovr}",
                            $"{dovr} dövrü üçün maaş təsdiqləndi, ödəniş gözləyir.",
                            redirectUrl: Url.Action("Index", "Maas", new { area = "HR", il, ay }));
                        break;

                    case MaasStatus.Odenildi:
                        await _bildirisService.YaratAsync(
                            isciId: maas.IsciId,
                            nov: BildirisNovu.MaasOdenildi,
                            bashliq: $"Əmək haqqı ödənildi — {dovr}",
                            metn: $"{dovr} üçün əmək haqqınız ({maas.NetMebleg:N2} ₼) ödənildi.",
                            redirectUrl: Url.Action("Index", "Maas", new { area = "User" }));
                        break;

                    case MaasStatus.LegvEdildi:
                        await _bildirisRouter.NotifyIsciAsync(
                            maas.IsciId,
                            BildirisNovu.MaasReddedildi,
                            $"Maaş ləğv edildi — {dovr}",
                            $"{dovr} dövrü üçün maaş qeydi ləğv edildi. Ətraflı məlumat üçün HR ilə əlaqə saxlayın.",
                            redirectUrl: Url.Action("Index", "Maas", new { area = "User" }));
                        break;
                }
            }
            catch { /* bildiriş xətası əsas əməliyyatı pozmasın */ }
        }

        // ── POST /HR/Maas/Sil ────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
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

        // ── POST /HR/Maas/YenidenHesabla ─────────────────────────
        // Layihə statuslu maaşı silib yenidən hesablayır. Məsələn xəstəlik/
        // məzuniyyət qeydiyyatında səhv düzəldilibsə mühasib rəqəmləri təzələyə bilir.
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.HR + "," + RoleNames.Admin + "," + RoleNames.Muhasib)]
        public async Task<IActionResult> YenidenHesabla(int id)
        {
            var maas = await _unitOfWork.Repository<Maas>().IdIleGetirAsync(id);
            if (maas == null || maas.Silinib)
            {
                TempData["Error"] = "Maaş tapılmadı.";
                return RedirectToAction(nameof(Index));
            }
            if (maas.Status != MaasStatus.Layihe)
            {
                TempData["Error"] = "Yalnız 'Layihə' statuslu maaş yenidən hesablana bilər. Təsdiqlənmiş və ya ödənilmiş maaşı dəyişmək üçün əvvəlcə statusu geri qaytarın.";
                return RedirectToAction(nameof(Detal), new { id });
            }

            int isciId = maas.IsciId;
            int il = maas.Il;
            int ay = maas.Ay;

            // Əvvəlki bonus/cərimə dəyərlərini saxla ki, yenidən hesablamada itməsin
            var maasWithDetallar = await _unitOfWork.Repository<Maas>()
                .Query()
                .Where(x => x.Id == id)
                .Include(x => x.Detallar).ThenInclude(d => d.MaasNovu)
                .FirstOrDefaultAsync();
            var bonusDetay = maasWithDetallar?.Detallar
                .FirstOrDefault(d => d.MaasNovu != null && d.MaasNovu.Ad == "Bonus/Mükafat");
            var cerimeDetay = maasWithDetallar?.Detallar
                .FirstOrDefault(d => d.MaasNovu != null && d.MaasNovu.Ad == "Gecikdirmə Cəriməsi");
            decimal bonusMebleg = bonusDetay?.Mebleg ?? 0;
            decimal cerimeMebleg = cerimeDetay?.Mebleg ?? 0;
            string? bonusAciq = bonusDetay?.Aciqlama;
            string? cerimeAciq = cerimeDetay?.Aciqlama;

            var silR = await _maasService.SilAsync(id);
            if (!silR.Success)
            {
                TempData["Error"] = $"Köhnə hesablama silinmədi: {silR.Message}";
                return RedirectToAction(nameof(Detal), new { id });
            }

            var r = await _hesablamaService.FerdiHesablaAsync(new FerdiHesablaInputDto
            {
                IsciId = isciId,
                Il = il,
                Ay = ay,
                BonusMeblegi = bonusMebleg,
                BonusAciqlama = bonusAciq,
                CerimeMeblegi = cerimeMebleg,
                CerimeAciqlama = cerimeAciq
            });

            if (!r.Success || r.Data == null)
            {
                TempData["Error"] = $"Yenidən hesablama alınmadı: {r.Message}";
                return RedirectToAction(nameof(Index), new { il, ay });
            }

            TempData["Success"] = $"Yenidən hesablandı — NET: {r.Data.NetMaas:N2} ₼";
            return RedirectToAction(nameof(Detal), new { id = r.Data.MaasId });
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
                .OrderBy(x => x.Isci.Sira).ThenBy(x => x.Isci.Ad).ThenBy(x => x.Isci.Soyad)
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
                "GROSS", "Gəlir Vergisi", "DSMF (İşçi)", "İşsizlik (İşçi)", "İTSS (İşçi)",
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
                // GROSS kolonu da vurğula
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
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FinNex.Application.Services.HR;
using FinNex.Domain.Entities.HR;
using FinNex.Domain.Interfaces;
using FinNex.UI.Services.Kredit;
using Microsoft.EntityFrameworkCore;

namespace FinNex.UI.Services.HR;

/// <summary>
/// Məzuniyyət əmrlərinin (K/M) Word generasiyası — bankın rəsmi şablonlarından.
/// Şablonlar: wwwroot/Files/Word/Emr/*.docx ({e_...} tokenli, orijinal formatda).
/// Şablon seçimi avtomatikdir: öz hesabına / 1 gün / çoxgünlük / iki iş ili / kompensasiya.
/// HR şablon faylını Word-də redaktə edə bilər — kod dəyişmir (tokenlər qalmaq şərtilə).
/// </summary>
public class MezuniyyetEmrWordService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmrService _emrService;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public MezuniyyetEmrWordService(IUnitOfWork uow, IEmrService emrService,
        IConfiguration config, IWebHostEnvironment env)
    {
        _uow = uow; _emrService = emrService; _config = config; _env = env;
    }

    private string SablonYolu(string ad) =>
        Path.Combine(_env.WebRootPath, "Files", "Word", "Emr", ad);

    private string MudirAd => _config["Emr:MudirAd"] ?? "A.K.Najafimarganmaskan";

    // ── MƏZUNİYYƏT ƏMRİ ─────────────────────────────────────────────
    public async Task<(byte[]? Bytes, string? FaylAdi, string? Xeta)> MezuniyyetEmriYaratAsync(int mezuniyyetId)
    {
        var mez = await _uow.Repository<Mezuniyyet>().Query().AsNoTracking()
            .Include(x => x.Isci).ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                .ThenInclude(t => t.Vezife).ThenInclude(v => v.Departament)
            .Include(x => x.EvezEdenIsci).ThenInclude(e => e!.IsciTeyinatlari.Where(t => !t.Silinib))
                .ThenInclude(t => t.Vezife).ThenInclude(v => v.Departament)
            .FirstOrDefaultAsync(x => x.Id == mezuniyyetId && !x.Silinib);

        if (mez == null) return (null, null, "Məzuniyyət tapılmadı.");
        if (mez.Status != MezuniyyetStatus.Tesdiqlenib)
            return (null, null, "Əmr yalnız təsdiqlənmiş məzuniyyət üçün çap oluna bilər.");
        if (!mez.EmrRegem.HasValue)
            return (null, null, "Bu məzuniyyətə K/M əmr nömrəsi verilməyib (HR təsdiqi tam getməyib).");
        if (mez.Nov != MezuniyyetNovu.Illik && mez.Nov != MezuniyyetNovu.OzHesabina)
            return (null, null, "Əmr yalnız illik və ya öz hesabına məzuniyyət üçün nəzərdə tutulub.");

        var isci = mez.Isci;
        var teyinat = isci.IsciTeyinatlari.FirstOrDefault(t => t.Esasdir) ?? isci.IsciTeyinatlari.FirstOrDefault();

        var tokenler = new Dictionary<string, string?>
        {
            ["{e_nomre}"] = $"{mez.EmrRegem}{mez.EmrSuffiks}",
            ["{e_tarix}"] = TarixSozle(mez.HrTesdiqTarixi ?? DateTime.Today),
            ["{e_isci_yonluk}"] = IsciYonluk(isci),
            ["{e_vezife_yonluk}"] = VezifeYonluk(teyinat),
            ["{e_isci_yiyelik}"] = IsciYiyelik(isci),
            ["{e_mudir}"] = MudirAd,
            ["{e_ise_baslama}"] = TarixIlSuffiksli(await NovbetiIsGunuAsync(mez.BitmeTarixi)),
        };

        string sablon;
        if (mez.Nov == MezuniyyetNovu.OzHesabina)
        {
            sablon = "Mezuniyyet_oz_hesabina.docx";
            tokenler["{e_bas_tarix}"] = TarixIlSuffiksli(mez.BaslamaTarixi);
            tokenler["{e_gun1}"] = ((mez.BitmeTarixi.Date - mez.BaslamaTarixi.Date).Days + 1).ToString();
        }
        else
        {
            // İllik — iş ili bölgüsü (FIFO: ən köhnə balans ili əvvəl istifadə olunur)
            var bolgu = await IsIliBolgusuAsync(mez);
            int gunSayi = mez.IsGunlerininSayiManual ?? mez.IsGunlerininSayi;

            if (bolgu.Count >= 2)
            {
                sablon = "Mezuniyyet_iki_is_ili.docx";
                tokenler["{e_isili1}"] = IsIliAraligi(isci, bolgu[0].Il);
                tokenler["{e_gun1}"] = bolgu[0].Gun.ToString();
                tokenler["{e_isili2}"] = IsIliAraligi(isci, bolgu[1].Il);
                // 3-cü il nadir haldır — qalıq ikinci payda cəmlənir
                tokenler["{e_gun2}"] = bolgu.Skip(1).Sum(x => x.Gun).ToString();
            }
            else if (gunSayi == 1)
            {
                sablon = "Mezuniyyet_1_gun.docx";
                tokenler["{e_isili1}"] = IsIliAraligi(isci, bolgu.Count > 0 ? bolgu[0].Il : mez.BaslamaTarixi.Year);
            }
            else
            {
                sablon = "Mezuniyyet_coxgunluk.docx";
                tokenler["{e_isili1}"] = IsIliAraligi(isci, bolgu.Count > 0 ? bolgu[0].Il : mez.BaslamaTarixi.Year);
                tokenler["{e_gun1}"] = gunSayi.ToString();
            }

            tokenler["{e_dovr}"] = gunSayi == 1
                ? $"{TarixIlSuffiksli(mez.BaslamaTarixi)} il tarixində"
                : $"{TarixIlSuffiksli(mez.BaslamaTarixi)} ildən {TarixIlSuffiksli(mez.BitmeTarixi)} ilə qədər";
            tokenler["{e_dovr2}"] = gunSayi == 1
                ? $"{TarixIlSuffiksli(mez.BaslamaTarixi)} il tarixində"
                : $"{TarixIlSuffiksli(mez.BaslamaTarixi)} il – {TarixIlSuffiksli(mez.BitmeTarixi)} il tarixində";
            tokenler["{e_vezife_adi}"] = teyinat?.Vezife?.Ad ?? "—";

            // Əvəzedici (§2) — seçilibsə doldurulur, seçilməyibsə §2 tam silinir
            if (mez.EvezEdenIsci != null)
            {
                var evTey = mez.EvezEdenIsci.IsciTeyinatlari.FirstOrDefault(t => t.Esasdir)
                            ?? mez.EvezEdenIsci.IsciTeyinatlari.FirstOrDefault();
                var evDeptAd = evTey?.Vezife?.Departament?.Ad;
                var evDept = (evDeptAd != null &&
                              evDeptAd.Trim().Equals("Rəhbərlik", StringComparison.OrdinalIgnoreCase))
                    ? "" : DepartamentYiyelik(evDeptAd);
                // Əvəzedicinin vəzifəsi MƏNSUBİYYƏT formasında: "departamentinin rəisi"
                var evVez = VezifeMensubiyyet(evTey?.Vezife?.Ad);
                tokenler["{e_evezedici}"] =
                    $"{(string.IsNullOrEmpty(evDept) ? "" : evDept + " ")}{evVez} {Inisiallar(mez.EvezEdenIsci)} {SoyadYonluk(mez.EvezEdenIsci.Soyad)}".Trim();
            }
        }

        var yol = SablonYolu(sablon);
        if (!File.Exists(yol)) return (null, null, $"Şablon tapılmadı: Files/Word/Emr/{sablon}");

        var bytes = KreditWordService.Doldur(yol, tokenler);

        // Əvəzedici yoxdursa §2 bölməsini sənəddən çıxar
        if (mez.Nov == MezuniyyetNovu.Illik && mez.EvezEdenIsci == null)
            bytes = Paragraf2Sil(bytes);

        var faylAdi = $"KM_{mez.EmrRegem}{mez.EmrSuffiks}_{Translit(isci.Soyad)}_{mez.BaslamaTarixi:yyyyMMdd}.docx";
        return (bytes, faylAdi, null);
    }

    // ── KOMPENSASİYA ƏMRİ ───────────────────────────────────────────
    // Əmr nömrəsi EYNİ K/M sayğacından götürülür (EmrNovu.Mezuniyyet) və Emr
    // reyestrində saxlanır — təkrar çapda eyni nömrə qalır.
    public async Task<(byte[]? Bytes, string? FaylAdi, string? Xeta)> KompensasiyaEmriYaratAsync(int kompId)
    {
        var komp = await _uow.Repository<MezuniyyetKompensasiyasi>().Query().AsNoTracking()
            .Include(x => x.Isci).ThenInclude(i => i.IsciTeyinatlari.Where(t => !t.Silinib))
                .ThenInclude(t => t.Vezife).ThenInclude(v => v.Departament)
            .FirstOrDefaultAsync(x => x.Id == kompId && !x.Silinib);
        if (komp == null) return (null, null, "Kompensasiya qeydi tapılmadı.");

        // Mövcud əmr nömrəsi (reyestrdən) və ya yenisi
        var movcudEmr = await _uow.Repository<Emr>().Query().AsNoTracking()
            .FirstOrDefaultAsync(x => !x.Silinib && x.Nov == EmrNovu.Mezuniyyet
                                   && x.ElaqeliEntityId == komp.Id
                                   && x.Metn == "Kompensasiya");
        int nomre; string? suffiks = null;
        if (movcudEmr != null) { nomre = movcudEmr.Nomre; suffiks = movcudEmr.Suffiks; }
        else
        {
            var emr = await _emrService.YeniEmrAsync(EmrNovu.Mezuniyyet,
                elaqeliEntityId: komp.Id, metn: "Kompensasiya");
            nomre = emr.Nomre; suffiks = emr.Suffiks;
        }

        var isci = komp.Isci;
        var teyinat = isci.IsciTeyinatlari.FirstOrDefault(t => t.Esasdir) ?? isci.IsciTeyinatlari.FirstOrDefault();

        // İş ili — ayrılma tarixindən əvvəlki son tam iş ili (yubiley əsaslı).
        // Fərqli hal lazımdırsa HR yüklənən Word-də düzəldə bilər.
        var sonIlBas = IsIliBaslangici(isci.IsheQebulTarixi, komp.AyrilmaTarixi).AddYears(-1);

        var tokenler = new Dictionary<string, string?>
        {
            ["{e_nomre}"] = $"{nomre}{suffiks}",
            ["{e_tarix}"] = TarixSozle(DateTime.Today),
            ["{e_isci_yonluk}"] = IsciYonluk(isci),
            ["{e_vezife_yonluk}"] = VezifeYonluk(teyinat),
            ["{e_isci_yiyelik}"] = IsciYiyelik(isci),
            ["{e_mudir}"] = MudirAd,
            ["{e_isili1}"] = IsIliAraligi(isci, sonIlBas.Year),
            ["{e_gun1}"] = Math.Round(komp.CemiKompensasiyaGun, 0).ToString("0"),
        };

        var yol = SablonYolu("Mezuniyyet_kompensasiya.docx");
        if (!File.Exists(yol)) return (null, null, "Şablon tapılmadı: Files/Word/Emr/Mezuniyyet_kompensasiya.docx");

        var bytes = KreditWordService.Doldur(yol, tokenler);
        var faylAdi = $"KM_{nomre}{suffiks}_Kompensasiya_{Translit(isci.Soyad)}.docx";
        return (bytes, faylAdi, null);
    }

    // ── İş ili bölgüsü — FIFO replay ────────────────────────────────
    // Təsdiq zamanı balans FIFO kəsilir (ən köhnə il əvvəl), amma bölgü ayrıca
    // saxlanmır. Burada eyni FIFO xronoloji təkrarlanır və BU məzuniyyətin payı tapılır.
    private async Task<List<(int Il, int Gun)>> IsIliBolgusuAsync(Mezuniyyet mez)
    {
        var balanslar = await _uow.Repository<MezuniyyetBalans>().Query().AsNoTracking()
            .Where(b => !b.Silinib && b.IsciId == mez.IsciId && b.Nov == MezuniyyetNovu.Illik)
            .OrderBy(b => b.Il)
            .Select(b => new { b.Il, b.ToplamGun })
            .ToListAsync();
        if (balanslar.Count == 0) return new();

        var mezler = await _uow.Repository<Mezuniyyet>().Query().AsNoTracking()
            .Where(x => !x.Silinib && x.IsciId == mez.IsciId
                     && x.Nov == MezuniyyetNovu.Illik
                     && x.Status == MezuniyyetStatus.Tesdiqlenib)
            .Select(x => new { x.Id, x.BaslamaTarixi, x.HrTesdiqTarixi,
                               Gun = x.IsGunlerininSayiManual ?? x.IsGunlerininSayi })
            .ToListAsync();

        var qaliq = balanslar.ToDictionary(b => b.Il, b => b.ToplamGun);
        var neticeler = new List<(int Il, int Gun)>();
        foreach (var m in mezler.OrderBy(x => x.HrTesdiqTarixi ?? x.BaslamaTarixi).ThenBy(x => x.Id))
        {
            int lazim = m.Gun;
            var payList = new List<(int Il, int Gun)>();
            foreach (var il in qaliq.Keys.OrderBy(k => k))
            {
                if (lazim <= 0) break;
                if (qaliq[il] <= 0) continue;
                int goturulen = Math.Min(qaliq[il], lazim);
                qaliq[il] -= goturulen; lazim -= goturulen;
                payList.Add((il, goturulen));
            }
            if (m.Id == mez.Id) { neticeler = payList; break; }
        }
        return neticeler;
    }

    // İş ili başlanğıcı: verilən tarixdən əvvəlki (yaxud bərabər) ən son yubiley
    private static DateTime IsIliBaslangici(DateTime iseQebul, DateTime tarix)
    {
        var d = new DateTime(tarix.Year, iseQebul.Month, Math.Min(iseQebul.Day, DateTime.DaysInMonth(tarix.Year, iseQebul.Month)));
        return d <= tarix ? d : d.AddYears(-1);
    }

    // "23.05.2016 – 23.05.2017-ci" — balans ili yubiley aralığına çevrilir
    private string IsIliAraligi(Isci isci, int il)
    {
        var q = isci.IsheQebulTarixi;
        var bas = new DateTime(il, q.Month, Math.Min(q.Day, DateTime.DaysInMonth(il, q.Month)));
        var bit = bas.AddYears(1);
        return $"{bas:dd.MM.yyyy} – {bit:dd.MM.yyyy}{IlSuffiks(bit.Year)}";
    }

    // ── Növbəti iş günü (həftəsonu + bayram atlanır) ────────────────
    private async Task<DateTime> NovbetiIsGunuAsync(DateTime bitme)
    {
        var bas = bitme.Date.AddDays(1);
        var son = bas.AddDays(30);
        var xususiler = await _uow.Repository<BayramGunu>().Query().AsNoTracking()
            .Where(x => !x.Silinib && x.Tarix >= bas && x.Tarix <= son)
            .ToDictionaryAsync(x => x.Tarix.Date, x => x.Tip);
        for (var d = bas; d <= son; d = d.AddDays(1))
        {
            bool isGunu = xususiler.TryGetValue(d, out var tip)
                ? tip == GunTipi.IsGunu
                : d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday;
            if (isGunu) return d;
        }
        return bas;
    }

    // ── Azərbaycan dili köməkçiləri ─────────────────────────────────
    private static readonly string[] AyAdlari =
        { "", "yanvar", "fevral", "mart", "aprel", "may", "iyun",
          "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr" };

    // İl suffiksi: 2016-cı, 2017-ci, 2019-cu, 2020-ci, 2023-cü, 2026-cı ...
    private static string IlSuffiks(int il)
    {
        int son2 = il % 100, son = il % 10;
        string suf = son switch
        {
            1 or 2 or 5 or 7 or 8 => "-ci",
            3 or 4 => "-cü",
            6 => "-cı",
            9 => "-cu",
            _ => son2 switch   // sonu 0 olanlar
            {
                10 or 30 => "-cu",
                20 or 50 or 70 or 80 => "-ci",
                40 or 60 or 90 => "-cı",
                0 => "-ci",    // 2000, 2100 (min-ci)
                _ => "-ci"
            }
        };
        return suf;
    }

    private static string TarixSozle(DateTime t) =>
        $"{t:dd} {AyAdlari[t.Month]} {t.Year}{IlSuffiks(t.Year)} il";

    private static string TarixIlSuffiksli(DateTime t) =>
        $"{t:dd.MM.yyyy}{IlSuffiks(t.Year)}";

    // "Bəşirov Gürşad Bərşad oğluna" — ata adının sonluğu cinse görə
    private static string IsciYonluk(Isci i)
    {
        var ata = (i.AtaAdi ?? "").Trim();
        string quyruq;
        if (ata.EndsWith("oğlu", StringComparison.OrdinalIgnoreCase))
            { ata = ata[..^4].Trim(); quyruq = "oğluna"; }
        else if (ata.EndsWith("qızı", StringComparison.OrdinalIgnoreCase))
            { ata = ata[..^4].Trim(); quyruq = "qızına"; }
        else
            quyruq = i.Cins == Cins.Qadin ? "qızına" : "oğluna";
        var orta = string.IsNullOrEmpty(ata) ? "" : ata + " ";
        return $"{i.Soyad} {i.Ad} {orta}{quyruq}";
    }

    // "G. B. Bəşirovun" — inisiallar + soyadın yiyəlik halı
    private string IsciYiyelik(Isci i) => $"{Inisiallar(i)} {SoyadYiyelik(i.Soyad)}";

    private static string Inisiallar(Isci i)
    {
        var a = string.IsNullOrEmpty(i.Ad) ? "" : $"{i.Ad[0]}. ";
        var ata = (i.AtaAdi ?? "").Trim();
        foreach (var son in new[] { "oğlu", "qızı" })
            if (ata.EndsWith(son, StringComparison.OrdinalIgnoreCase)) ata = ata[..^son.Length].Trim();
        var b = string.IsNullOrEmpty(ata) ? "" : $"{ata[0]}. ";
        return (a + b).TrimEnd();
    }

    private static readonly char[] Saitler = { 'a', 'ı', 'o', 'u', 'e', 'ə', 'i', 'ö', 'ü',
                                              'A', 'I', 'O', 'U', 'E', 'Ə', 'İ', 'Ö', 'Ü' };
    private static char? SonSait(string s)
    {
        for (int i = s.Length - 1; i >= 0; i--)
            if (Saitler.Contains(s[i])) return char.ToLowerInvariant(s[i] == 'İ' ? 'i' : s[i]);
        return null;
    }
    private static bool Qalin(char c) => c is 'a' or 'ı' or 'o' or 'u';
    private static bool DodaqQalin(char c) => c is 'o' or 'u';
    private static bool DodaqInce(char c) => c is 'ö' or 'ü';

    // Yiyəlik: Bəşirov→Bəşirovun, Qulamova→Qulamovanın, Zadə→Zadənin
    private static string SoyadYiyelik(string soyad)
    {
        if (string.IsNullOrEmpty(soyad)) return soyad;
        var sonHerf = soyad[^1];
        var sait = SonSait(soyad) ?? 'a';
        string sufSait = DodaqQalin(sait) ? "u" : DodaqInce(sait) ? "ü" : Qalin(sait) ? "ı" : "i";
        if (Saitler.Contains(sonHerf))
            return soyad + "n" + sufSait + "n";
        return soyad + sufSait + "n";
    }

    // Yönlük: İsmayılov→İsmayılova, Abdullazadə→Abdullazadəyə, Zadə→Zadəyə
    private static string SoyadYonluk(string soyad)
    {
        if (string.IsNullOrEmpty(soyad)) return soyad;
        var sonHerf = soyad[^1];
        var sait = SonSait(soyad) ?? 'a';
        string suf = Qalin(sait) || DodaqQalin(sait) ? "a" : "ə";
        if (Saitler.Contains(sonHerf)) return soyad + "y" + suf;
        return soyad + suf;
    }

    // "kredit departamenti" → "kredit departamentinin"; "Rəhbərlik" → "Rəhbərliyin"
    private static string DepartamentYiyelik(string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return "";
        ad = ad.Trim();
        var sonHerf = ad[^1];
        var sait = SonSait(ad) ?? 'a';
        string sufSait = DodaqQalin(sait) ? "u" : DodaqInce(sait) ? "ü" : Qalin(sait) ? "ı" : "i";
        if (Saitler.Contains(sonHerf)) return ad + "n" + sufSait + "n";
        if (sonHerf is 'k') return ad[..^1] + "y" + sufSait + "n";   // Rəhbərlik → Rəhbərliyin
        if (sonHerf is 'q') return ad[..^1] + "ğ" + sufSait + "n";
        return ad + sufSait + "n";
    }

    // Vəzifənin yönlük halı: Vezife.YonlukHal doludursa o, yoxdursa avtomatik
    // ("rəis"→"rəisinə", "müavin"→"müavininə", "əməliyyatçı"→"əməliyyatçısına")
    private static string VezifeYonluk(IsciTeyinat? teyinat)
    {
        var vez = teyinat?.Vezife;
        if (vez == null) return "—";
        var deptGen = DepartamentYiyelik(vez.Departament?.Ad);
        string vezYon;
        if (!string.IsNullOrWhiteSpace(vez.YonlukHal))
            vezYon = vez.YonlukHal.Trim();
        else
        {
            var ad = vez.Ad.Trim();
            var sonHerf = ad[^1];
            var sait = SonSait(ad) ?? 'a';
            string s1 = DodaqQalin(sait) ? "u" : DodaqInce(sait) ? "ü" : Qalin(sait) ? "ı" : "i";
            string s2 = Qalin(sait) || DodaqQalin(sait) ? "a" : "ə";
            // Çoxsözlü və sonu i/ı/u/ü ilə bitən ad artıq MƏNSUBİYYƏT şəkilçilidir
            // ("müdir müavini", "baş kassirin müavini") → yalnız n + a/ə əlavə olunur.
            bool mensubiyyetli = ad.Contains(' ') &&
                (sonHerf is 'i' or 'ı' or 'u' or 'ü' || char.ToLowerInvariant(sonHerf) is 'i');
            if (mensubiyyetli)
                vezYon = ad + "n" + s2;         // müdir müavini → müdir müavininə
            else if (Saitler.Contains(sonHerf))
                vezYon = ad + "s" + s1 + "n" + s2;  // əməliyyatçı → əməliyyatçısına
            else
                vezYon = ad + s1 + "n" + s2;    // rəis → rəisinə, müavin → müavininə
        }
        // "Rəhbərlik" strukturu üçün departament adı əmrə yazılmır
        // ("bankın Rəhbərliyin müdir müavininə" əvəzinə "bankın müdir müavininə").
        if (vez.Departament?.Ad != null &&
            vez.Departament.Ad.Trim().Equals("Rəhbərlik", StringComparison.OrdinalIgnoreCase))
            deptGen = "";
        return string.IsNullOrEmpty(deptGen) ? vezYon : $"{deptGen} {vezYon}";
    }

    // Vəzifənin MƏNSUBİYYƏT forması (§2-də əvəzedici üçün):
    // "rəis" → "rəisi", "auditor" → "auditoru", "əməliyyatçı" → "əməliyyatçısı";
    // "rəis müavini" kimi onsuz da mənsubiyyətli adlar olduğu kimi qalır.
    private static string VezifeMensubiyyet(string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return "";
        ad = ad.Trim();
        var sonHerf = ad[^1];
        var sait = SonSait(ad) ?? 'a';
        string s1 = DodaqQalin(sait) ? "u" : DodaqInce(sait) ? "ü" : Qalin(sait) ? "ı" : "i";
        bool mensubiyyetli = ad.Contains(' ') &&
            (sonHerf is 'i' or 'ı' or 'u' or 'ü');
        if (mensubiyyetli) return ad;                     // rəis müavini → rəis müavini
        if (Saitler.Contains(sonHerf)) return ad + "s" + s1;  // əməliyyatçı → əməliyyatçısı
        return ad + s1;                                    // rəis → rəisi
    }

    // ── §2 bölməsinin silinməsi (əvəzedici seçilməyəndə) ────────────
    private static byte[] Paragraf2Sil(byte[] docx)
    {
        using var ms = new MemoryStream();
        ms.Write(docx, 0, docx.Length);
        using (var doc = WordprocessingDocument.Open(ms, true))
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            var paralar = body.Elements<Paragraph>().ToList();
            bool silmede = false;
            foreach (var p in paralar)
            {
                var metn = p.InnerText.Replace(" ", "");
                if (!silmede && metn.Contains("§2")) silmede = true;
                else if (silmede && (metn.Contains("BankMelli") || metn.Contains("müdiri:")))
                    break;
                else if (!silmede) continue;
                p.Remove();
            }
            doc.MainDocumentPart.Document.Save();
        }
        return ms.ToArray();
    }

    private static string Translit(string s)
    {
        var map = new Dictionary<char, string> {
            ['ə']="e",['Ə']="E",['ş']="sh",['Ş']="Sh",['ç']="ch",['Ç']="Ch",
            ['ğ']="g",['Ğ']="G",['ı']="i",['I']="I",['İ']="I",['ö']="o",['Ö']="O",
            ['ü']="u",['Ü']="U" };
        return string.Concat(s.Select(c => map.TryGetValue(c, out var v) ? v : c.ToString()));
    }
}

using System.Text.Json;
using FinNex.Application.DTOs.Muhasibat;
using FinNex.Application.DTOs.Sorgular;
using FinNex.Application.Interfaces.Muhasibat;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Sorgular;
using Microsoft.Extensions.Configuration;

namespace FinNex.Application.Services.Muhasibat;

// Mühasibat — Balans İcmalı servisi.
// Oracle YALNIZ SELECT (CLAUDE.md). Tarix parametri validasiya olunmuş DateTime-dır
// və ciddi formatla (dd/MM/yyyy) SQL-ə yerləşdirilir — sərbəst istifadəçi mətni deyil.
public class MuhasibatService : IMuhasibatService
{
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorgu;
    private readonly IConfiguration _config;

    public MuhasibatService(IOracleService oracle, IOracleSorguService sorgu, IConfiguration config)
    {
        _oracle = oracle;
        _sorgu = sorgu;
        _config = config;
    }

    // ── IFRS 9 floor parametrləri (DMS JSON — DB migration lazım deyil) ──────────
    private Ifrs9ParametrDto? _ifrs9Parametr;

    private string Ifrs9ParametrYol()
    {
        var dmsRoot = _config["DocumentStorage:RootPath"] ?? @"C:\FinNex_DMS";
        return System.IO.Path.Combine(dmsRoot, "hesabat-sablonlari", "ifrs9-parametr.json");
    }

    public async Task<Ifrs9ParametrDto> Ifrs9ParametrleriAsync()
    {
        if (_ifrs9Parametr != null) return _ifrs9Parametr;
        try
        {
            var yol = Ifrs9ParametrYol();
            if (System.IO.File.Exists(yol))
            {
                var json = await System.IO.File.ReadAllTextAsync(yol);
                _ifrs9Parametr = JsonSerializer.Deserialize<Ifrs9ParametrDto>(json) ?? new Ifrs9ParametrDto();
            }
            else _ifrs9Parametr = new Ifrs9ParametrDto();   // default = cari dəyərlər
        }
        catch { _ifrs9Parametr = new Ifrs9ParametrDto(); }
        return _ifrs9Parametr;
    }

    public async Task Ifrs9ParametrYazAsync(Ifrs9ParametrDto p)
    {
        var yol = Ifrs9ParametrYol();
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(yol)!);
        var json = JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(yol, json);
        _ifrs9Parametr = p;   // cache yenilə
    }

    // Floor placeholder-larını cari parametrlərlə əvəz et (Ifrs9Sql və Ifrs9AuditSql üçün).
    private static string FloorTetbiq(string sql, Ifrs9ParametrDto p)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return sql
            .Replace("{MENZIL_SERT}", p.MenzilGuzest ? "1=1" : "1=0")
            .Replace("{FLOOR_MENZIL}", (p.MenzilFloor / 100m).ToString("0.########", inv))
            .Replace("{FLOOR_S1}", (p.Stage1Floor / 100m).ToString("0.########", inv))
            .Replace("{FLOOR_S2}", (p.Stage2Floor / 100m).ToString("0.########", inv));
    }

    // Stage 3 bərpa termi — iki metod. KÖHNƏ (Excel) sətri toxunmadan qalır; MB seçiləndə
    // yalnız Stage 3 branch-ı runtime-da əvəz olunur (Ifrs9Sql/Ifrs9AuditSql mənbəyi dəyişmir).
    //   Excel: M_Stage3 = (batıqda qalan) × bərpa                 → risk% = (I/F) × bərpa
    //   MB:    M_Stage3 = EAD × (1 − bərpa)                       → risk% = 1 − bərpa  (LGD, PD=100%)
    private const string Stage3Excel =
        "(t.f-t.g-t.h-t.j-t.k)*CASE WHEN {MENZIL_SERT} AND t.sahe_kodu IN (1902,1904) THEN r.q2 ELSE r.p2 END";
    private const string Stage3MB =
        "t.f*(1-(CASE WHEN {MENZIL_SERT} AND t.sahe_kodu IN (1902,1904) THEN r.q2 ELSE r.p2 END))";

    // Metod tətbiqi: "MB" olduqda Stage 3 termini LGD=1−bərpa formasına çevirir.
    // "Excel" (default) — heç nə dəyişmir, köhnə SQL eynən qalır.
    private static string MetodTetbiq(string sql, Ifrs9ParametrDto p)
        => string.Equals(p.Metod, "MB", StringComparison.OrdinalIgnoreCase)
            ? sql.Replace(Stage3Excel, Stage3MB)
            : sql;

    // OracleSorgular-dakı sorğu adları (admin paneldən redaktə oluna bilər).
    // Tapılmasa aşağıdakı embedded SQL fallback işləyir.
    private const string AdBalans   = "Muhasibat — Balans qaliqlari";
    private const string AdMuqayise = "Muhasibat — Balans muqayise";
    private const string AdDepozit  = "Muhasibat — Depozit hesablari";
    private const string AdElaqeli  = "Muhasibat — Elaqeli teref";
    private const string AdKredit   = "Muhasibat — Kredit portfeli";
    private const string AdValyuta  = "Muhasibat — Valyuta emeliyyatlari";
    private const string AdRezident = "Muhasibat — Rezident";
    private const string AdRezidentDetal = "Muhasibat — Rezident detal";
    private const string AdElaqeliDetal  = "Muhasibat — Elaqeli detal";
    private const string AdMenfeet       = "Muhasibat — Menfeet zerer";
    private const string AdMenfeetDetal  = "Muhasibat — Menfeet detal";
    private const string AdMenfeetBaza   = "Muhasibat — Menfeet baza";
    private const string AdMaturity          = "Muhasibat — Maturity ladder";
    private const string AdMaturityKontekst  = "Muhasibat — Maturity kontekst";
    private const string AdKeyfiyyet       = "Muhasibat — Kredit keyfiyyet";
    private const string AdKeyfiyyetGirov  = "Muhasibat — Kredit girov";
    private const string AdKeyfiyyetBaza   = "Muhasibat — Kredit keyfiyyet baza";
    private const string AdKeyfiyyetDetal  = "Muhasibat — Kredit keyfiyyet detal";
    private const string AdYerlesdirme     = "Muhasibat — Yerlesdirme";

    // Sorğu siyahısı bir dəfə oxunur və scoped servis ömrü boyu cache-lənir (DbContext).
    // Bu, GunlukIcmal kimi çoxlu SqlAl çağıran bölmələrdə DbContext-i təkrar-təkrar vurmur
    // və alt-bölmələrin PARALEL işləməsinə imkan verir (əvvəlcədən SorgulariYukleAsync ilə doldurulur).
    private List<OracleSorguDto>? _sorguCache;

    private async Task<List<OracleSorguDto>> SorgulariYukleAsync()
        => _sorguCache ??= (await _sorgu.HamisiniGetirAsync())?.Data?.ToList() ?? new List<OracleSorguDto>();

    // SQL-i OracleSorgular-dan ad ilə oxu (cache-dən). Yoxdursa xəta at (embedded fallback yoxdur).
    private async Task<string> SqlAl(string ad)
    {
        var list = await SorgulariYukleAsync();
        var q = list.FirstOrDefault(x => x.Aktiv
            && !string.IsNullOrWhiteSpace(x.SorguMetni)
            && string.Equals((x.SorguAdi ?? "").Trim(), ad, StringComparison.OrdinalIgnoreCase));
        if (q == null)
            throw new InvalidOperationException(
                $"OracleSorgular-da sorğu tapılmadı: '{ad}'. Muhasibat INSERT script işlədilməlidir.");
        return q.SorguMetni;
    }

    public async Task<MuhasibatIcmalDto> GunlukIcmalAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatIcmalDto { Tarix = t };

        try
        {
            // Sorğu mətnlərini əvvəlcədən cache-ə yığ (DbContext, ardıcıl) — sonra bölmələr
            // PARALEL işləyə bilər: SqlAl artıq cache-dən oxuyur (DbContext-ə vurmur), Oracle
            // sorğuları isə hər biri öz bağlantısını açır (OracleService.SelectAsync = yeni con),
            // deməli paralel təhlükəsizdir. Ardıcıl 7 sorğu → eyni anda (vaxt = ən yavaş bölmə).
            await SorgulariYukleAsync();

            var balT = BalansAsync(t);
            var depT = DepozitAsync(t);
            var krdT = KreditPortfelAsync(t);
            var lkvT = LikvidlikAsync(t);
            var pnlT = MenfeetAsync(new DateTime(t.Year, 1, 1), t);
            await Task.WhenAll(balT, depT, krdT, lkvT, pnlT);

            var bal = balT.Result;
            var dep = depT.Result;
            var krd = krdT.Result;
            var lkv = lkvT.Result;
            var pnl = pnlT.Result;

            dto.UmumiAktiv     = bal.UmumiAktiv;
            dto.UmumiOhdelik   = bal.UmumiOhdelik;
            dto.Kapital        = bal.Kapital;
            dto.XalisMenfeet   = bal.XalisMenfeet;
            dto.Roa            = bal.Roa;
            dto.Roe            = bal.Roe;
            dto.KapitalAktiv   = bal.UmumiAktiv != 0 ? Math.Round(bal.Kapital / bal.UmumiAktiv * 100, 2) : 0;
            dto.AktivDeyisme   = bal.AktivDeyisme;
            dto.OhdelikDeyisme = bal.OhdelikDeyisme;
            dto.KapitalDeyisme = bal.KapitalDeyisme;
            dto.MenfeetDeyisme = bal.MenfeetDeyisme;
            dto.MuqayiseVar    = bal.MuqayiseVar;

            dto.DepozitPortfel = dep.UmumiPortfel;
            dto.DepozitorSayi  = dep.MusteriSayi;

            dto.KreditPortfel = krd.UmumiPortfel;
            dto.KreditSayi    = krd.MuqavileSayi;
            dto.Npl           = krd.NplMebleg;
            dto.NplFaiz       = krd.NplFaiz;

            dto.Lcr          = lkv.Lcr;
            dto.Hqla         = lkv.Hqla;
            dto.AniLikvidlik = lkv.AniLikvidlik;

            dto.Nii                   = pnl.XalisFaizGeliri;
            dto.Nim                   = pnl.Nim;
            dto.EhtiyatdanEvvelMenfeet = pnl.EhtiyatdanEvvelMenfeet;

            dto.Ugurlu = bal.Ugurlu;
            if (!bal.Ugurlu) dto.Xeta = bal.Xeta;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    public async Task<MuhasibatBalansDto> BalansAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatBalansDto { Tarix = t };

        try
        {
            var sql = (await SqlAl(AdBalans)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 200000);

            var aktiv   = new Dictionary<string, decimal>();
            var ohdelik = new Dictionary<string, decimal>();
            var valyuta = new Dictionary<string, decimal>();
            decimal kapital = 0m, tesnifsiz = 0m, menfeet = 0m;

            foreach (var r in rows)
            {
                var hesab = Val(r, "hesab")?.ToString() ?? "";
                var ad = Val(r, "ad")?.ToString() ?? "";
                var valKod = Val(r, "valyuta")?.ToString() ?? "";
                var qaliq = Dec(Val(r, "qaliq"));
                if (qaliq == 0m) continue;

                // Cari ilin mənfəəti (50130*) — kredit qalıqlı, çevir
                if (hesab.StartsWith("50130")) menfeet += -qaliq;

                // Real müştəri depoziti (frm_Dep məntiqi) — Depozit tab-ı ilə tam uyğun
                var depTip = Val(r, "dep_tip")?.ToString() ?? "X";
                if (depTip == "H")
                {
                    ohdelik["Hüquqi şəxs depozitləri"] = ohdelik.GetValueOrDefault("Hüquqi şəxs depozitləri") + (-qaliq);
                    continue;
                }
                if (depTip == "F")
                {
                    ohdelik["Fiziki şəxs depozitləri"] = ohdelik.GetValueOrDefault("Fiziki şəxs depozitləri") + (-qaliq);
                    continue;
                }

                // Kredit faizi artıq ayrıca sətir DEYİL — "Müştərilərə kreditlər"ə qatılır
                // (əsas + faiz + kredit ehtiyatları = xalis kredit). Ona görə kredit_novu
                // override götürülüb; faiz Tesnif-dən birbaşa "Müştərilərə kreditlər" qalır.
                var (kat, qrup) = Tesnif(hesab, ad);
                switch (kat)
                {
                    case "aktiv":
                        aktiv[qrup] = aktiv.GetValueOrDefault(qrup) + qaliq;   // debet-müsbət
                        var vad = ValyutaAd(valKod);
                        valyuta[vad] = valyuta.GetValueOrDefault(vad) + qaliq;
                        break;
                    case "ohdelik":
                        ohdelik[qrup] = ohdelik.GetValueOrDefault(qrup) + (-qaliq);  // kredit-mənfi → çevir
                        break;
                    case "kapital":
                        kapital += (-qaliq);
                        break;
                    default:
                        tesnifsiz += qaliq;
                        break;
                }
            }

            dto.UmumiAktiv   = Math.Round(aktiv.Values.Sum(), 2);
            dto.UmumiOhdelik = Math.Round(ohdelik.Values.Sum(), 2);
            dto.Kapital      = Math.Round(kapital, 2);
            dto.Tesnifsiz    = Math.Round(tesnifsiz, 2);

            // Gəlirlilik — ROA / ROE (YTD) + illikləşdirilmiş
            dto.XalisMenfeet = Math.Round(menfeet, 2);
            dto.Roa = dto.UmumiAktiv != 0 ? Math.Round(menfeet / dto.UmumiAktiv * 100, 2) : 0;
            dto.Roe = dto.Kapital != 0 ? Math.Round(menfeet / dto.Kapital * 100, 2) : 0;
            var gun = t.DayOfYear;
            var faktor = gun > 0 ? 365m / gun : 1m;
            dto.RoaIllik = Math.Round(dto.Roa * faktor, 2);
            dto.RoeIllik = Math.Round(dto.Roe * faktor, 2);

            // Əvvəlki iş günü ilə müqayisə (xəta əsas balansı pozmasın)
            try
            {
                var msql = (await SqlAl(AdMuqayise)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                var mrows = await _oracle.SelectAsync(msql, maxRows: 5);
                if (mrows.Count > 0 && Dec(Val(mrows[0], "aktiv")) != 0)
                {
                    dto.AktivDeyisme   = Math.Round(dto.UmumiAktiv   - Dec(Val(mrows[0], "aktiv")), 2);
                    dto.OhdelikDeyisme = Math.Round(dto.UmumiOhdelik - Dec(Val(mrows[0], "ohdelik")), 2);
                    dto.KapitalDeyisme = Math.Round(dto.Kapital      - Dec(Val(mrows[0], "kapital")), 2);
                    dto.MenfeetDeyisme = Math.Round(dto.XalisMenfeet - Dec(Val(mrows[0], "menfeet")), 2);
                    dto.MuqayiseVar = true;
                }
            }
            catch { /* müqayisə alınmadı — oxlar göstərilməz */ }

            dto.Aktivler = aktiv.Where(x => Math.Abs(x.Value) > 0.005m)
                .OrderBy(x => SiraNo(AktivSira, x.Key)).ThenByDescending(x => x.Value)
                .Select(x => new BalansMaddeDto
                {
                    Ad = x.Key, Mebleg = Math.Round(x.Value, 2),
                    Faiz = dto.UmumiAktiv != 0 ? Math.Round(x.Value / dto.UmumiAktiv * 100, 1) : 0
                }).ToList();

            dto.Ohdelikler = ohdelik.Where(x => Math.Abs(x.Value) > 0.005m)
                .OrderBy(x => SiraNo(OhdelikSira, x.Key)).ThenByDescending(x => x.Value)
                .Select(x => new BalansMaddeDto
                {
                    Ad = x.Key, Mebleg = Math.Round(x.Value, 2),
                    Faiz = dto.UmumiOhdelik != 0 ? Math.Round(x.Value / dto.UmumiOhdelik * 100, 1) : 0
                }).ToList();

            dto.ValyutaBolgusu = valyuta.Where(x => Math.Abs(x.Value) > 0.005m)
                .OrderByDescending(x => x.Value)
                .Select(x => new BalansMaddeDto
                {
                    Ad = x.Key, Mebleg = Math.Round(x.Value, 2),
                    Faiz = dto.UmumiAktiv != 0 ? Math.Round(x.Value / dto.UmumiAktiv * 100, 1) : 0
                }).ToList();

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    public async Task<MuhasibatDepozitDto> DepozitAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatDepozitDto { Tarix = t };

        try
        {
            var sql = (await SqlAl(AdDepozit)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 300000);

            decimal huquqi = 0m, fiziki = 0m, sahibkar = 0m;
            var valyuta = new Dictionary<string, decimal>();
            // müştəri açarı (tip|qeyd) → (ad, tip, cəm)
            var musteriler = new Dictionary<string, (string ad, string tip, decimal meb)>();

            foreach (var r in rows)
            {
                var tip = Val(r, "tip")?.ToString() ?? "";
                var qeyd = Val(r, "qeyd")?.ToString() ?? "";
                var ad = Val(r, "musteri")?.ToString() ?? "(adsız)";
                var vk = Val(r, "valyuta")?.ToString() ?? "";
                var q = Dec(Val(r, "qaliq"));
                if (q == 0m) continue;

                if (tip == "sahibkar") sahibkar += q;
                else if (tip == "fiziki") fiziki += q;
                else huquqi += q;

                var vad = ValyutaAd(vk);
                valyuta[vad] = valyuta.GetValueOrDefault(vad) + q;

                var key = tip + "|" + qeyd;
                if (musteriler.TryGetValue(key, out var cur))
                    musteriler[key] = (cur.ad, cur.tip, cur.meb + q);
                else
                    musteriler[key] = (ad, tip, q);
            }

            dto.HuquqiCem    = Math.Round(huquqi, 2);
            dto.FizikiCem    = Math.Round(fiziki, 2);
            dto.SahibkarCem  = Math.Round(sahibkar, 2);
            dto.UmumiPortfel = Math.Round(huquqi + fiziki + sahibkar, 2);
            dto.MusteriSayi  = musteriler.Count;

            // Konsentrasiya — bütün depozitorlar üzrə ən böyükləri
            var sirali = musteriler.Values.OrderByDescending(m => m.meb).ToList();
            var top10 = sirali.Take(10).Sum(m => m.meb);
            var top20 = sirali.Take(20).Sum(m => m.meb);
            dto.Top10Pay = dto.UmumiPortfel != 0 ? Math.Round(top10 / dto.UmumiPortfel * 100, 1) : 0;
            dto.Top20Pay = dto.UmumiPortfel != 0 ? Math.Round(top20 / dto.UmumiPortfel * 100, 1) : 0;

            // Əlaqəli tərəf (normativ) — ayrıca sorğu; xəta əsas depoziti pozmasın
            try
            {
                var esql = (await SqlAl(AdElaqeli)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                var erows = await _oracle.SelectAsync(esql, maxRows: 5);
                if (erows.Count > 0)
                {
                    var portfel = Dec(Val(erows[0], "portfel"));
                    var elaqeli = Dec(Val(erows[0], "elaqeli"));
                    dto.ElaqeliDepozit = Math.Round(elaqeli, 2);
                    dto.ElaqeliPortfel = Math.Round(portfel, 2);
                    dto.ElaqeliXususiCeki = portfel != 0 ? Math.Round(elaqeli / portfel * 100, 2) : 0;
                }
            }
            catch { /* əlaqəli tərəf hesablanmadı — panel boş qalar */ }

            dto.TipBolgusu = new List<BalansMaddeDto>
            {
                new() { Ad = "Hüquqi şəxslər", Mebleg = dto.HuquqiCem,
                        Faiz = dto.UmumiPortfel != 0 ? Math.Round(dto.HuquqiCem / dto.UmumiPortfel * 100, 1) : 0 },
                new() { Ad = "Fiziki şəxslər", Mebleg = dto.FizikiCem,
                        Faiz = dto.UmumiPortfel != 0 ? Math.Round(dto.FizikiCem / dto.UmumiPortfel * 100, 1) : 0 },
                new() { Ad = "Sahibkarlar", Mebleg = dto.SahibkarCem,
                        Faiz = dto.UmumiPortfel != 0 ? Math.Round(dto.SahibkarCem / dto.UmumiPortfel * 100, 1) : 0 },
            };

            dto.ValyutaBolgusu = valyuta.Where(x => Math.Abs(x.Value) > 0.005m)
                .OrderByDescending(x => x.Value)
                .Select(x => new BalansMaddeDto
                {
                    Ad = x.Key, Mebleg = Math.Round(x.Value, 2),
                    Faiz = dto.UmumiPortfel != 0 ? Math.Round(x.Value / dto.UmumiPortfel * 100, 1) : 0
                }).ToList();

            dto.TopHuquqi = musteriler.Values.Where(m => m.tip == "huquqi")
                .OrderByDescending(m => m.meb).Take(10)
                .Select(m => new BalansMaddeDto
                {
                    Ad = m.ad, Mebleg = Math.Round(m.meb, 2),
                    Faiz = dto.HuquqiCem != 0 ? Math.Round(m.meb / dto.HuquqiCem * 100, 1) : 0
                }).ToList();

            dto.TopFiziki = musteriler.Values.Where(m => m.tip == "fiziki")
                .OrderByDescending(m => m.meb).Take(10)
                .Select(m => new BalansMaddeDto
                {
                    Ad = m.ad, Mebleg = Math.Round(m.meb, 2),
                    Faiz = dto.FizikiCem != 0 ? Math.Round(m.meb / dto.FizikiCem * 100, 1) : 0
                }).ToList();

            dto.TopSahibkar = musteriler.Values.Where(m => m.tip == "sahibkar")
                .OrderByDescending(m => m.meb).Take(10)
                .Select(m => new BalansMaddeDto
                {
                    Ad = m.ad, Mebleg = Math.Round(m.meb, 2),
                    Faiz = dto.SahibkarCem != 0 ? Math.Round(m.meb / dto.SahibkarCem * 100, 1) : 0
                }).ToList();

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    public async Task<MuhasibatKreditDto> KreditPortfelAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatKreditDto { Tarix = t };

        try
        {
            var sql = (await SqlAl(AdKredit)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 300000);

            var tipD = new Dictionary<string, decimal>();
            var teyinatD = new Dictionary<string, decimal>();
            var valyutaD = new Dictionary<string, decimal>();
            var gecikmeD = new Dictionary<string, decimal>();
            decimal total = 0m, vkTotal = 0m, npl = 0m;
            int say = 0;

            foreach (var r in rows)
            {
                var kurs = Dec(Val(r, "kurs"));
                var esas = Dec(Val(r, "esas"));
                var vk = Dec(Val(r, "vk"));
                var gec = (int)Dec(Val(r, "gec_gun"));
                var qaliq = (esas + vk) * kurs;
                // Açıq müqavilə (date_close null) qalığı 0 olsa belə sayılır (b/k/faizdə ola bilər).

                total += qaliq;
                vkTotal += vk * kurs;
                say++;
                if (gec >= 90) npl += qaliq;

                var tip = TipAd((int)Dec(Val(r, "tip")));
                tipD[tip] = tipD.GetValueOrDefault(tip) + qaliq;

                // teyinat artıq index_otrasli cədvəlinin ADI-dır (sorğuda join olunub), kod yox.
                var tey = Val(r, "teyinat")?.ToString();
                if (string.IsNullOrWhiteSpace(tey)) tey = "(təyinatsız)";
                teyinatD[tey] = teyinatD.GetValueOrDefault(tey) + qaliq;

                var vad = ValyutaAd(Val(r, "valyuta")?.ToString() ?? "");
                valyutaD[vad] = valyutaD.GetValueOrDefault(vad) + qaliq;

                var age = AgeAd(gec);
                gecikmeD[age] = gecikmeD.GetValueOrDefault(age) + qaliq;
            }

            dto.UmumiPortfel = Math.Round(total, 2);
            dto.VkMebleg = Math.Round(vkTotal, 2);
            dto.MuqavileSayi = say;
            dto.NplMebleg = Math.Round(npl, 2);
            dto.NplFaiz = total != 0 ? Math.Round(npl / total * 100, 2) : 0;

            dto.TipBolgusu     = ToMadde(tipD, total);
            dto.TeyinatBolgusu = ToMadde(teyinatD, total);
            dto.ValyutaBolgusu = ToMadde(valyutaD, total);
            dto.GecikmeBolgusu = ToMaddeSira(gecikmeD, total);

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    public async Task<MuhasibatMenfeetDto> MenfeetAsync(DateTime? bas = null, DateTime? son = null)
    {
        var s = (son ?? DateTime.Now.Date.AddDays(-1)).Date;
        var b = (bas ?? new DateTime(s.Year, 1, 1)).Date;   // default: il əvvəli → hesabat tarixi (YTD)
        var dto = new MuhasibatMenfeetDto { BasTarix = b, SonTarix = s };

        try
        {
            // 1) P&L dövriyyə (kateqoriya) + 2) baza (işləyən aktiv + 50130) — PARALEL.
            var plSql = (await SqlAl(AdMenfeet))
                .Replace("{BAS}", b.ToString("dd/MM/yyyy"))
                .Replace("{SON}", s.ToString("dd/MM/yyyy"));
            var bazaSql = (await SqlAl(AdMenfeetBaza)).Replace("{SON}", s.ToString("dd/MM/yyyy"));

            var plTask   = _oracle.SelectAsync(plSql, maxRows: 200);
            var bazaTask = _oracle.SelectAsync(bazaSql, maxRows: 5);
            await Task.WhenAll(plTask, bazaTask);
            var rows  = plTask.Result;
            var brows = bazaTask.Result;

            var gelirD = new Dictionary<string, decimal>();
            var xercD  = new Dictionary<string, decimal>();   // ƏMƏLİYYAT xərci (ehtiyatsız)
            decimal ehtiyatGross = 0m;                          // 89 provision — gross churn (ayrıca)
            foreach (var r in rows)
            {
                var sinif2 = Val(r, "sinif2")?.ToString() ?? "";
                var debet  = Dec(Val(r, "debet"));
                var kredit = Dec(Val(r, "kredit"));
                var (kat, gelir) = MenfeetTesnif(sinif2);
                var meb = gelir ? kredit : debet;
                if (meb == 0m) continue;
                if (gelir) gelirD[kat] = gelirD.GetValueOrDefault(kat) + meb;
                else if (kat == "Ehtiyat xərci") ehtiyatGross += meb;   // gross — hər gün yenidən hesablanır (şişir)
                else xercD[kat] = xercD.GetValueOrDefault(kat) + meb;
            }

            // Kurs fərqi (66 gəlir − 86 xərc) və Dilinq fərqi (68 gəlir − 88 xərc).
            // İkisi də valyuta əməliyyatlarının nəticəsidir: kurs = mövqe gündəlik yenidən
            // qiymətləndirmə (GROSS böyük, bir-birini kompensasiya edir), dilinq = dealing.
            // Hər ikisi NET olaraq GƏLİR strukturunda göstərilir (mənfi ola bilər) və
            // ƏMƏLİYYAT XƏRCİNƏ DAXİL EDİLMİR. Pre-provision mənfəət dəyişmir (cəbri eyni).
            var kursGelir   = gelirD.GetValueOrDefault("Kurs fərqi gəliri");
            var kursZerer   = xercD.GetValueOrDefault("Kurs fərqi zərəri");
            var dilinqGelir = gelirD.GetValueOrDefault("Dilinq fərqi gəliri");
            var dilinqZerer = xercD.GetValueOrDefault("Dilinq fərqi zərəri");
            gelirD.Remove("Kurs fərqi gəliri");
            gelirD.Remove("Dilinq fərqi gəliri");
            xercD.Remove("Kurs fərqi zərəri");
            xercD.Remove("Dilinq fərqi zərəri");
            gelirD["Kurs fərqi"]   = kursGelir - kursZerer;
            gelirD["Dilinq fərqi"] = dilinqGelir - dilinqZerer;

            // Faiz gəliri cəmi = 4 alt-kateqoriyanın toplamı (NII/NIM/Excel bunu istifadə edir).
            dto.FaizGeliri   = Math.Round(FaizGelirKatlar.Sum(k => gelirD.GetValueOrDefault(k)), 2);
            dto.FaizXerci    = Math.Round(xercD.GetValueOrDefault("Faiz xərci"), 2);
            dto.EhtiyatGross = Math.Round(ehtiyatGross, 2);
            dto.UmumiGelir   = Math.Round(gelirD.Values.Sum(), 2);            // əməliyyat gəliri
            dto.UmumiXerc    = Math.Round(xercD.Values.Sum(), 2);            // əməliyyat xərci (ehtiyatsız)
            dto.XalisFaizGeliri = Math.Round(dto.FaizGeliri - dto.FaizXerci, 2);
            dto.EhtiyatdanEvvelMenfeet = Math.Round(dto.UmumiGelir - dto.UmumiXerc, 2);
            dto.XercGelirNisbeti = dto.UmumiGelir != 0 ? Math.Round(dto.UmumiXerc / dto.UmumiGelir * 100, 2) : 0;
            dto.GelirBolgusu = ToMadde(gelirD, dto.UmumiGelir);
            dto.XercBolgusu  = ToMadde(xercD, dto.UmumiXerc);

            // 2) Baza nəticəsi — işləyən aktiv (NIM məxrəci) + 50130 mənfəəti (yoxlama).
            // Kiçik aqreqat (2 rəqəm), ≤SON son iş günü — 200k sətir çəkilmir.
            var bazaRow = brows.FirstOrDefault();
            decimal isleyen = bazaRow != null ? Dec(Val(bazaRow, "isleyen")) : 0m;
            decimal menfeetGL = bazaRow != null ? Dec(Val(bazaRow, "menfeet")) : 0m;
            dto.IsleyenAktiv = Math.Round(isleyen, 2);
            dto.MenfeetGL    = Math.Round(menfeetGL, 2);
            // Xalis ehtiyat TÖRƏDİLİR: ehtiyatdan əvvəl mənfəət − GL mənfəəti (89 gross churn əvəzinə real net).
            dto.XalisEhtiyat = Math.Round(dto.EhtiyatdanEvvelMenfeet - dto.MenfeetGL, 2);
            dto.XalisMenfeet = dto.MenfeetGL;   // GL ilə tutuşur (konstruksiyaya görə)

            // NIM (illik, təxmini) = xalis faiz gəliri / işləyən aktiv × (365/gün).
            var gun = (s - b).Days + 1;
            dto.Nim = (isleyen != 0m && gun > 0)
                ? Math.Round(dto.XalisFaizGeliri / isleyen * (365m / gun) * 100m, 2)
                : 0;

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // Faiz gəliri alt-kateqoriyaları (hesab prefiksinə görə). NII/NIM üçün cəmi bunların
    // toplamıdır. Sıra dəyişsə həm cəmi, həm drill-down təsnifatı avtomatik izləyir.
    private static readonly string[] FaizGelirKatlar =
    {
        "Qiymətli kağızlar üzrə faiz",     // 60
        "Overnayt depozit və repo faizi",  // 61
        "Kreditlər üzrə faiz",             // 63, 64
        "Digər faiz gəliri"                // 65
    };

    // P&L kateqoriya təsnifatı hesab sinifinə (ilk 2 rəqəm) görə.
    // gelir=true → sinif 6/7 (kredit dövriyyəsi); gelir=false → sinif 8 (debet dövriyyəsi).
    // Faiz gəliri prefiksə görə 4 alt-kateqoriyaya bölünür (60/61/63-64/65).
    private static (string kat, bool gelir) MenfeetTesnif(string sinif2) => sinif2 switch
    {
        "60"                                  => ("Qiymətli kağızlar üzrə faiz", true),
        "61"                                  => ("Overnayt depozit və repo faizi", true),
        "63" or "64"                          => ("Kreditlər üzrə faiz", true),
        "65"                                  => ("Digər faiz gəliri", true),
        "66"                                  => ("Kurs fərqi gəliri", true),     // mövqe yenidən qiymətləndirmə
        "68"                                  => ("Dilinq fərqi gəliri", true),   // dilinq (dealing) əməliyyatı
        "67"                                  => ("Komissiya gəliri", true),
        "70" or "72"                          => ("Digər gəlir", true),
        "81" or "82" or "84" or "85"          => ("Faiz xərci", false),
        "86"                                  => ("Kurs fərqi zərəri", false),    // mövqe yenidən qiymətləndirmə
        "88"                                  => ("Dilinq fərqi zərəri", false),  // dilinq (dealing) əməliyyatı
        "87"                                  => ("Komissiya xərci", false),
        "89"                                  => ("Ehtiyat xərci", false),
        _ => sinif2.StartsWith("8") ? ("Digər xərc", false) : ("Digər gəlir", true)
    };

    public async Task<MuhasibatKeyfiyyetDto> KreditKeyfiyyetAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatKeyfiyyetDto { Tarix = t };

        try
        {
            // 1) Təsnifat qrupları (ehtiyat dərəcəsi üzrə): say/qalıq/ehtiyat.
            var sql = (await SqlAl(AdKeyfiyyet)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 20);

            var adlar = new Dictionary<int, string>
            {
                [1] = "Standart", [2] = "Nəzarət altında", [3] = "Qeyri-standart",
                [4] = "Şübhəli", [5] = "Ümidsiz (zərər)"
            };
            var katMap = new Dictionary<int, KeyfiyyetKatDto>();
            foreach (var r in rows)
            {
                var kat = (int)Dec(Val(r, "kat"));
                if (!adlar.ContainsKey(kat)) continue;
                katMap[kat] = new KeyfiyyetKatDto
                {
                    Ad = adlar[kat],
                    Say = (int)Dec(Val(r, "say")),
                    Qaliq = Math.Round(Dec(Val(r, "qaliq")), 2),
                    Ehtiyat = Math.Round(Dec(Val(r, "ehtiyat")), 2)
                };
            }

            dto.Portfel      = Math.Round(katMap.Values.Sum(x => x.Qaliq), 2);
            dto.Ehtiyat      = Math.Round(katMap.Values.Sum(x => x.Ehtiyat), 2);
            dto.MuqavileSayi = katMap.Values.Sum(x => x.Say);
            dto.EhtiyatFaiz  = dto.Portfel != 0 ? Math.Round(dto.Ehtiyat / dto.Portfel * 100, 2) : 0;
            // Problemli (impaired) = qeyri-standart + şübhəli + ümidsiz (kat 3-5).
            dto.ProblemliQaliq = Math.Round(
                new[] { 3, 4, 5 }.Where(katMap.ContainsKey).Sum(k => katMap[k].Qaliq), 2);
            dto.Ortuyu = dto.ProblemliQaliq != 0 ? Math.Round(dto.Ehtiyat / dto.ProblemliQaliq * 100, 1) : 0;

            // Kanonik sıra (1→5), rəng (yaşıl→qırmızı) + payla.
            var renglar = new Dictionary<int, string>
            {
                [1] = "#16a34a", [2] = "#84cc16", [3] = "#f59e0b", [4] = "#f97316", [5] = "#dc2626"
            };
            foreach (var k in new[] { 1, 2, 3, 4, 5 })
                if (katMap.TryGetValue(k, out var kd))
                {
                    kd.Faiz = dto.Portfel != 0 ? Math.Round(kd.Qaliq / dto.Portfel * 100, 1) : 0;
                    kd.Reng = renglar[k];
                    dto.Kateqoriyalar.Add(kd);
                }

            // 2) Girov strukturu — növ üzrə (tipzaloga → tipzal.name). Təminatsız = tipzaloga 8.
            var gsql = (await SqlAl(AdKeyfiyyetGirov)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var grows = await _oracle.SelectAsync(gsql, maxRows: 100);
            foreach (var r in grows)
            {
                var kod = (int)Dec(Val(r, "kod"));
                var qaliq = Math.Round(Dec(Val(r, "qaliq")), 2);
                // Açıq kredit (date_close null) qalığı 0 olsa belə görünür — say düz gəlsin.
                var ad = Val(r, "ad")?.ToString();
                if (string.IsNullOrWhiteSpace(ad)) ad = kod == 8 ? "Girovsuz" : $"Digər (#{kod})";
                var girov = Math.Round(Dec(Val(r, "girov")), 2);
                var say = (int)Dec(Val(r, "say"));
                // Girovsuz (tipzaloga 8) — təminatsız; qalanı təminatlı.
                if (kod == 8) { dto.GirovsuzSay += say; dto.GirovsuzQaliq += qaliq; }
                else { dto.GirovluSay += say; dto.GirovluQaliq += qaliq; dto.GirovCem += girov; }
                dto.GirovStrukturu.Add(new KeyfiyyetKatDto
                {
                    Ad = ad, Say = say, Qaliq = qaliq, Ehtiyat = girov,
                    Reng = kod == 8 ? "#94a3b8" : "#7c3aed"
                });
            }
            dto.GirovluQaliq  = Math.Round(dto.GirovluQaliq, 2);
            dto.GirovsuzQaliq = Math.Round(dto.GirovsuzQaliq, 2);
            dto.GirovCem      = Math.Round(dto.GirovCem, 2);
            dto.OrtaLtv       = dto.GirovCem != 0 ? Math.Round(dto.GirovluQaliq / dto.GirovCem * 100, 1) : 0;
            var gtotal = dto.GirovluQaliq + dto.GirovsuzQaliq;
            foreach (var g in dto.GirovStrukturu)
                g.Faiz = gtotal != 0 ? Math.Round(g.Qaliq / gtotal * 100, 1) : 0;
            dto.GirovStrukturu = dto.GirovStrukturu.OrderByDescending(x => x.Qaliq).ToList();

            // 3) Restrukt (bir sətir).
            var bsql = (await SqlAl(AdKeyfiyyetBaza)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var brows = await _oracle.SelectAsync(bsql, maxRows: 5);
            var br = brows.FirstOrDefault();
            if (br != null)
            {
                dto.RestruktSay   = (int)Dec(Val(br, "restrukt_say"));
                dto.RestruktQaliq = Math.Round(Dec(Val(br, "restrukt_qaliq")), 2);
            }

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // Ehtiyat dərəcəsini (0-100 %) təsnifat kateqoriyasına (1-5) çevir.
    private static int KeyfiyyetKat(decimal rez) =>
        rez <= 5 ? 1 : rez <= 20 ? 2 : rez <= 50 ? 3 : rez < 100 ? 4 : 5;

    public async Task<MuhasibatYerlesdirmeDto> YerlesdirmeAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatYerlesdirmeDto { Tarix = t };

        try
        {
            var sql = (await SqlAl(AdYerlesdirme)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 5000);

            // kontragent adı → (AZN qalıq, say, faiz×qalıq ağırlığı, ehtiyat AZN, AMB-mi)
            var kontragentD = new Dictionary<string, (decimal qaliq, int say, decimal faizAgirliq, decimal ehtiyat, bool amb)>();
            var valyutaD = new Dictionary<string, decimal>();
            var muddetD  = new Dictionary<string, decimal>();
            decimal total = 0m, faizAgirliqCem = 0m, illikGelir = 0m, ehtiyatCem = 0m;
            int say = 0;

            foreach (var r in rows)
            {
                var hesab5 = Val(r, "hesab5")?.ToString() ?? "";
                var kurs = Dec(Val(r, "kurs"));
                var esas = Dec(Val(r, "esas"));
                var faiz = Dec(Val(r, "faiz"));
                var ehtiyatFaiz = Dec(Val(r, "ehtiyat_faiz"));
                var qaliq = esas * kurs;
                var ehtiyat = qaliq * ehtiyatFaiz / 100m;
                var amb = hesab5.StartsWith("11");
                // Açıq yerləşdirmə (date_close null) qalığı 0 olsa belə sayılır.

                total += qaliq;
                say++;
                faizAgirliqCem += qaliq * faiz;      // AZN-qalıqla ölçülü faiz
                illikGelir += qaliq * faiz / 100m;
                ehtiyatCem += ehtiyat;

                // AMB overnight (11xxx) vs banklararası (15xxx / digər)
                if (amb) { dto.AmbMebleg += qaliq; dto.AmbSay++; }
                else { dto.BanklararasiMebleg += qaliq; dto.BanklararasiSay++; }

                // Vaxtı keçmiş / problemli: plan-bağlanma keçib, amma hələ açıq (pul qayıtmayıb).
                if (Val(r, "planbaglanma") is DateTime pb && pb.Date < t)
                { dto.VaxtiKecmisSay++; dto.VaxtiKecmisMebleg += qaliq; }

                var ad = Val(r, "ad")?.ToString();
                if (string.IsNullOrWhiteSpace(ad)) ad = $"Hesab {hesab5}";
                var cur = kontragentD.GetValueOrDefault(ad);
                kontragentD[ad] = (cur.qaliq + qaliq, cur.say + 1, cur.faizAgirliq + qaliq * faiz, cur.ehtiyat + ehtiyat, amb);

                var vad = ValyutaAd(Val(r, "valyuta")?.ToString() ?? "");
                valyutaD[vad] = valyutaD.GetValueOrDefault(vad) + qaliq;

                var mad = MuddetQrup(Val(r, "planbaglanma"), t);
                muddetD[mad] = muddetD.GetValueOrDefault(mad) + qaliq;
            }

            dto.UmumiPortfel        = Math.Round(total, 2);
            dto.Say                 = say;
            dto.OrtaFaiz            = total != 0 ? Math.Round(faizAgirliqCem / total, 2) : 0;
            dto.IllikGelir          = Math.Round(illikGelir, 2);
            dto.AmbMebleg           = Math.Round(dto.AmbMebleg, 2);
            dto.BanklararasiMebleg  = Math.Round(dto.BanklararasiMebleg, 2);
            dto.VaxtiKecmisMebleg   = Math.Round(dto.VaxtiKecmisMebleg, 2);
            dto.Ehtiyat             = Math.Round(ehtiyatCem, 2);
            dto.XalisPortfel        = Math.Round(total - ehtiyatCem, 2);
            dto.EhtiyatFaiz         = total != 0 ? Math.Round(ehtiyatCem / total * 100, 2) : 0;

            // Kontragent (bank) siyahısı — məbləğə görə, sabit rənglərlə.
            var renglar = new[] { "#0ea5e9", "#6366f1", "#8b5cf6", "#ec4899", "#f59e0b", "#10b981", "#64748b" };
            var sirali = kontragentD.OrderByDescending(x => x.Value.qaliq).ToList();
            int idx = 0;
            foreach (var k in sirali)
            {
                dto.Kontragentler.Add(new YerlesdirmeKatDto
                {
                    Ad = k.Key, Say = k.Value.say, Qaliq = Math.Round(k.Value.qaliq, 2),
                    Faiz = k.Value.qaliq != 0 ? Math.Round(k.Value.faizAgirliq / k.Value.qaliq, 2) : 0,
                    Ehtiyat = Math.Round(k.Value.ehtiyat, 2),
                    Pay = total != 0 ? Math.Round(k.Value.qaliq / total * 100, 1) : 0,
                    Reng = renglar[idx % renglar.Length]
                });
                idx++;
            }

            // Konsentrasiya: ən böyük kontragent (adətən AMB), TOP-3 pay, AMB xaric ən böyük.
            if (sirali.Count > 0)
            {
                dto.EnBoyukAd  = sirali[0].Key;
                dto.EnBoyukPay = total != 0 ? Math.Round(sirali[0].Value.qaliq / total * 100, 1) : 0;
                dto.Top3Pay    = total != 0 ? Math.Round(sirali.Take(3).Sum(x => x.Value.qaliq) / total * 100, 1) : 0;
                var enBoyukBank = sirali.FirstOrDefault(x => !x.Value.amb);
                if (enBoyukBank.Key != null)
                {
                    dto.EnBoyukBankAd     = enBoyukBank.Key;
                    dto.EnBoyukBankMebleg = Math.Round(enBoyukBank.Value.qaliq, 2);
                }
            }

            dto.ValyutaBolgusu = ToMadde(valyutaD, total);
            dto.MuddetBolgusu  = ToMaddeMuddet(muddetD, total);

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // ── IFRS 9 ECL mühərriki ────────────────────────────────────────────────
    // Bu sabit requlyativ sorğu OracleSorgular-da yox, burada const kimi saxlanılır:
    //   • sabit "mühərrik"dir (admin redaktə etməməlidir), version-control-dadır;
    //   • Azərbaycan hərfi yoxdur, amma DB-charset ə-itkisi riskindən tam qaçırıq;
    //   • yenə YALNIZ SELECT-dir və _oracle vasitəsilə işləyir (Oracle qaydası pozulmur).
    // Metodologiya = istifadəçinin Excel modeli (mode = cari il XARİC / else budağı):
    //   iller 2021..2025 (5 keçid), sahə səviyyəsində trans, floor+bərpa, AVG(M/F).
    // Performans (istifadəçi optimallaşdırması): son_tarixler tarix-aralığı ilə (indeks),
    //   snap = yalnız 5 tarixin arh_nacpogprokre sətirləri, LEADING/USE_NL hint-ləri.
    // QEYD: snap arxivdən (arh_nacpogprokre) oxuyur — keçmiş il-sonları həmişə arxivdədir.
    //   Əgər gələcəkdə cari-il-DAXİL rejimə keçilsə və hesabat günü hələ arxivə düşməyibsə,
    //   snap-i UNION ALL (arxiv + canlı) variantına keçirmək lazımdır.
    private const string Ifrs9Sql = @"
WITH iller AS (
    SELECT EXTRACT(YEAR FROM ADD_MONTHS(TRUNC(TO_DATE('{TARIX}','dd/mm/yyyy'),'YYYY'),
             -12*LEVEL)) AS il
    FROM dual CONNECT BY LEVEL <= 5
),
son_tarixler AS (
    SELECT i.il,
           (SELECT MAX(ar.date_oper)
              FROM arh_licschkre ar
             WHERE ar.date_oper >= TO_DATE(i.il || '-01-01','YYYY-MM-DD')
               AND ar.date_oper <  TO_DATE((i.il + 1) || '-01-01','YYYY-MM-DD')
               AND ar.date_oper <= TO_DATE('{TARIX}','dd/mm/yyyy')) AS son_tarix
    FROM iller i
),
snap AS (
    SELECT licschpkre, subschkre, date_oper, lastoverduedate
    FROM   arh_nacpogprokre
    WHERE  date_oper IN (SELECT son_tarix FROM son_tarixler)
),
portfel AS (
    SELECT /*+ LEADING(st ar) USE_NL(ar) INDEX(ar I_ARH_LICSCHKRE_DO) */
           st.il, ar.licschpkre, ar.subschkre,
           i.index_otrasli AS sahe_kodu, i.name_index_otrasli AS sahe_adi,
           CASE WHEN odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)) BETWEEN 0 AND 30 THEN 'Stage 1'
                WHEN odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 'Stage 2'
                ELSE 'Stage 3' END AS stage,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) AS qaliq
    FROM son_tarixler st
    JOIN arh_licschkre ar ON ar.date_oper = st.son_tarix
    JOIN snap x ON x.licschpkre = ar.licschpkre
           AND x.subschkre  = ar.subschkre
           AND x.date_oper  = st.son_tarix
    JOIN index_otrasli i ON i.index_otrasli = ar.index_otrasli
    WHERE ar.date_close IS NULL
      AND ar.date_open >= ADD_MONTHS(TRUNC(TO_DATE('{TARIX}','dd/mm/yyyy'),'YYYY'), -12*5)
),
kechid AS (
    SELECT p1.il AS il_start, p1.licschpkre, p1.subschkre, p1.sahe_kodu,
           p1.stage AS stage_start, NVL(p2.stage,'Baglanib') AS stage_next,
           p1.qaliq AS qaliq_start, NVL(p2.qaliq,0) AS qaliq_next
    FROM portfel p1
    LEFT JOIN portfel p2 ON p1.licschpkre=p2.licschpkre
                        AND p1.subschkre=p2.subschkre
                        AND p2.il=p1.il+1
),
trans AS (
    SELECT il_start, sahe_kodu, stage_start,
           SUM(qaliq_start) AS f,
           SUM(CASE WHEN stage_next='Stage 1' THEN qaliq_next ELSE 0 END) AS g,
           SUM(CASE WHEN stage_next='Stage 2' THEN qaliq_next ELSE 0 END) AS h,
           SUM(CASE WHEN stage_next='Stage 3' THEN qaliq_next ELSE 0 END) AS i_col,
           SUM(CASE WHEN stage_next='Baglanib' THEN qaliq_start ELSE 0 END) AS j,
           SUM(CASE WHEN stage_next IN ('Stage 1','Stage 2','Stage 3') THEN (qaliq_start-qaliq_next) ELSE 0 END) AS k
    FROM kechid
    GROUP BY il_start, sahe_kodu, stage_start
),
recovery AS (
    SELECT
      NVL(AVG(CASE WHEN sahe_kodu NOT IN (1902,1904) AND stage_start='Stage 3' AND f>0 THEN (j+k)/f END),0)    AS p2,
      NVL(AVG(CASE WHEN sahe_kodu     IN (1902,1904) AND stage_start='Stage 3' AND f>0 THEN (j+k)/f END),0.75) AS q2
    FROM trans
),
riskrow AS (
    SELECT t.sahe_kodu, t.stage_start, t.f,
      GREATEST(
        t.f * CASE WHEN {MENZIL_SERT} AND t.sahe_kodu IN (1902,1904) THEN {FLOOR_MENZIL}
                   WHEN t.stage_start='Stage 1' THEN {FLOOR_S1}
                   WHEN t.stage_start='Stage 2' THEN {FLOOR_S2} ELSE 0 END,
        CASE WHEN t.stage_start='Stage 3'
             THEN (t.f-t.g-t.h-t.j-t.k)*CASE WHEN {MENZIL_SERT} AND t.sahe_kodu IN (1902,1904) THEN r.q2 ELSE r.p2 END
             ELSE t.i_col*(CASE WHEN t.f=0 THEN 1 ELSE (t.f-t.g-t.h-t.j-t.k)/t.f END)*CASE WHEN {MENZIL_SERT} AND t.sahe_kodu IN (1902,1904) THEN r.q2 ELSE r.p2 END
        END
      ) AS m
    FROM trans t CROSS JOIN recovery r
),
riskfaiz AS (
    SELECT sahe_kodu, stage_start, AVG(CASE WHEN f=0 THEN 0.0001 ELSE m/f END) AS risk_faiz
    FROM riskrow GROUP BY sahe_kodu, stage_start
),
cari_snap AS (
    SELECT MAX(date_oper) AS d FROM arh_licschkre WHERE date_oper <= TO_DATE('{TARIX}','dd/mm/yyyy')
),
cari AS (
    SELECT ar.licschkre, ar.subschkre, ar.tipkredita,
           substr(ar.licschkre,6,2) AS valyuta,
           ar.index_otrasli AS sahe_kodu, io.name_index_otrasli AS sahe_adi,
           NVL(ar.procstavrez,0) AS bank_faiz,
           NVL(odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)),0) AS dpd,
           CASE WHEN NVL(odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)),0) BETWEEN 0 AND 30 THEN 'Stage 1'
                WHEN odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 'Stage 2'
                ELSE 'Stage 3' END AS stage,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) AS ead
    FROM cari_snap cs
    JOIN arh_licschkre ar ON ar.date_oper=cs.d
    LEFT JOIN view_nacpogprokre_all x ON x.licschpkre=ar.licschpkre
                                     AND x.subschkre=ar.subschkre
                                     AND x.date_oper=ar.date_oper
    JOIN index_otrasli io ON io.index_otrasli=ar.index_otrasli
    WHERE ar.date_close IS NULL AND LENGTH(ar.licschkre)=20
)
SELECT c.licschkre                         AS hesab,
       c.tipkredita                        AS tip,
       c.valyuta                           AS valyuta,
       c.sahe_kodu                         AS sahe_kodu,
       c.sahe_adi                          AS sahe_adi,
       c.stage                             AS stage,
       c.dpd                               AS dpd,
       ROUND(c.ead,2)                      AS ead,
       ROUND(NVL(rf.risk_faiz,0.0001),8)   AS risk_faiz,
       ROUND(c.ead*NVL(rf.risk_faiz,0.0001),2) AS ecl,
       ROUND(c.ead*c.bank_faiz/100,2)      AS bank_ehtiyat,
       ROUND(rec.p2,8)                     AS p2,
       ROUND(rec.q2,8)                     AS q2
FROM cari c
CROSS JOIN recovery rec
LEFT JOIN riskfaiz rf ON rf.sahe_kodu=c.sahe_kodu AND rf.stage_start=c.stage
ORDER BY c.stage, c.sahe_kodu";

    public async Task<MuhasibatIfrs9Dto> Ifrs9EclAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatIfrs9Dto { Tarix = t };

        try
        {
            var parametr = await Ifrs9ParametrleriAsync();
            dto.MenzilGuzest = parametr.MenzilGuzest;
            dto.MenzilFloor  = parametr.MenzilFloor;
            dto.Stage1Floor  = parametr.Stage1Floor;
            dto.Stage2Floor  = parametr.Stage2Floor;
            dto.Metod        = parametr.Metod;
            var sql = FloorTetbiq(MetodTetbiq(Ifrs9Sql, parametr).Replace("{TARIX}", t.ToString("dd/MM/yyyy")), parametr);
            var rows = await _oracle.SelectAsync(sql, maxRows: 20000);

            var stageD = new Dictionary<string, (int say, decimal ead, decimal ecl, decimal bank)>();
            var saheD  = new Dictionary<string, (string ad, int say, decimal ead, decimal ecl)>();
            var saheStageD = new Dictionary<(string sahe, int stage), (decimal ead, decimal ecl)>();  // sahə×mərhələ
            decimal totEad = 0m, totEcl = 0m, totBank = 0m;
            int say = 0;

            foreach (var r in rows)
            {
                var hesab    = Val(r, "hesab")?.ToString()?.Trim() ?? "";
                var tip      = (int)Dec(Val(r, "tip"));
                var valyuta  = Val(r, "valyuta")?.ToString()?.Trim() ?? "";
                var saheKodu = Val(r, "sahe_kodu")?.ToString()?.Trim() ?? "";
                var saheAdi  = Val(r, "sahe_adi")?.ToString() ?? "";
                var stage    = Val(r, "stage")?.ToString() ?? "";
                var dpd      = (int)Dec(Val(r, "dpd"));
                var ead      = Dec(Val(r, "ead"));
                var riskFaiz = Dec(Val(r, "risk_faiz"));
                var ecl      = Dec(Val(r, "ecl"));
                var bank     = Dec(Val(r, "bank_ehtiyat"));
                if (dto.P2 == 0) dto.P2 = Dec(Val(r, "p2"));
                if (dto.Q2 == 0) dto.Q2 = Dec(Val(r, "q2"));

                totEad += ead; totEcl += ecl; totBank += bank; say++;

                var s = stageD.GetValueOrDefault(stage);
                stageD[stage] = (s.say + 1, s.ead + ead, s.ecl + ecl, s.bank + bank);

                var sh = saheD.GetValueOrDefault(saheKodu);
                saheD[saheKodu] = (string.IsNullOrEmpty(sh.ad) ? saheAdi : sh.ad,
                                   sh.say + 1, sh.ead + ead, sh.ecl + ecl);

                var stageNo = stage == "Stage 1" ? 1 : stage == "Stage 2" ? 2 : 3;
                var ss = saheStageD.GetValueOrDefault((saheKodu, stageNo));
                saheStageD[(saheKodu, stageNo)] = (ss.ead + ead, ss.ecl + ecl);

                dto.Setirler.Add(new Ifrs9SetirDto
                {
                    Hesab = hesab, Tip = tip, Valyuta = valyuta, SaheKodu = saheKodu, SaheAdi = saheAdi,
                    Stage = stage, Dpd = dpd, Ead = Math.Round(ead, 2),
                    RiskFaiz = Math.Round(riskFaiz * 100m, 4), Ecl = Math.Round(ecl, 2),
                    BankEhtiyat = Math.Round(bank, 2)
                });
            }

            dto.UmumiPortfel = Math.Round(totEad, 2);
            dto.Say          = say;
            dto.UmumiEcl     = Math.Round(totEcl, 2);
            dto.BankEhtiyat  = Math.Round(totBank, 2);
            dto.EclFaiz      = totEad != 0 ? Math.Round(totEcl / totEad * 100m, 2) : 0;

            var stageReng = new Dictionary<string, string>
            {
                ["Stage 1"] = "#10b981", ["Stage 2"] = "#f59e0b", ["Stage 3"] = "#ef4444"
            };
            foreach (var stg in new[] { "Stage 1", "Stage 2", "Stage 3" })
            {
                if (!stageD.TryGetValue(stg, out var v)) continue;
                dto.Stagelar.Add(new Ifrs9StageDto
                {
                    Stage = stg, Say = v.say, Ead = Math.Round(v.ead, 2),
                    RiskFaiz = v.ead != 0 ? Math.Round(v.ecl / v.ead * 100m, 2) : 0,
                    Ecl = Math.Round(v.ecl, 2), BankEhtiyat = Math.Round(v.bank, 2),
                    Pay = totEad != 0 ? Math.Round(v.ead / totEad * 100m, 1) : 0,
                    Reng = stageReng.GetValueOrDefault(stg, "#64748b")
                });
            }

            decimal StageRisk(string sahe, int stage)
            {
                var v = saheStageD.GetValueOrDefault((sahe, stage));
                return v.ead != 0 ? Math.Round(v.ecl / v.ead * 100m, 2) : -1m;   // -1 = o mərhələdə kredit yoxdur
            }
            dto.Saheler = saheD.OrderByDescending(x => x.Value.ecl)
                .Select(x => new Ifrs9SaheDto
                {
                    Kod = x.Key, Ad = x.Value.ad, Say = x.Value.say,
                    Ead = Math.Round(x.Value.ead, 2), Ecl = Math.Round(x.Value.ecl, 2),
                    RiskFaiz = x.Value.ead != 0 ? Math.Round(x.Value.ecl / x.Value.ead * 100m, 2) : 0,
                    Pay = totEcl != 0 ? Math.Round(x.Value.ecl / totEcl * 100m, 1) : 0,
                    Risk1 = StageRisk(x.Key, 1), Risk2 = StageRisk(x.Key, 2), Risk3 = StageRisk(x.Key, 3)
                }).ToList();

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // AMB MHBS 9 — Cədvəl A1: IFRS 9 ECL sətirlərini AMB kateqoriyalarına aqreqasiya et.
    public async Task<MuhasibatAmbA1Dto> AmbA1Async(DateTime? tarix = null)
    {
        var ecl = await Ifrs9EclAsync(tarix);
        var dto = new MuhasibatAmbA1Dto { Tarix = ecl.Tarix };
        if (!ecl.Ugurlu) { dto.Ugurlu = false; dto.Xeta = ecl.Xeta; return dto; }

        static void Add(Dictionary<string, AmbHuceyre> map, string kat, string stage, decimal ead, decimal ecl_)
        {
            if (!map.TryGetValue(kat, out var h)) { h = new AmbHuceyre(); map[kat] = h; }
            if (stage == "Stage 1")      { h.G1 += ead; h.E1 += ecl_; }
            else if (stage == "Stage 2") { h.G2 += ead; h.E2 += ecl_; }
            else                         { h.G3 += ead; h.E3 += ecl_; }
        }

        static void AddDpd(Dictionary<string, AmbDpdSetir> map, string qrup, int stageNo, int dpd, decimal ead)
        {
            var key = $"{qrup}|{stageNo}";
            if (!map.TryGetValue(key, out var d)) { d = new AmbDpdSetir(); map[key] = d; }
            if (dpd <= 0)       d.Cari   += ead;
            else if (dpd <= 30) d.D1_30  += ead;
            else if (dpd <= 90) d.D31_90 += ead;
            else                d.D90    += ead;
        }

        foreach (var s in ecl.Setirler)
        {
            var kat = AmbKateqoriya(s.SaheKodu);
            Add(dto.Butun, kat, s.Stage, s.Ead, s.Ecl);

            var qrup = AmbQrup(kat);
            var stageNo = s.Stage == "Stage 1" ? 1 : s.Stage == "Stage 2" ? 2 : 3;
            AddDpd(dto.Dpd, qrup, stageNo, s.Dpd, s.Ead);

            if (!string.IsNullOrEmpty(s.Valyuta) && s.Valyuta != "00")   // xarici valyuta (AZN=00)
            {
                Add(dto.Xarici, kat, s.Stage, s.Ead, s.Ecl);
                AddDpd(dto.DpdXarici, qrup, stageNo, s.Dpd, s.Ead);
            }
        }

        dto.Ugurlu = true;
        return dto;
    }

    // AMB kateqoriya açarı → A1.2/A1.1 qrupu (biznes / istehlak / dasinmaz / diger).
    private static string AmbQrup(string kat) => kat switch
    {
        "3" => "dasinmaz",
        "4" => "diger",
        _ when kat.StartsWith("1_") => "biznes",
        _ when kat.StartsWith("2_") => "istehlak",
        _ => "diger"
    };

    // A1.1 roll-forward: dövr əvvəli (2025 sonu) və dövr sonu (hesabat tarixi) snapshot-larını
    // loan-səviyyəsində tutuşdurur. os NULL→yeni, cs NULL→bağlanmış, os≠cs→köçürmə.
    private const string Ifrs9RollForwardSql = @"
WITH acilis_snap AS (
    SELECT MAX(date_oper) d FROM arh_licschkre
    WHERE date_oper < TRUNC(TO_DATE('{TARIX}','dd/mm/yyyy'),'YYYY')
),
baglanis_snap AS (
    SELECT MAX(date_oper) d FROM arh_licschkre
    WHERE date_oper <= TO_DATE('{TARIX}','dd/mm/yyyy')
),
acilis AS (
    SELECT ar.licschpkre, ar.subschkre, ar.index_otrasli sahe,
           CASE WHEN odb.tar_ferq360(x.date_oper,NVL(x.lastoverduedate,x.date_oper)) BETWEEN 0 AND 30 THEN 1
                WHEN odb.tar_ferq360(x.date_oper,NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 2
                ELSE 3 END st,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) bal
    FROM acilis_snap cs JOIN arh_licschkre ar ON ar.date_oper=cs.d
    LEFT JOIN view_nacpogprokre_all x ON x.licschpkre=ar.licschpkre AND x.subschkre=ar.subschkre AND x.date_oper=ar.date_oper
    WHERE ar.date_close IS NULL AND LENGTH(ar.licschkre)=20
),
baglanis AS (
    SELECT ar.licschpkre, ar.subschkre, ar.index_otrasli sahe,
           CASE WHEN odb.tar_ferq360(x.date_oper,NVL(x.lastoverduedate,x.date_oper)) BETWEEN 0 AND 30 THEN 1
                WHEN odb.tar_ferq360(x.date_oper,NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 2
                ELSE 3 END st,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) bal
    FROM baglanis_snap cs JOIN arh_licschkre ar ON ar.date_oper=cs.d
    LEFT JOIN view_nacpogprokre_all x ON x.licschpkre=ar.licschpkre AND x.subschkre=ar.subschkre AND x.date_oper=ar.date_oper
    WHERE ar.date_close IS NULL AND LENGTH(ar.licschkre)=20
),
birlesme AS (
    SELECT NVL(a.sahe,b.sahe) sahe, a.st os, b.st cs, NVL(a.bal,0) obal, NVL(b.bal,0) cbal
    FROM acilis a FULL OUTER JOIN baglanis b
      ON a.licschpkre=b.licschpkre AND a.subschkre=b.subschkre
)
SELECT sahe, NVL(os,0) os, NVL(cs,0) cs, ROUND(SUM(obal),2) obal, ROUND(SUM(cbal),2) cbal,
       (SELECT d FROM acilis_snap) acilis_tarix
FROM birlesme GROUP BY sahe, os, cs";

    public async Task<MuhasibatAmbA1_1Dto> AmbA1_1Async(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        // Fallback: cari ilin əvvəli (31 dekabr). Real snapshot tarixi SQL-dən gələndə override olunur —
        // ilin son iş günü 31 dekabr olmaya bilər (məs. 30.12.2025), etiket real tarixi göstərməlidir.
        var dto = new MuhasibatAmbA1_1Dto { Tarix = t, AcilisTarix = new DateTime(t.Year, 1, 1).AddDays(-1) };

        try
        {
            var sql = Ifrs9RollForwardSql.Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 20000);

            // Açılış etiketini real snapshot tarixinə bağla (hardcode 31 dekabr yerinə).
            var realAcilis = rows.Select(r => Val(r, "acilis_tarix"))
                                 .FirstOrDefault(v => v != null && v != DBNull.Value);
            if (realAcilis != null) dto.AcilisTarix = Convert.ToDateTime(realAcilis);

            foreach (var g in new[] { "biznes", "istehlak", "dasinmaz", "diger" })
                dto.Qruplar[g] = new AmbRollForward();

            foreach (var r in rows)
            {
                var sahe = Val(r, "sahe")?.ToString()?.Trim() ?? "";
                var os = (int)Dec(Val(r, "os"));   // 0 = yeni verilmiş
                var cs = (int)Dec(Val(r, "cs"));   // 0 = tam ödənilmiş/bağlanmış
                var obal = Dec(Val(r, "obal"));
                var cbal = Dec(Val(r, "cbal"));
                var rf = dto.Qruplar[AmbQrup(AmbKateqoriya(sahe))];

                if (os == 0)                        // dövr ərzində verilmiş (yeni), bağlanış mərhələsi cs
                {
                    if (cs == 1) rf.V1 += cbal; else if (cs == 2) rf.V2 += cbal; else rf.V3 += cbal;
                }
                else                                // açılışda mövcud (os>0)
                {
                    if (os == 1) rf.A1 += obal; else if (os == 2) rf.A2 += obal; else rf.A3 += obal;

                    if (cs == 0)                    // tam ödənilmiş
                    {
                        if (os == 1) rf.O1 += obal; else if (os == 2) rf.O2 += obal; else rf.O3 += obal;
                    }
                    else                            // davam edir (cs>0) — qismən ödəniş + mümkün köçürmə
                    {
                        var odenis = obal - cbal;
                        if (os == 1) rf.O1 += odenis; else if (os == 2) rf.O2 += odenis; else rf.O3 += odenis;

                        if (os != cs)
                        {
                            if (os == 1 && cs == 2) rf.T12 += cbal;
                            else if (os == 1 && cs == 3) rf.T13 += cbal;
                            else if (os == 2 && cs == 1) rf.T21 += cbal;
                            else if (os == 2 && cs == 3) rf.T23 += cbal;
                            else if (os == 3 && cs == 1) rf.T31 += cbal;
                            else if (os == 3 && cs == 2) rf.T32 += cbal;
                        }
                    }
                }
            }

            // A2 — ECL bölməsi: dövr sonu + dövr əvvəli (mühərrik iki tarixdə), qrup×mərhələ.
            dto.EclBaglanis = await EclQrupUzreAsync(t);
            dto.EclAcilis   = await EclQrupUzreAsync(dto.AcilisTarix);

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // IFRS 9 iş kağızları — tarixi keçid matrisi (F..K, M, N) + P2/Q2. ECL mühərrikinin
    // eyni CTE-ləri, amma son nəticə əvəzinə keçid sətirlərini qaytarır (audit izi).
    private const string Ifrs9AuditSql = @"
WITH iller AS (
    SELECT EXTRACT(YEAR FROM ADD_MONTHS(TRUNC(TO_DATE('{TARIX}','dd/mm/yyyy'),'YYYY'),
             -12*LEVEL)) AS il
    FROM dual CONNECT BY LEVEL <= 5
),
son_tarixler AS (
    SELECT i.il,
           (SELECT MAX(ar.date_oper)
              FROM arh_licschkre ar
             WHERE ar.date_oper >= TO_DATE(i.il || '-01-01','YYYY-MM-DD')
               AND ar.date_oper <  TO_DATE((i.il + 1) || '-01-01','YYYY-MM-DD')
               AND ar.date_oper <= TO_DATE('{TARIX}','dd/mm/yyyy')) AS son_tarix
    FROM iller i
),
snap AS (
    SELECT licschpkre, subschkre, date_oper, lastoverduedate
    FROM   arh_nacpogprokre
    WHERE  date_oper IN (SELECT son_tarix FROM son_tarixler)
),
portfel AS (
    SELECT /*+ LEADING(st ar) USE_NL(ar) INDEX(ar I_ARH_LICSCHKRE_DO) */
           st.il, ar.licschpkre, ar.subschkre,
           i.index_otrasli AS sahe_kodu,
           CASE WHEN odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)) BETWEEN 0 AND 30 THEN 'Stage 1'
                WHEN odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate,x.date_oper)) BETWEEN 31 AND 90 THEN 'Stage 2'
                ELSE 'Stage 3' END AS stage,
           (ar.summa+ar.summa_19)*ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6) AS qaliq
    FROM son_tarixler st
    JOIN arh_licschkre ar ON ar.date_oper = st.son_tarix
    JOIN snap x ON x.licschpkre = ar.licschpkre AND x.subschkre = ar.subschkre AND x.date_oper = st.son_tarix
    JOIN index_otrasli i ON i.index_otrasli = ar.index_otrasli
    WHERE ar.date_close IS NULL
      AND ar.date_open >= ADD_MONTHS(TRUNC(TO_DATE('{TARIX}','dd/mm/yyyy'),'YYYY'), -12*5)
),
kechid AS (
    SELECT p1.il AS il_start, p1.licschpkre, p1.subschkre, p1.sahe_kodu,
           p1.stage AS stage_start, NVL(p2.stage,'Baglanib') AS stage_next,
           p1.qaliq AS qaliq_start, NVL(p2.qaliq,0) AS qaliq_next
    FROM portfel p1
    LEFT JOIN portfel p2 ON p1.licschpkre=p2.licschpkre AND p1.subschkre=p2.subschkre AND p2.il=p1.il+1
),
trans AS (
    SELECT il_start, sahe_kodu, stage_start,
           SUM(qaliq_start) AS f,
           SUM(CASE WHEN stage_next='Stage 1' THEN qaliq_next ELSE 0 END) AS g,
           SUM(CASE WHEN stage_next='Stage 2' THEN qaliq_next ELSE 0 END) AS h,
           SUM(CASE WHEN stage_next='Stage 3' THEN qaliq_next ELSE 0 END) AS i_col,
           SUM(CASE WHEN stage_next='Baglanib' THEN qaliq_start ELSE 0 END) AS j,
           SUM(CASE WHEN stage_next IN ('Stage 1','Stage 2','Stage 3') THEN (qaliq_start-qaliq_next) ELSE 0 END) AS k
    FROM kechid GROUP BY il_start, sahe_kodu, stage_start
),
recovery AS (
    SELECT
      NVL(AVG(CASE WHEN sahe_kodu NOT IN (1902,1904) AND stage_start='Stage 3' AND f>0 THEN (j+k)/f END),0)    AS p2,
      NVL(AVG(CASE WHEN sahe_kodu     IN (1902,1904) AND stage_start='Stage 3' AND f>0 THEN (j+k)/f END),0.75) AS q2
    FROM trans
),
riskrow2 AS (
    SELECT t.il_start, t.sahe_kodu, t.stage_start, t.f, t.g, t.h, t.i_col, t.j, t.k,
      GREATEST(
        t.f * CASE WHEN {MENZIL_SERT} AND t.sahe_kodu IN (1902,1904) THEN {FLOOR_MENZIL}
                   WHEN t.stage_start='Stage 1' THEN {FLOOR_S1}
                   WHEN t.stage_start='Stage 2' THEN {FLOOR_S2} ELSE 0 END,
        CASE WHEN t.stage_start='Stage 3'
             THEN (t.f-t.g-t.h-t.j-t.k)*CASE WHEN {MENZIL_SERT} AND t.sahe_kodu IN (1902,1904) THEN r.q2 ELSE r.p2 END
             ELSE t.i_col*(CASE WHEN t.f=0 THEN 1 ELSE (t.f-t.g-t.h-t.j-t.k)/t.f END)*CASE WHEN {MENZIL_SERT} AND t.sahe_kodu IN (1902,1904) THEN r.q2 ELSE r.p2 END
        END
      ) AS m,
      r.p2, r.q2
    FROM trans t CROSS JOIN recovery r
)
SELECT rr.il_start AS il, rr.sahe_kodu, io.name_index_otrasli AS sahe_adi, rr.stage_start AS stage,
       ROUND(rr.f,2) f, ROUND(rr.g,2) g, ROUND(rr.h,2) h, ROUND(rr.i_col,2) i_col, ROUND(rr.j,2) j, ROUND(rr.k,2) k,
       ROUND(rr.m,2) m, ROUND(CASE WHEN rr.f=0 THEN 0.0001 ELSE rr.m/rr.f END,8) n,
       ROUND(rr.p2,8) p2, ROUND(rr.q2,8) q2
FROM riskrow2 rr LEFT JOIN index_otrasli io ON io.index_otrasli=rr.sahe_kodu
ORDER BY rr.sahe_kodu, rr.il_start, rr.stage_start";

    public async Task<MuhasibatIfrs9AuditDto> Ifrs9IsKagizlariAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatIfrs9AuditDto { Tarix = t };

        try
        {
            var parametr = await Ifrs9ParametrleriAsync();
            var sql = FloorTetbiq(MetodTetbiq(Ifrs9AuditSql, parametr).Replace("{TARIX}", t.ToString("dd/MM/yyyy")), parametr);
            var rows = await _oracle.SelectAsync(sql, maxRows: 20000);

            foreach (var r in rows)
            {
                dto.Kechidler.Add(new Ifrs9KechidSetir
                {
                    Il = (int)Dec(Val(r, "il")),
                    SaheKodu = Val(r, "sahe_kodu")?.ToString()?.Trim() ?? "",
                    SaheAdi = Val(r, "sahe_adi")?.ToString() ?? "",
                    Stage = Val(r, "stage")?.ToString() ?? "",
                    F = Dec(Val(r, "f")), G = Dec(Val(r, "g")), H = Dec(Val(r, "h")),
                    I = Dec(Val(r, "i_col")), J = Dec(Val(r, "j")), K = Dec(Val(r, "k")),
                    M = Dec(Val(r, "m")), N = Dec(Val(r, "n"))
                });
                if (dto.P2 == 0) dto.P2 = Dec(Val(r, "p2"));
                if (dto.Q2 == 0) dto.Q2 = Dec(Val(r, "q2"));
            }

            dto.Ecl = await Ifrs9EclAsync(t);
            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // IFRS 9 ECL-i bir tarixdə hesablayıb qrup×mərhələ üzrə (E1/E2/E3-ə) yığır.
    private async Task<Dictionary<string, AmbHuceyre>> EclQrupUzreAsync(DateTime t)
    {
        var map = new Dictionary<string, AmbHuceyre>();
        foreach (var g in new[] { "biznes", "istehlak", "dasinmaz", "diger" }) map[g] = new AmbHuceyre();

        var ecl = await Ifrs9EclAsync(t);
        if (!ecl.Ugurlu) return map;

        foreach (var s in ecl.Setirler)
        {
            var h = map[AmbQrup(AmbKateqoriya(s.SaheKodu))];
            if (s.Stage == "Stage 1") h.E1 += s.Ecl;
            else if (s.Stage == "Stage 2") h.E2 += s.Ecl;
            else h.E3 += s.Ecl;
        }
        return map;
    }

    // Sahə kodu (index_otrasli) → AMB A1 kateqoriya açarı.
    // İstehlak (fiziki şəxs, 19xx) sahələri ada görə; biznes (31xx–37xx) prefiksə görə.
    // QEYD: 01904 (yaşayış təmiri, əmlakla təmin) default "2_1"-dədir; AMB "3. Daşınmaz əmlak"
    //       istənilsə bu sətri "3"-ə dəyiş (istifadəçi təsdiqi ilə).
    private static string AmbKateqoriya(string? saheKodu)
    {
        var k = (saheKodu ?? "").Trim().TrimStart('0');
        switch (k)
        {
            case "1902": return "3";     // yaşayış alın+tikinti, əmlakla təmin → daşınmaz əmlak
            case "1903": return "2_1";   // yaşayış təmiri, təminatsız
            case "1904": return "2_1";   // yaşayış təmiri, əmlakla təmin (bax: yuxarıdakı qeyd)
            case "1905": return "2_2";   // avtomobil
            case "1906": return "2_3";   // məişət avadanlıqları
            case "1907": return "2_4";   // kredit kartları
            case "1908": return "2_5";   // digər istehlak
        }
        if (k.StartsWith("19")) return "2_5";                       // digər fiziki şəxs kreditləri
        if (k.StartsWith("31")) return "1_1";                       // sənaye
        if (k.StartsWith("32")) return "1_2";                       // kənd təsərrüfatı
        if (k.StartsWith("33")) return "1_3";                       // tikinti
        if (k.StartsWith("34")) return "1_4";                       // nəqliyyat
        if (k == "35000")       return "1_5";                       // informasiya və rabitə
        if (k.StartsWith("35") || k.StartsWith("36")) return "1_6"; // ticarət
        if (k.StartsWith("37")) return "1_7";                       // digər qeyri-istehsal
        return "1_7";
    }

    // Qalıq müddət qutuları (date_planclose − hesabat tarixi). null → müddətsiz/tələbli.
    private static readonly string[] MuddetSira =
        { "≤7 gün (overnight/qısa)", "8–30 gün", "1–3 ay", "3–12 ay", "12 ay+", "Müddətsiz / tələbli" };

    private static string MuddetQrup(object? planbaglanma, DateTime t)
    {
        if (planbaglanma is not DateTime dt) return "Müddətsiz / tələbli";
        var gun = (dt.Date - t).Days;
        if (gun <= 7)   return "≤7 gün (overnight/qısa)";
        if (gun <= 30)  return "8–30 gün";
        if (gun <= 90)  return "1–3 ay";
        if (gun <= 365) return "3–12 ay";
        return "12 ay+";
    }

    private static List<BalansMaddeDto> ToMaddeMuddet(Dictionary<string, decimal> d, decimal total) =>
        MuddetSira.Where(a => d.ContainsKey(a))
                  .Select(a => new BalansMaddeDto { Ad = a, Mebleg = Math.Round(d[a], 2),
                      Faiz = total != 0 ? Math.Round(d[a] / total * 100, 1) : 0 }).ToList();

    public async Task<MuhasibatMaturityDto> MaturityAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatMaturityDto { Tarix = t };

        try
        {
            // Aktiv tərəf — kredit ödəniş qrafiki (graphpogkre), date_pog qutuları.
            var sql = (await SqlAl(AdMaturity)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 5);
            var r = rows.FirstOrDefault();

            var adlar = new[] { "0–1 ay", "1–3 ay", "3–6 ay", "6–12 ay", "1–2 il", "2 il+" };
            var esas = new decimal[6];
            var faiz = new decimal[6];
            if (r != null)
                for (int i = 0; i < 6; i++)
                {
                    esas[i] = Dec(Val(r, $"e{i + 1}"));
                    faiz[i] = Dec(Val(r, $"f{i + 1}"));
                }

            dto.EsasCem = Math.Round(esas.Sum(), 2);
            dto.FaizCem = Math.Round(faiz.Sum(), 2);
            dto.CemAxin = Math.Round(dto.EsasCem + dto.FaizCem, 2);

            decimal kum = 0m;
            for (int i = 0; i < 6; i++)
            {
                var cem = esas[i] + faiz[i];
                kum += cem;
                dto.Qutular.Add(new MaturityQutuDto
                {
                    Ad = adlar[i],
                    Esas = Math.Round(esas[i], 2),
                    Faiz = Math.Round(faiz[i], 2),
                    Cem = Math.Round(cem, 2),
                    Kumulyativ = Math.Round(kum, 2),
                    Faiz_Pay = dto.CemAxin != 0 ? Math.Round(cem / dto.CemAxin * 100, 1) : 0
                });
            }
            dto.Axin1Ay  = dto.Qutular[0].Cem;
            dto.Axin3Ay  = dto.Qutular[1].Kumulyativ;
            dto.Axin12Ay = dto.Qutular[3].Kumulyativ;

            // Kontekst — tələbli depozit bazası + HQLA (likvid tampon).
            var csql = (await SqlAl(AdMaturityKontekst)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var crows = await _oracle.SelectAsync(csql, maxRows: 5);
            var cr = crows.FirstOrDefault();
            if (cr != null)
            {
                dto.TelebliDepozit = Math.Round(Dec(Val(cr, "depozit")), 2);
                dto.Hqla           = Math.Round(Dec(Val(cr, "hqla")), 2);
            }

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    public async Task<MuhasibatLikvidlikDto> LikvidlikAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatLikvidlikDto { Tarix = t };

        try
        {
            var sql = (await SqlAl(AdBalans)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 200000);

            var likvidD = new Dictionary<string, decimal>();
            var valyutaD = new Dictionary<string, decimal>();
            decimal likvid = 0m, aktiv = 0m, ohdelik = 0m, level2Kesir = 0m;
            decimal fizikiDep = 0m, huquqiDep = 0m;

            foreach (var r in rows)
            {
                var hesab = Val(r, "hesab")?.ToString() ?? "";
                var valKod = Val(r, "valyuta")?.ToString() ?? "";
                var qaliq = Dec(Val(r, "qaliq"));
                if (qaliq == 0m) continue;

                var depTip = Val(r, "dep_tip")?.ToString() ?? "X";
                if (depTip == "F") fizikiDep += -qaliq;
                else if (depTip == "H") huquqiDep += -qaliq;

                var (kat, _) = Tesnif(hesab);
                if (kat == "aktiv") aktiv += qaliq;
                else if (kat == "ohdelik") ohdelik += -qaliq;

                var lq = LikvidQrup(hesab);
                if (lq != null)
                {
                    likvid += qaliq;
                    if (lq == "Cari likvid vəsaitlər")
                    {
                        // Level 2 haircut — valyutaya görə: IRR yalnız 50% sayılır (50% kəsir),
                        // qalan valyutalar 75% sayılır (25% kəsir).
                        var kesir = ValyutaAd(valKod) == "IRR" ? 0.50m : 0.25m;
                        level2Kesir += qaliq * kesir;
                    }
                    likvidD[lq] = likvidD.GetValueOrDefault(lq) + qaliq;
                    var vad = ValyutaAd(valKod);
                    valyutaD[vad] = valyutaD.GetValueOrDefault(vad) + qaliq;
                }
            }

            dto.LikvidAktiv    = Math.Round(likvid, 2);
            dto.UmumiOhdelik   = Math.Round(ohdelik, 2);
            dto.AniLikvidlik   = ohdelik != 0 ? Math.Round(likvid / ohdelik * 100, 1) : 0;
            dto.LikvidAktivPay = aktiv != 0 ? Math.Round(likvid / aktiv * 100, 1) : 0;
            dto.LikvidStruktur = ToMadde(likvidD, likvid);
            dto.ValyutaBolgusu = ToMadde(valyutaD, likvid);

            // Təxmini LCR — Level 2 haircut valyutaya görə (IRR 50%, digər 25%); net outflow = fiziki×10% + hüquqi×40%
            var hqla = likvid - level2Kesir;
            var netMex = fizikiDep * 0.10m + huquqiDep * 0.40m;
            dto.Hqla          = Math.Round(hqla, 2);
            dto.FizikiDepozit = Math.Round(fizikiDep, 2);
            dto.HuquqiDepozit = Math.Round(huquqiDep, 2);
            dto.XalisMexaric  = Math.Round(netMex, 2);
            dto.Lcr           = netMex != 0 ? Math.Round(hqla / netMex * 100, 1) : 0;

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // Likvid aktiv qrupu (yoxsa null). Prefikslər LCR/frm_Dep-dən.
    private static string? LikvidQrup(string hesab)
    {
        if (hesab.Length < 3) return null;
        if (hesab.Substring(0, 3) == "100") return "Kassa (nağd)";
        if (hesab.Length < 5) return null;
        var p5 = hesab.Substring(0, 5);
        if (p5 is "11010" or "11020" or "11110" or "11710") return "AMB / müxbir (NOSTRO)";
        if (p5 is "14010" or "14012" or "14014" or "14030" or "14032" or "14034") return "Qiymətli kağızlar";
        if (p5 is "15020" or "15025") return "Cari likvid vəsaitlər";
        if (p5 == "15770") return "Yüksək likvid aktivlər (HQLA)";
        return null;
    }

    public async Task<MuhasibatValyutaDto> ValyutaAsync(DateTime? bas = null, DateTime? son = null)
    {
        var s = (son ?? DateTime.Now.Date).Date;
        var b = (bas ?? s.AddDays(-30)).Date;
        var dto = new MuhasibatValyutaDto { BasTarix = b, SonTarix = s };

        try
        {
            var sql = (await SqlAl(AdValyuta))
                .Replace("{BAS}", b.ToString("dd/MM/yyyy"))
                .Replace("{SON}", s.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 500000);

            // valyuta → cəmlər
            var alisH = new Dictionary<string, decimal>();
            var alisA = new Dictionary<string, decimal>();
            var satisH = new Dictionary<string, decimal>();
            var satisA = new Dictionary<string, decimal>();
            int sayA = 0, sayS = 0;

            foreach (var r in rows)
            {
                var yon = Val(r, "yon")?.ToString() ?? "";
                var val = ValyutaAd(Val(r, "val")?.ToString() ?? "");
                var hecm = Dec(Val(r, "hecm"));
                var azn = Dec(Val(r, "azn"));

                if (yon == "alis")
                {
                    alisH[val] = alisH.GetValueOrDefault(val) + hecm;
                    alisA[val] = alisA.GetValueOrDefault(val) + azn;
                    sayA++;
                }
                else
                {
                    satisH[val] = satisH.GetValueOrDefault(val) + hecm;
                    satisA[val] = satisA.GetValueOrDefault(val) + azn;
                    sayS++;
                }
            }

            var valyutalar = alisH.Keys.Union(satisH.Keys).Distinct();
            foreach (var v in valyutalar)
            {
                var ah = alisH.GetValueOrDefault(v);
                var aa = alisA.GetValueOrDefault(v);
                var sh = satisH.GetValueOrDefault(v);
                var sa = satisA.GetValueOrDefault(v);
                var ortaA = ah != 0 ? aa / ah : 0;
                var ortaS = sh != 0 ? sa / sh : 0;
                dto.Setirler.Add(new ValyutaSetirDto
                {
                    Valyuta = v,
                    AlisHecm = Math.Round(ah, 2), AlisAzn = Math.Round(aa, 2),
                    SatisHecm = Math.Round(sh, 2), SatisAzn = Math.Round(sa, 2),
                    OrtaAlisKurs = Math.Round(ortaA, 4), OrtaSatisKurs = Math.Round(ortaS, 4),
                    Spred = Math.Round(ortaS - ortaA, 4),
                    AcigMovqe = Math.Round(ah - sh, 2)
                });
            }
            dto.Setirler = dto.Setirler.Where(x => x.AlisAzn != 0 || x.SatisAzn != 0)
                                       .OrderByDescending(x => x.AlisAzn + x.SatisAzn).ToList();

            dto.AlisAzn = Math.Round(alisA.Values.Sum(), 2);
            dto.SatisAzn = Math.Round(satisA.Values.Sum(), 2);
            dto.Xalis = Math.Round(dto.SatisAzn - dto.AlisAzn, 2);
            dto.EmeliyyatSayi = sayA + sayS;
            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    public async Task<MuhasibatRezidentDto> RezidentAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatRezidentDto { Tarix = t };

        try
        {
            var sql = (await SqlAl(AdRezident)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 10);
            foreach (var r in rows)
            {
                var tip = Val(r, "tip")?.ToString() ?? "";
                var meb = Dec(Val(r, "mebleg"));
                var say = (int)Dec(Val(r, "say"));
                if (tip == "qr") { dto.QeyriRezident = Math.Round(meb, 2); dto.QeyriRezidentSay = say; }
                else { dto.Rezident = Math.Round(meb, 2); dto.RezidentSay = say; }
            }
            dto.Umumi = Math.Round(dto.Rezident + dto.QeyriRezident, 2);
            dto.QeyriRezidentPay = dto.Umumi != 0 ? Math.Round(dto.QeyriRezident / dto.Umumi * 100, 2) : 0;
            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    // ── Drill-down ──────────────────────────────────────────────────────────
    // Aqreqasiya ilə EYNİ sorğunu işlədir və EYNİ helper-lərlə (Tesnif/LikvidQrup/
    // dep_tip/TipAd...) təsnif edir → detal cəmi kartla üst-üstə düşür.
    public async Task<MuhasibatDetalDto> DetalAsync(string sahe, string madde,
        DateTime? tarix = null, DateTime? bas = null, DateTime? son = null)
    {
        var s0 = (sahe ?? "").ToLowerInvariant();
        madde ??= "";
        var dto = new MuhasibatDetalDto { Sahe = s0, Madde = madde, Baslik = madde };
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;

        try
        {
            switch (s0)
            {
                case "balans":
                case "balans-valyuta":
                case "balans-menfeet":
                case "likvidlik":
                {
                    var sql = (await SqlAl(AdBalans)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 200000);
                    foreach (var r in rows)
                    {
                        var hesab = Val(r, "hesab")?.ToString() ?? "";
                        var ad = Val(r, "ad")?.ToString() ?? "";
                        var valKod = Val(r, "valyuta")?.ToString() ?? "";
                        var qaliq = Dec(Val(r, "qaliq"));
                        if (qaliq == 0m) continue;
                        var depTip = Val(r, "dep_tip")?.ToString() ?? "X";
                        // Öz valyutası: saldo_ish_inval AZN hesablarda 0-dır (yalnız xarici valyutada dolu),
                        // ona görə AZN-də manat qalığını göstəririk. Digər valyutalarda inval-ı disp-in
                        // işarəsi ilə uyğunlaşdırıb veririk (disp = ±qaliq → ±qaliq_inval).
                        var qaliqInval = Dec(Val(r, "qaliq_inval"));
                        decimal? InvalDisp(decimal disp) =>
                            ValyutaAd(valKod) == "AZN" ? disp
                            : (qaliq == 0m ? (decimal?)null : disp / qaliq * qaliqInval);

                        if (s0 == "balans-menfeet")
                        {
                            if (!hesab.StartsWith("50130")) continue;
                            dto.Setirler.Add(DSetir(hesab, ad, ValyutaAd(valKod), -qaliq, InvalDisp(-qaliq)));
                            continue;
                        }
                        if (s0 == "likvidlik")
                        {
                            var lq = LikvidQrup(hesab);
                            if (lq == null) continue;
                            if (madde != "*" && lq != madde) continue;   // "*" → bütün likvid aktivlər
                            var setir = DSetir(hesab, ad, ValyutaAd(valKod), qaliq, InvalDisp(qaliq));
                            if (lq == "Cari likvid vəsaitlər")
                            {
                                // LCR haircut: IRR yalnız 50% sayılır, qalan valyutalar 75%.
                                // Sonuncu sütun rəqəmdir (sayılan məbləğ) — başlıqda qısa qeyd,
                                // CƏMİ də sayılan məbləğə görə hesablanır.
                                var ceki = ValyutaAd(valKod) == "IRR" ? 0.50m : 0.75m;
                                setir.ElaveMebleg = Math.Round(qaliq * ceki, 2);
                                dto.ElaveReqem = true;
                                dto.ElaveBaslik = "Sayılan";
                            }
                            dto.Setirler.Add(setir);
                            continue;
                        }
                        if (s0 == "balans-valyuta")
                        {
                            if (depTip is "H" or "F") continue;      // valyuta bölgüsü yalnız aktivlərdən
                            var (kat0, _) = Tesnif(hesab, ad);
                            if (kat0 != "aktiv") continue;
                            if (ValyutaAd(valKod) != madde) continue;
                            dto.Setirler.Add(DSetir(hesab, ad, ValyutaAd(valKod), qaliq, InvalDisp(qaliq)));
                            continue;
                        }

                        // s0 == "balans": bucket etiketi BalansAsync ilə eyni.
                        // madde "*aktiv"/"*ohdelik"/"*kapital" → total kartı (bütün kat hesabları).
                        string kat2, bucket; decimal disp;
                        if (depTip == "H") { kat2 = "ohdelik"; bucket = "Hüquqi şəxs depozitləri"; disp = -qaliq; }
                        else if (depTip == "F") { kat2 = "ohdelik"; bucket = "Fiziki şəxs depozitləri"; disp = -qaliq; }
                        else
                        {
                            // Faiz artıq ayrıca sətir deyil — Tesnif-dən "Müştərilərə kreditlər" qalır.
                            var (kat, qrup) = Tesnif(hesab, ad);
                            kat2 = kat;
                            if (kat == "aktiv") { bucket = qrup; disp = qaliq; }
                            else if (kat == "ohdelik") { bucket = qrup; disp = -qaliq; }
                            else if (kat == "kapital") { bucket = "Kapital və ehtiyatlar"; disp = -qaliq; }
                            else { bucket = "Təsnif edilməmiş"; disp = qaliq; }
                        }
                        bool match = madde.StartsWith("*") ? ("*" + kat2 == madde) : (bucket == madde);
                        if (!match) continue;
                        dto.Setirler.Add(DSetir(hesab, ad, ValyutaAd(valKod), disp, InvalDisp(disp)));
                    }
                    break;
                }

                case "depozit":
                {
                    // madde: "tip:huquqi" | "tip:fiziki" | "valyuta:<VAL>" | "musteri:<tip>:<qeyd>"
                    var sql = (await SqlAl(AdDepozit)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 300000);
                    var parts = (madde ?? "").Split(':');
                    var mod = parts.Length > 0 ? parts[0] : "";

                    // Öz valyutası: AZN-də manat qalığı (inval 0-dır), digər valyutada qaliq_inval.
                    if (mod == "musteri")
                    {
                        var mtip = parts.Length > 1 ? parts[1] : "";
                        var mqeyd = parts.Length > 2 ? parts[2] : "";
                        var accs = new List<(string hesab, string ad, string vk, decimal q, decimal? inval)>();
                        foreach (var r in rows)
                        {
                            if ((Val(r, "tip")?.ToString() ?? "") != mtip) continue;
                            if ((Val(r, "qeyd")?.ToString() ?? "") != mqeyd) continue;
                            var q = Dec(Val(r, "qaliq"));
                            if (q == 0m) continue;
                            var ad = Val(r, "musteri")?.ToString() ?? "(adsız)";
                            var vk = Val(r, "valyuta")?.ToString() ?? "";
                            var hesab = Val(r, "hesab")?.ToString() ?? mqeyd;
                            decimal? inval = ValyutaAd(vk) == "AZN" ? q : Dec(Val(r, "qaliq_inval"));
                            accs.Add((hesab, ad, vk, q, inval));
                        }
                        // Hesab koduna görə sıralı
                        foreach (var a in accs.OrderBy(a => a.hesab, StringComparer.Ordinal))
                            dto.Setirler.Add(DSetir(a.hesab, a.ad, ValyutaAd(a.vk), a.q, a.inval));
                        dto.Baslik = "Depozitor hesabları";
                    }
                    else
                    {
                        var arg = parts.Length > 1 ? parts[1] : "";
                        // Müştəri + VALYUTA üzrə aqreqasiya — hər valyuta AYRICA sətir (qarışıq yoxdur),
                        // öz valyutası həmişə dolur.
                        var qruplar = new Dictionary<string, (string qeyd, string ad, string tip, string val, decimal meb, decimal inval)>();
                        foreach (var r in rows)
                        {
                            var tip = Val(r, "tip")?.ToString() ?? "";
                            var qeyd = Val(r, "qeyd")?.ToString() ?? "";
                            var vk = Val(r, "valyuta")?.ToString() ?? "";
                            var vad = ValyutaAd(vk);
                            var q = Dec(Val(r, "qaliq"));
                            if (q == 0m) continue;
                            if (mod == "tip" && tip != arg) continue;
                            if (mod == "valyuta" && vad != arg) continue;
                            var ad = Val(r, "musteri")?.ToString() ?? "(adsız)";
                            var qInval = vad == "AZN" ? q : Dec(Val(r, "qaliq_inval"));
                            var key = tip + "|" + qeyd + "|" + vad;
                            if (qruplar.TryGetValue(key, out var cur))
                                qruplar[key] = (cur.qeyd, cur.ad, cur.tip, cur.val, cur.meb + q, cur.inval + qInval);
                            else
                                qruplar[key] = (qeyd, ad, tip, vad, q, qInval);
                        }
                        foreach (var g in qruplar.Values
                                     .OrderBy(x => x.qeyd, StringComparer.Ordinal).ThenBy(x => x.val, StringComparer.Ordinal))
                        {
                            dto.Setirler.Add(new MuhasibatDetalSetirDto
                            {
                                Kod = g.qeyd, Ad = g.ad, Valyuta = g.val,
                                Mebleg = Math.Round(g.meb, 2),
                                MeblegInval = Math.Round(g.inval, 2),
                                Elave = g.tip == "fiziki" ? "fiziki"
                                      : g.tip == "sahibkar" ? "sahibkar" : "hüquqi"
                            });
                        }
                    }
                    break;
                }

                case "kredit":
                {
                    // madde: "tip:<Ad>" | "teyinat:<Ad>" | "valyuta:<VAL>" | "age:<Ad>"
                    var sql = (await SqlAl(AdKredit)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 300000);
                    var parts = (madde ?? "").Split(':', 2);
                    var mod = parts.Length > 0 ? parts[0] : "";
                    var mval = parts.Length > 1 ? parts[1] : "";
                    foreach (var r in rows)
                    {
                        var kurs = Dec(Val(r, "kurs"));
                        var esas = Dec(Val(r, "esas"));
                        var vk = Dec(Val(r, "vk"));
                        var gec = (int)Dec(Val(r, "gec_gun"));
                        var qaliq = (esas + vk) * kurs;
                        // Açıq müqavilə qalığı 0 olsa belə görünür — say drill-down ilə tutuşsun.
                        bool uygun = mod switch
                        {
                            "tip"      => TipAd((int)Dec(Val(r, "tip"))) == mval,
                            "teyinat"  => (Val(r, "teyinat")?.ToString() ?? "(təyinatsız)") == mval,
                            "valyuta"  => ValyutaAd(Val(r, "valyuta")?.ToString() ?? "") == mval,
                            "age"      => AgeAd(gec) == mval,
                            _          => false
                        };
                        if (!uygun) continue;
                        var muq = Val(r, "muqavile")?.ToString() ?? "";
                        // Müştəri adı — regnom.name_regnom (substr(licschkre,10,6)=regnom).
                        // Ad boşdursa (regnom tapılmasa) müştəri tipinə düş ki, sətir adsız qalmasın.
                        var musteri = Val(r, "musteri")?.ToString()?.Trim() ?? "";
                        var tipAdi  = TipAd((int)Dec(Val(r, "tip")));
                        dto.Setirler.Add(new MuhasibatDetalSetirDto
                        {
                            Kod = muq,
                            Ad = string.IsNullOrWhiteSpace(musteri) ? tipAdi : musteri,
                            Valyuta = ValyutaAd(Val(r, "valyuta")?.ToString() ?? ""),
                            Mebleg = Math.Round(qaliq, 2),
                            Elave = gec > 0 ? $"{tipAdi} · DPD {gec}" : tipAdi
                        });
                    }
                    dto.Setirler = dto.Setirler.OrderByDescending(x => x.Mebleg).ToList();
                    break;
                }

                case "valyuta":
                {
                    // madde: "val:<VAL>" | "val:<VAL>:alis" | "val:<VAL>:satis"
                    var sv = (son ?? DateTime.Now.Date).Date;
                    var bv = (bas ?? sv.AddDays(-30)).Date;
                    var sql = (await SqlAl(AdValyuta))
                        .Replace("{BAS}", bv.ToString("dd/MM/yyyy"))
                        .Replace("{SON}", sv.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 500000);
                    var parts = (madde ?? "").Split(':');
                    var mval = parts.Length > 1 ? parts[1] : "";
                    var myon = parts.Length > 2 ? parts[2] : "";
                    foreach (var r in rows)
                    {
                        var yon = Val(r, "yon")?.ToString() ?? "";
                        var val = ValyutaAd(Val(r, "val")?.ToString() ?? "");
                        if (val != mval) continue;
                        if (!string.IsNullOrEmpty(myon) && yon != myon) continue;
                        var hecm = Dec(Val(r, "hecm"));
                        var azn = Dec(Val(r, "azn"));
                        var tarixStr = Val(r, "tarix") is DateTime dtv ? dtv.ToString("dd.MM.yyyy") : "";
                        dto.Setirler.Add(new MuhasibatDetalSetirDto
                        {
                            Kod = tarixStr, Ad = yon == "alis" ? "Alış" : "Satış", Valyuta = val,
                            Mebleg = Math.Round(azn, 2),
                            Elave = $"{Math.Round(hecm, 2)} {val}"
                        });
                    }
                    break;
                }

                case "elaqeli":
                {
                    // əlaqəli tərəf — hesab-səviyyə (aqreqat "elaqeli" ilə eyni filtr).
                    var sql = (await SqlAl(AdElaqeliDetal)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 200000);
                    foreach (var r in rows)
                    {
                        var meb = Dec(Val(r, "mebleg"));
                        if (meb == 0m) continue;
                        // Əlaqəli tərəf adı SQL-dən gəlir (prefiks C#-da — Oracle SQL mətnində
                        // ə hərfi client charset-də "?"-ə çevrilir, ona görə literal burada verilir).
                        var rel = Val(r, "elave")?.ToString();
                        dto.Setirler.Add(new MuhasibatDetalSetirDto
                        {
                            Kod = Val(r, "hesab")?.ToString() ?? "",
                            Ad = Val(r, "ad")?.ToString() ?? "",
                            Valyuta = ValyutaAd(Val(r, "valyuta")?.ToString() ?? ""),
                            Mebleg = Math.Round(meb, 2),
                            Elave = string.IsNullOrWhiteSpace(rel) ? "—" : "Əlaqəli: " + rel
                        });
                    }
                    dto.Baslik = "Əlaqəli tərəf hesabları";
                    break;   // SQL-dəki qrup sıralaması saxlanılır (abs re-sort yox)
                }

                case "rezident":
                {
                    // per-account rezident detalı — ayrıca stored sorğu (eyni case məntiqi).
                    var sql = (await SqlAl(AdRezidentDetal)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 200000);
                    foreach (var r in rows)
                    {
                        if ((Val(r, "tip")?.ToString() ?? "") != madde) continue;
                        var meb = Dec(Val(r, "mebleg"));
                        if (meb == 0m) continue;
                        dto.Setirler.Add(DSetir(Val(r, "hesab")?.ToString() ?? "",
                            Val(r, "ad")?.ToString() ?? "", null, Math.Round(meb, 2)));
                    }
                    break;
                }

                case "menfeet":
                {
                    // P&L drill-down — madde = kateqoriya adı (məs. "Kreditlər üzrə faiz").
                    // Faiz alt-kateqoriyaları (60/61/63-64/65) ayrıca drill olunur.
                    // Hesab-səviyyə dövriyyə, aqreqat ilə EYNİ MenfeetTesnif təsnifatı.
                    var bb = (bas ?? new DateTime((son ?? t).Year, 1, 1)).Date;
                    var ss = (son ?? t).Date;
                    var sql = (await SqlAl(AdMenfeetDetal))
                        .Replace("{BAS}", bb.ToString("dd/MM/yyyy"))
                        .Replace("{SON}", ss.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 20000);
                    var kursNet   = madde == "Kurs fərqi";     // 66 gəlir − 86 xərc
                    var dilinqNet = madde == "Dilinq fərqi";   // 68 gəlir − 88 xərc
                    foreach (var r in rows)
                    {
                        var sinif2 = Val(r, "sinif2")?.ToString() ?? "";
                        var (kat, gelir) = MenfeetTesnif(sinif2);
                        decimal meb;
                        if (kursNet)
                        {
                            // net kateqoriya: gəlir tərəf (66) +kredit, zərər tərəf (86) −debet.
                            if (kat == "Kurs fərqi gəliri") meb = Dec(Val(r, "kredit"));
                            else if (kat == "Kurs fərqi zərəri") meb = -Dec(Val(r, "debet"));
                            else continue;
                        }
                        else if (dilinqNet)
                        {
                            // net kateqoriya: gəlir tərəf (68) +kredit, zərər tərəf (88) −debet.
                            if (kat == "Dilinq fərqi gəliri") meb = Dec(Val(r, "kredit"));
                            else if (kat == "Dilinq fərqi zərəri") meb = -Dec(Val(r, "debet"));
                            else continue;
                        }
                        else
                        {
                            if (kat != madde) continue;
                            meb = gelir ? Dec(Val(r, "kredit")) : Dec(Val(r, "debet"));
                        }
                        if (meb == 0m) continue;
                        dto.Setirler.Add(DSetir(Val(r, "hesab")?.ToString() ?? "",
                            Val(r, "ad")?.ToString() ?? "", null, Math.Round(meb, 2)));
                    }
                    dto.Setirler = dto.Setirler.OrderByDescending(x => x.Mebleg).ToList();
                    break;
                }

                case "kredit-keyfiyyet":
                {
                    // madde = təsnifat kateqoriyası (Standart/Şübhəli/...) və ya girovlu/girovsuz/restrukt.
                    var adlar = new Dictionary<int, string>
                    {
                        [1] = "Standart", [2] = "Nəzarət altında", [3] = "Qeyri-standart",
                        [4] = "Şübhəli", [5] = "Ümidsiz (zərər)"
                    };
                    var kateqoriyaMi = adlar.Values.Contains(madde);
                    var sql = (await SqlAl(AdKeyfiyyetDetal)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 100000);
                    foreach (var r in rows)
                    {
                        // Açıq kredit (date_close null) qalığı 0 olsa belə görünür.
                        var qaliq = Dec(Val(r, "qaliq"));
                        var rez = Dec(Val(r, "rez"));
                        var restrukt = (int)Dec(Val(r, "restrukt"));
                        var kod = (int)Dec(Val(r, "kod"));
                        var girov = Dec(Val(r, "girov"));
                        var girovnov = Val(r, "ad")?.ToString();
                        if (string.IsNullOrWhiteSpace(girovnov)) girovnov = kod == 8 ? "Girovsuz" : $"Digər (#{kod})";
                        string? elave;
                        if (kateqoriyaMi)
                        {
                            if (adlar[KeyfiyyetKat(rez)] != madde) continue;
                            elave = $"Ehtiyat: {rez:0}%";
                        }
                        else if (madde == "restrukt")
                        {
                            if (restrukt != 1) continue;
                            elave = $"Ehtiyat: {rez:0}%";
                        }
                        else
                        {
                            // Girov növü üzrə.
                            if (girovnov != madde) continue;
                            var girovStr = girov.ToString("#,##0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
                            var ltv = girov > 0m ? $" · LTV {Math.Round(qaliq / girov * 100, 0):0}%" : "";
                            elave = girov > 0m ? $"Girov: {girovStr} ₼{ltv}" : "—";
                        }
                        var setir = DSetir(Val(r, "muqavile")?.ToString() ?? "",
                            "Tip " + (Val(r, "tip")?.ToString() ?? ""), null, Math.Round(qaliq, 2));
                        setir.Elave = elave;
                        dto.Setirler.Add(setir);
                    }
                    dto.Setirler = dto.Setirler.OrderByDescending(x => x.Mebleg).ToList();
                    dto.ElaveBaslik = kateqoriyaMi || madde == "restrukt" ? "Ehtiyat dərəcəsi" : "Girov / LTV";
                    break;
                }

                case "yerlesdirme":
                {
                    // madde: "kontragent:<Ad>" | "valyuta:<VAL>" | "muddet:<qutu>" | "amb" | "banklararasi" | "vaxti-kecmis"
                    var sql = (await SqlAl(AdYerlesdirme)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 5000);
                    var parts = (madde ?? "").Split(':', 2);
                    var mod = parts.Length > 0 ? parts[0] : "";
                    var mval = parts.Length > 1 ? parts[1] : "";
                    foreach (var r in rows)
                    {
                        var hesab5 = Val(r, "hesab5")?.ToString() ?? "";
                        var kurs = Dec(Val(r, "kurs"));
                        var esas = Dec(Val(r, "esas"));
                        var faiz = Dec(Val(r, "faiz"));
                        var ehtiyatFaiz = Dec(Val(r, "ehtiyat_faiz"));
                        var qaliq = esas * kurs;
                        var ad = Val(r, "ad")?.ToString();
                        if (string.IsNullOrWhiteSpace(ad)) ad = $"Hesab {hesab5}";
                        var vad = ValyutaAd(Val(r, "valyuta")?.ToString() ?? "");
                        var mad = MuddetQrup(Val(r, "planbaglanma"), t);
                        var vaxtiKecmis = Val(r, "planbaglanma") is DateTime pbd && pbd.Date < t;
                        // Açıq yerləşdirmə qalığı 0 olsa belə görünür (say drill-down ilə tutuşsun).
                        bool uygun = mod switch
                        {
                            "kontragent"   => ad == mval,
                            "valyuta"      => vad == mval,
                            "muddet"       => mad == mval,
                            "amb"          => hesab5.StartsWith("11"),
                            "banklararasi" => !hesab5.StartsWith("11"),
                            "vaxti-kecmis" => vaxtiKecmis,
                            _              => false
                        };
                        if (!uygun) continue;
                        var muq = Val(r, "muqavile")?.ToString() ?? "";
                        // öz valyutası = esas (öz valyutasında qalıq); AZN-də manata bərabər.
                        var setir = DSetir(muq, ad, vad, Math.Round(qaliq, 2), Math.Round(esas, 2));
                        var pb = Val(r, "planbaglanma") is DateTime dtp ? dtp.ToString("dd.MM.yyyy") : "müddətsiz";
                        // Kompakt: faiz · ehtiyat (varsa) · plan/⚠tarix.
                        var bits = new List<string> { $"{faiz:0.##}%" };
                        if (ehtiyatFaiz > 0) bits.Add($"ehtiyat {ehtiyatFaiz:0.##}%");
                        bits.Add(vaxtiKecmis ? $"⚠ {pb}" : pb);
                        setir.Elave = string.Join(" · ", bits);
                        dto.Setirler.Add(setir);
                    }
                    dto.Setirler = dto.Setirler.OrderByDescending(x => x.Mebleg).ToList();
                    dto.ElaveBaslik = "Faiz · ehtiyat · müddət";
                    break;
                }

                default:
                    throw new InvalidOperationException($"Naməlum sahə: {sahe}");
            }

            // "elaqeli" SQL-də qrupla sıralanır — burada re-sort etmə.
            if (s0 is "balans" or "balans-valyuta" or "balans-menfeet" or "likvidlik" or "rezident")
                dto.Setirler = dto.Setirler.OrderByDescending(x => Math.Abs(x.Mebleg)).ToList();

            // Sonuncu sütun rəqəm olanda (məs. likvidlik "Cari likvid vəsaitlər" —
            // valyutaya görə sayılan/haircut) CƏMİ də həmin yanaşmaya görə sayılır.
            dto.Cem = dto.ElaveReqem
                ? Math.Round(dto.Setirler.Sum(x => x.ElaveMebleg ?? x.Mebleg), 2)
                : Math.Round(dto.Setirler.Sum(x => x.Mebleg), 2);
            dto.Say = dto.Setirler.Count;
            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    private static MuhasibatDetalSetirDto DSetir(string kod, string ad, string? val, decimal meb, decimal? invalMeb = null) =>
        new()
        {
            Kod = kod, Ad = ad, Valyuta = val,
            Mebleg = Math.Round(meb, 2),
            MeblegInval = invalMeb.HasValue ? Math.Round(invalMeb.Value, 2) : (decimal?)null
        };

    // Balans strukturunun kanonik ardıcıllığı (məbləğə görə yox — mühasibat sırası).
    private static readonly string[] AktivSira =
    {
        "Kassa (nağd vəsaitlər)",
        "AMB (Mərkəzi Bank)",
        "Müxbir hesablar",
        "Qiymətli kağızlar",
        "Digər yerləşdirmələr / likvid aktivlər",
        "Müştərilərə kreditlər",
        "Digər ehtiyyat",
        "Hesablanmış faizlər və digər aktivlər",
        "Əsas vəsaitlər və qeyri-maddi aktivlər",
        "Digər aktivlər",
    };
    private static readonly string[] OhdelikSira =
    {
        "Hüquqi şəxs depozitləri",
        "Fiziki şəxs depozitləri",
        "Bank və maliyyə öhdəlikləri",
        "Digər öhdəliklər",
    };
    private static int SiraNo(string[] order, string ad)
    {
        var i = Array.IndexOf(order, ad);
        return i < 0 ? int.MaxValue : i;
    }

    private static List<BalansMaddeDto> ToMadde(Dictionary<string, decimal> d, decimal total) =>
        d.Where(x => Math.Abs(x.Value) > 0.005m).OrderByDescending(x => x.Value)
         .Select(x => new BalansMaddeDto { Ad = x.Key, Mebleg = Math.Round(x.Value, 2),
             Faiz = total != 0 ? Math.Round(x.Value / total * 100, 1) : 0 }).ToList();

    // Gecikmə (aging) — məntiqi sıra ilə (cari → 90+).
    private static readonly string[] AgeSira = { "Cari (0 gün)", "1–30 gün", "31–90 gün", "90+ gün (NPL)" };
    private static List<BalansMaddeDto> ToMaddeSira(Dictionary<string, decimal> d, decimal total) =>
        AgeSira.Where(a => d.ContainsKey(a))
               .Select(a => new BalansMaddeDto { Ad = a, Mebleg = Math.Round(d[a], 2),
                   Faiz = total != 0 ? Math.Round(d[a] / total * 100, 1) : 0 }).ToList();

    private static string TipAd(int kod) => kod switch
    {
        1 => "Hüquqi şəxs",
        2 => "Fiziki şəxs",
        3 => "Sahibkar",
        _ => "Digər"
    };

    private static string TeyinatAd(int kod) => kod switch
    {
        1901 or 1902 => "İpoteka / daşınmaz",
        1903 or 1904 => "Təmir",
        1905 => "Avtomobil",
        1906 => "Məişət",
        1907 => "Kart krediti",
        _ => "Digər təyinat"
    };

    private static string AgeAd(int gec) =>
        gec <= 0 ? "Cari (0 gün)" :
        gec <= 30 ? "1–30 gün" :
        gec <= 90 ? "31–90 gün" :
        "90+ gün (NPL)";

    // Hesab kodunun ilk rəqəmi/ilk 2 rəqəmi üzrə təsnifat.
    // 1,2 → aktiv;  3,4 → öhdəlik (44/45 və 5x istisna → kapital);  qalanı → təsnifsiz.
    private static (string kat, string qrup) Tesnif(string hesab, string ad = "")
    {
        if (string.IsNullOrEmpty(hesab) || hesab.Length < 2)
            return ("tesnifsiz", "Təsnif edilməmiş");

        var d1 = hesab[0];
        var p2 = hesab.Substring(0, 2);

        // Kapital və ehtiyatlar
        if (p2 == "44" || p2 == "45" || d1 == '5')
            return ("kapital", "Kapital və ehtiyatlar");

        if (d1 == '1' || d1 == '2')
        {
            // Ehtiyat / provision alt-qrupu (3-4-cü rəqəm "91", məs. 20910, 21910, 23911) —
            // kredit qalıqlı (mənfi), aktivin azaldıcısıdır.
            //  • 20-21 "91" → KREDİT ehtiyatı: kreditin xalis dəyərinə qatılır
            //    (Müştərilərə kreditlər = əsas + faiz + kredit ehtiyatları).
            //  • 22-23 "91" (məs. 23911) → kreditlə bağlı DEYİL: ayrıca "Digər ehtiyyat".
            if (hesab.Length >= 4 && hesab.Substring(2, 2) == "91"
                && (p2 == "20" || p2 == "21"))
                return ("aktiv", "Müştərilərə kreditlər");
            if (hesab.Length >= 4 && hesab.Substring(2, 2) == "91"
                && (p2 == "22" || p2 == "23"))
                return ("aktiv", "Digər ehtiyyat");

            string q = p2 switch
            {
                "10" => "Kassa (nağd vəsaitlər)",
                "11" => "AMB (Mərkəzi Bank)",             // mühasib: ilk 2 rəqəm 11 → AMB (NOSTRO daxil)
                "15" => "Müxbir hesablar",                 // mühasib: ilk 2 rəqəm 15 → M/H (müxbir hesab)
                "12" or "13" or "14" => "Qiymətli kağızlar",
                "20" or "21" or "22" or "23" => "Müştərilərə kreditlər",
                "24" or "25" or "26" => "Hesablanmış faizlər və digər aktivlər",
                "27" or "28" => "Əsas vəsaitlər və qeyri-maddi aktivlər",
                _ => "Digər aktivlər"
            };
            return ("aktiv", q);
        }

        if (d1 == '3' || d1 == '4')
        {
            // Real müştəri depozitləri (hüquqi/fiziki) BalansAsync-də dep_tip flaqı
            // ilə ayrılır (frm_Dep müştəri məntiqi). Burada yalnız qalanlar:
            //   35/36 → bank öhdəlikləri; digər 3x/4x (müştərisiz depozit, bağlı
            //   hesab və s.) → digər öhdəliklər.
            string q = (p2 == "35" || p2 == "36") ? "Bank və maliyyə öhdəlikləri" : "Digər öhdəliklər";
            return ("ohdelik", q);
        }

        return ("tesnifsiz", "Təsnif edilməmiş");
    }

    private static string ValyutaAd(string kod) => kod switch
    {
        "00" => "AZN",
        "01" => "USD",
        "02" => "EUR",
        "03" => "RUB",
        "04" => "IRR",
        "05" => "AED",
        _ => "Digər"
    };

    // Oracle sütun adları böyük hərflə gələ bilər — case-insensitive axtarış.
    private static object? Val(Dictionary<string, object?> r, string key)
    {
        foreach (var kv in r)
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        return null;
    }

    private static decimal Dec(object? o) =>
        (o == null || o == DBNull.Value) ? 0m : Convert.ToDecimal(o);
}

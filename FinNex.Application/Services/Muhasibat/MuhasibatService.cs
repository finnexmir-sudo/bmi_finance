using FinNex.Application.DTOs.Muhasibat;
using FinNex.Application.Interfaces.Muhasibat;
using FinNex.Application.Interfaces.Oracle;
using FinNex.Application.Interfaces.Sorgular;

namespace FinNex.Application.Services.Muhasibat;

// Mühasibat — Balans İcmalı servisi.
// Oracle YALNIZ SELECT (CLAUDE.md). Tarix parametri validasiya olunmuş DateTime-dır
// və ciddi formatla (dd/MM/yyyy) SQL-ə yerləşdirilir — sərbəst istifadəçi mətni deyil.
public class MuhasibatService : IMuhasibatService
{
    private readonly IOracleService _oracle;
    private readonly IOracleSorguService _sorgu;

    public MuhasibatService(IOracleService oracle, IOracleSorguService sorgu)
    {
        _oracle = oracle;
        _sorgu = sorgu;
    }

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

    // SQL-i OracleSorgular-dan ad ilə oxu. Yoxdursa xəta at (embedded fallback yoxdur).
    private async Task<string> SqlAl(string ad)
    {
        var res = await _sorgu.HamisiniGetirAsync();
        var q = res?.Data?.FirstOrDefault(x => x.Aktiv
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
            // Bölmələri ARDICIL çağır — rəqəmlər tab-larla eynidir.
            // (Paralel OLMAZ: sorğu mətnləri EF Core DbContext-dən oxunur, o isə
            // thread-safe deyil — paralel çağırış "second operation on context" atır.
            // Oracle sorğuları özləri yeni bağlantı açır, amma SqlAl DbContext-ə vurur.)
            var bal     = await BalansAsync(t);
            var dep     = await DepozitAsync(t);
            var krd     = await KreditPortfelAsync(t);
            var lkv     = await LikvidlikAsync(t);
            var pnl     = await MenfeetAsync(new DateTime(t.Year, 1, 1), t);

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

            // Valyuta/ticarət (66/68 gəlir, 86/88 zərər) — gündəlik yenidən qiymətləndirmə
            // olduğu üçün GROSS böyükdür, amma bir-birini kompensasiya edir. NET göstərilir
            // ki, struktur və Cost/Income təhrif olunmasın (pre-provision mənfəət dəyişmir).
            var vGelir = gelirD.GetValueOrDefault("Valyuta/ticarət gəliri");
            var vZerer = xercD.GetValueOrDefault("Valyuta/ticarət zərəri");
            gelirD.Remove("Valyuta/ticarət gəliri");
            xercD.Remove("Valyuta/ticarət zərəri");
            var netFx = vGelir - vZerer;
            if (netFx >= 0m) gelirD["Valyuta/ticarət (net)"] = netFx;
            else             xercD["Valyuta/ticarət (net)"]  = -netFx;

            dto.FaizGeliri   = Math.Round(gelirD.GetValueOrDefault("Faiz gəliri"), 2);
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

    // P&L kateqoriya təsnifatı hesab sinifinə (ilk 2 rəqəm) görə.
    // gelir=true → sinif 6/7 (kredit dövriyyəsi); gelir=false → sinif 8 (debet dövriyyəsi).
    private static (string kat, bool gelir) MenfeetTesnif(string sinif2) => sinif2 switch
    {
        "60" or "61" or "63" or "64" or "65" => ("Faiz gəliri", true),
        "66" or "68"                          => ("Valyuta/ticarət gəliri", true),
        "67"                                  => ("Komissiya gəliri", true),
        "70" or "72"                          => ("Digər gəlir", true),
        "81" or "82" or "84" or "85"          => ("Faiz xərci", false),
        "86" or "88"                          => ("Valyuta/ticarət zərəri", false),
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
                        dto.Setirler.Add(new MuhasibatDetalSetirDto
                        {
                            Kod = muq, Ad = TipAd((int)Dec(Val(r, "tip"))),
                            Valyuta = ValyutaAd(Val(r, "valyuta")?.ToString() ?? ""),
                            Mebleg = Math.Round(qaliq, 2),
                            Elave = gec > 0 ? $"DPD {gec}" : "cari"
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
                    // P&L drill-down — madde = kateqoriya adı (məs. "Faiz gəliri").
                    // Hesab-səviyyə dövriyyə, aqreqat ilə EYNİ MenfeetTesnif təsnifatı.
                    var bb = (bas ?? new DateTime((son ?? t).Year, 1, 1)).Date;
                    var ss = (son ?? t).Date;
                    var sql = (await SqlAl(AdMenfeetDetal))
                        .Replace("{BAS}", bb.ToString("dd/MM/yyyy"))
                        .Replace("{SON}", ss.ToString("dd/MM/yyyy"));
                    var rows = await _oracle.SelectAsync(sql, maxRows: 20000);
                    var fxNet = madde == "Valyuta/ticarət (net)";
                    foreach (var r in rows)
                    {
                        var sinif2 = Val(r, "sinif2")?.ToString() ?? "";
                        var (kat, gelir) = MenfeetTesnif(sinif2);
                        decimal meb;
                        if (fxNet)
                        {
                            // net kateqoriya: gəlir tərəf (66/68) +kredit, zərər tərəf (86/88) −debet.
                            if (kat == "Valyuta/ticarət gəliri") meb = Dec(Val(r, "kredit"));
                            else if (kat == "Valyuta/ticarət zərəri") meb = -Dec(Val(r, "debet"));
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

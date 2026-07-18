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

            decimal huquqi = 0m, fiziki = 0m;
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

                if (tip == "fiziki") fiziki += q; else huquqi += q;

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
            dto.UmumiPortfel = Math.Round(huquqi + fiziki, 2);
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

            dto.Ugurlu = true;
        }
        catch (Exception ex)
        {
            dto.Ugurlu = false;
            dto.Xeta = ex.Message;
        }

        return dto;
    }

    public async Task<MuhasibatKreditDto> KreditPortfelAsync()
    {
        var dto = new MuhasibatKreditDto { Tarix = DateTime.Now.Date };

        try
        {
            var sql = await SqlAl(AdKredit);
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
                if (qaliq == 0m) continue;

                total += qaliq;
                vkTotal += vk * kurs;
                say++;
                if (gec >= 90) npl += qaliq;

                var tip = TipAd((int)Dec(Val(r, "tip")));
                tipD[tip] = tipD.GetValueOrDefault(tip) + qaliq;

                var tey = TeyinatAd((int)Dec(Val(r, "teyinat")));
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
            decimal likvid = 0m, aktiv = 0m, ohdelik = 0m, level2 = 0m;
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
                    if (lq == "Cari likvid vəsaitlər") level2 += qaliq;   // Level 2 (haircut)
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

            // Təxmini LCR — Level 2 haircut 25%; net outflow = fiziki×10% + hüquqi×40%
            var hqla = likvid - 0.25m * level2;
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
        if (p5 is "14010" or "14012" or "14014" or "14030" or "14032" or "14034") return "Banklararası / Mərkəzi Bank";
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
                            dto.Setirler.Add(DSetir(hesab, ad, ValyutaAd(valKod), qaliq, InvalDisp(qaliq)));
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

                    if (mod == "musteri")
                    {
                        var mtip = parts.Length > 1 ? parts[1] : "";
                        var mqeyd = parts.Length > 2 ? parts[2] : "";
                        foreach (var r in rows)
                        {
                            if ((Val(r, "tip")?.ToString() ?? "") != mtip) continue;
                            if ((Val(r, "qeyd")?.ToString() ?? "") != mqeyd) continue;
                            var q = Dec(Val(r, "qaliq"));
                            if (q == 0m) continue;
                            var ad = Val(r, "musteri")?.ToString() ?? "(adsız)";
                            var hesab = Val(r, "hesab")?.ToString() ?? mqeyd;
                            dto.Setirler.Add(DSetir(hesab, ad, ValyutaAd(Val(r, "valyuta")?.ToString() ?? ""), q));
                        }
                        dto.Baslik = "Depozitor hesabları";
                    }
                    else
                    {
                        var arg = parts.Length > 1 ? parts[1] : "";
                        var musteriler = new Dictionary<string, (string ad, string tip, decimal meb)>();
                        foreach (var r in rows)
                        {
                            var tip = Val(r, "tip")?.ToString() ?? "";
                            var qeyd = Val(r, "qeyd")?.ToString() ?? "";
                            var vk = Val(r, "valyuta")?.ToString() ?? "";
                            var q = Dec(Val(r, "qaliq"));
                            if (q == 0m) continue;
                            if (mod == "tip" && tip != arg) continue;
                            if (mod == "valyuta" && ValyutaAd(vk) != arg) continue;
                            var ad = Val(r, "musteri")?.ToString() ?? "(adsız)";
                            var key = tip + "|" + qeyd;
                            if (musteriler.TryGetValue(key, out var cur))
                                musteriler[key] = (cur.ad, cur.tip, cur.meb + q);
                            else
                                musteriler[key] = (ad, tip, q);
                        }
                        foreach (var m in musteriler.OrderByDescending(x => x.Value.meb))
                        {
                            var qeyd = m.Key.Contains('|') ? m.Key.Split('|')[1] : m.Key;
                            dto.Setirler.Add(new MuhasibatDetalSetirDto
                            {
                                Kod = qeyd, Ad = m.Value.ad, Mebleg = Math.Round(m.Value.meb, 2),
                                Elave = m.Value.tip == "fiziki" ? "fiziki" : "hüquqi"
                            });
                        }
                    }
                    break;
                }

                case "kredit":
                {
                    // madde: "tip:<Ad>" | "teyinat:<Ad>" | "valyuta:<VAL>" | "age:<Ad>"
                    var sql = await SqlAl(AdKredit);
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
                        if (qaliq == 0m) continue;
                        bool uygun = mod switch
                        {
                            "tip"      => TipAd((int)Dec(Val(r, "tip"))) == mval,
                            "teyinat"  => TeyinatAd((int)Dec(Val(r, "teyinat"))) == mval,
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

                default:
                    throw new InvalidOperationException($"Naməlum sahə: {sahe}");
            }

            // "elaqeli" SQL-də qrupla sıralanır — burada re-sort etmə.
            if (s0 is "balans" or "balans-valyuta" or "balans-menfeet" or "likvidlik" or "rezident")
                dto.Setirler = dto.Setirler.OrderByDescending(x => Math.Abs(x.Mebleg)).ToList();

            dto.Cem = Math.Round(dto.Setirler.Sum(x => x.Mebleg), 2);
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

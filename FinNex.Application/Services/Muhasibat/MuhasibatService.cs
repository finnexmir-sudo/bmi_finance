using FinNex.Application.DTOs.Muhasibat;
using FinNex.Application.Interfaces.Muhasibat;
using FinNex.Application.Interfaces.Oracle;

namespace FinNex.Application.Services.Muhasibat;

// Mühasibat — Balans İcmalı servisi.
// Oracle YALNIZ SELECT (CLAUDE.md). Tarix parametri validasiya olunmuş DateTime-dır
// və ciddi formatla (dd/MM/yyyy) SQL-ə yerləşdirilir — sərbəst istifadəçi mətni deyil.
public class MuhasibatService : IMuhasibatService
{
    private readonly IOracleService _oracle;

    public MuhasibatService(IOracleService oracle) => _oracle = oracle;

    // Balans qalıqları — bir tarixə bütün açıq hesablar (odb.arh_saldo_ls).
    // saldo_ish_nacval = günün sonuna milli valyutada (AZN) qalıq.
    private const string BalansSql = @"
SELECT ar.licsch AS hesab,
       CASE WHEN SUBSTR(ar.licsch,0,3) IN ('159','209','219','239','259')
            THEN SUBSTR(ar.licsch,16,2) ELSE SUBSTR(ar.licsch,6,2) END AS valyuta,
       ar.saldo_ish_nacval AS qaliq
FROM   odb.arh_saldo_ls ar, licsch ch
WHERE  ar.date_oper = TO_DATE('{TARIX}','dd/mm/yyyy')
  AND  ch.licsch = ar.licsch
  AND  (ch.date_close_licsch IS NULL OR ar.date_oper <= ch.date_close_licsch)";

    // Depozit hesabları — hüquqi (40/3x) + fiziki (41), müştəri linki ilə.
    // saldo_ish_nacval passiv → mənfi; -qaliq ilə müsbətə çevrilir.
    private const string DepozitSql = @"
SELECT 'huquqi' tip, b.registrac_nomer qeyd, r.name_regnom musteri,
       SUBSTR(l.licsch,6,2) valyuta, -l.saldo_ish_nacval qaliq
FROM   odb.arh_saldo_ls l, licsch b, regnom r
WHERE  l.date_oper = TO_DATE('{TARIX}','dd/mm/yyyy')
  AND  b.licsch = l.licsch AND r.regnom = b.registrac_nomer
  AND  (SUBSTR(l.licsch,1,2)='40' OR SUBSTR(l.licsch,1,1)='3')
  AND  SUBSTR(l.licsch,1,5) NOT IN ('35020','35025','35026','35940')
  AND  SUBSTR(l.licsch,10,6) <> '000004'
  AND  l.saldo_ish_nacval <> 0
UNION ALL
SELECT 'fiziki' tip, SUBSTR(l.licsch,10,6) qeyd, r.name_regnom musteri,
       SUBSTR(l.licsch,6,2) valyuta, -l.saldo_ish_nacval qaliq
FROM   odb.arh_saldo_ls l, regnom r
WHERE  l.date_oper = TO_DATE('{TARIX}','dd/mm/yyyy')
  AND  SUBSTR(l.licsch,1,2)='41' AND r.regnom = SUBSTR(l.licsch,10,6)
  AND  SUBSTR(l.licsch,10,6) <> '000004'
  AND  l.saldo_ish_nacval <> 0";

    // Cari kredit portfeli — sysdate snapshot. summa=əsas, summa_19=VK.
    // kurs bir dəfə çıxarılır; qalıq C#-da (esas+vk)*kurs kimi hesablanır.
    private const string KreditSql = @"
SELECT lk.tipkredita tip,
       lk.index_otrasli teyinat,
       SUBSTR(lk.licschkre,6,2) valyuta,
       ROUND(odb.func_get_kurval(SUBSTR(lk.licschkre,6,2), to_date(sysdate)), 6) kurs,
       lk.summa esas,
       lk.summa_19 vk,
       odb.tar_ferq360(x.date_oper, NVL(x.lastoverduedate, x.date_oper)) gec_gun
FROM   odb.licschkre lk, view_nacpogprokre_all x
WHERE  lk.licschpkre = x.licschpkre AND lk.subschkre = x.subschkre
  AND  x.date_oper = to_date(sysdate)
  AND  lk.date_close IS NULL AND LENGTH(lk.licschkre) = 20";

    public async Task<MuhasibatBalansDto> BalansAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatBalansDto { Tarix = t };

        try
        {
            var sql = BalansSql.Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 200000);

            var aktiv   = new Dictionary<string, decimal>();
            var ohdelik = new Dictionary<string, decimal>();
            var valyuta = new Dictionary<string, decimal>();
            decimal kapital = 0m, tesnifsiz = 0m;

            foreach (var r in rows)
            {
                var hesab = Val(r, "hesab")?.ToString() ?? "";
                var valKod = Val(r, "valyuta")?.ToString() ?? "";
                var qaliq = Dec(Val(r, "qaliq"));
                if (qaliq == 0m) continue;

                var (kat, qrup) = Tesnif(hesab);
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

            dto.Aktivler = aktiv.Where(x => Math.Abs(x.Value) > 0.005m)
                .OrderByDescending(x => x.Value)
                .Select(x => new BalansMaddeDto
                {
                    Ad = x.Key, Mebleg = Math.Round(x.Value, 2),
                    Faiz = dto.UmumiAktiv != 0 ? Math.Round(x.Value / dto.UmumiAktiv * 100, 1) : 0
                }).ToList();

            dto.Ohdelikler = ohdelik.Where(x => Math.Abs(x.Value) > 0.005m)
                .OrderByDescending(x => x.Value)
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
            var sql = DepozitSql.Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
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
            var rows = await _oracle.SelectAsync(KreditSql, maxRows: 300000);

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
    private static (string kat, string qrup) Tesnif(string hesab)
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
            string q = p2 switch
            {
                "10" => "Kassa (nağd vəsaitlər)",
                "11" => "AMB və müxbir hesablar",
                "12" or "13" or "14" => "Banklararası yerləşdirmələr",
                "15" => "Digər yerləşdirmələr / likvid aktivlər",
                "20" or "21" or "22" or "23" => "Müştərilərə kreditlər",
                "24" or "25" or "26" => "Hesablanmış faizlər və digər aktivlər",
                "27" or "28" => "Əsas vəsaitlər və qeyri-maddi aktivlər",
                _ => "Digər aktivlər"
            };
            return ("aktiv", q);
        }

        if (d1 == '3' || d1 == '4')
        {
            string q = p2 switch
            {
                "35" or "36" => "Bank və maliyyə öhdəlikləri",
                "38" or "39" => "Cəlb olunmuş vəsaitlər",
                "40" => "Hüquqi şəxs depozitləri",
                "41" => "Fiziki şəxs depozitləri",
                _ => "Digər öhdəliklər"
            };
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

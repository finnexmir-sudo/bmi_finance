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

    // SQL-i OracleSorgular-dan ad ilə oxu; yoxdursa embedded fallback qaytar.
    private async Task<string> SqlAl(string ad, string fallback)
    {
        try
        {
            var res = await _sorgu.HamisiniGetirAsync();
            var q = res?.Data?.FirstOrDefault(x => x.Aktiv
                && !string.IsNullOrWhiteSpace(x.SorguMetni)
                && string.Equals((x.SorguAdi ?? "").Trim(), ad, StringComparison.OrdinalIgnoreCase));
            return q != null ? q.SorguMetni : fallback;
        }
        catch { return fallback; }
    }

    // Balans qalıqları — bir tarixə bütün açıq hesablar (odb.arh_saldo_ls).
    // saldo_ish_nacval = günün sonuna milli valyutada (AZN) qalıq.
    // dep_tip: real müştəri depoziti? (frm_Dep məntiqi ilə eyni)
    //   'F' = fiziki (41, müştəri regnom var, ≠000004)
    //   'H' = hüquqi (40/3x, texniki istisna, müştəri regnom var, ≠000004)
    //   'X' = deyil → adi təsnifat (Tesnif)
    private const string BalansSql = @"
SELECT ar.licsch AS hesab,
       CASE WHEN SUBSTR(ar.licsch,0,3) IN ('159','209','219','239','259')
            THEN SUBSTR(ar.licsch,16,2) ELSE SUBSTR(ar.licsch,6,2) END AS valyuta,
       ar.saldo_ish_nacval AS qaliq,
       CASE
         WHEN SUBSTR(ar.licsch,1,2)='41'
              AND SUBSTR(ar.licsch,10,6)<>'000004'
              AND EXISTS (SELECT 1 FROM regnom r WHERE r.regnom=SUBSTR(ar.licsch,10,6))
           THEN 'F'
         WHEN (SUBSTR(ar.licsch,1,2)='40' OR SUBSTR(ar.licsch,1,1)='3')
              AND SUBSTR(ar.licsch,1,5) NOT IN ('35020','35025','35026','35940')
              AND SUBSTR(ar.licsch,10,6)<>'000004'
              AND EXISTS (SELECT 1 FROM regnom r WHERE r.regnom=ch.registrac_nomer)
           THEN 'H'
         ELSE 'X'
       END AS dep_tip
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

    // Əlaqəli tərəf depoziti (normativ) — şirkət (təsisçisi imza sahibi olan) +
    // təsisçi/imza sahibinin şəxsi depozitləri. İŞÇİ komponenti HƏLƏ YOX (frm_elaqeli
    // ilə tam uyğunluq üçün sonra əlavə olunacaq). Məxrəc: top_qal (35-49).
    private const string ElaqeliSql = @"
with elaqeli_hesab as (
  select distinct ar.licsch
  from odb.arh_saldo_ls ar, licsch l
  where ar.licsch = l.licsch and ar.date_oper = to_date('{TARIX}','dd/mm/yyyy')
    and substr(ar.licsch,1,1) in ('3','4')
    and (l.registrac_nomer in (select customer_regnom from imza_huquqi_olan_shexsler)
         or l.registrac_nomer in (select regnom from imza_huquqi_olan_shexsler))
)
select
  (select round(-sum(k.saldo_ish_nacval),2)
     from odb.arh_saldo_ls k, licsch p
    where p.licsch = k.licsch and k.date_oper = to_date('{TARIX}','dd/mm/yyyy')
      and substr(k.licsch,1,2) in ('35','36','38','39','40','41','49')
      and (p.date_close_licsch is null or k.date_oper <= p.date_close_licsch)) portfel,
  (select round(-sum(ar.saldo_ish_nacval),2)
     from odb.arh_saldo_ls ar
    where ar.date_oper = to_date('{TARIX}','dd/mm/yyyy')
      and ar.licsch in (select licsch from elaqeli_hesab)) elaqeli
from dual";

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

    // Valyuta alış/satış — arh_dd dövriyyələri (alış: 10060←10050; satış: 10050←10060).
    private const string ValyutaSql = @"
SELECT 'alis' yon, SUBSTR(d.debet,6,2) val, d.summa_v_inval hecm, d.summa_v_inval*d.kurs_valuti azn
FROM   arh_dd d
WHERE  d.date_oper BETWEEN TO_DATE('{BAS}','dd/mm/yyyy') AND TO_DATE('{SON}','dd/mm/yyyy')
  AND  SUBSTR(d.debet,1,5) = '10060' AND SUBSTR(d.kredit,1,5) = '10050'
UNION ALL
SELECT 'satis' yon, SUBSTR(d.kredit,6,2) val, d.summa_v_inval hecm, d.summa_v_inval*d.kurs_valuti azn
FROM   arh_dd d
WHERE  d.date_oper BETWEEN TO_DATE('{BAS}','dd/mm/yyyy') AND TO_DATE('{SON}','dd/mm/yyyy')
  AND  SUBSTR(d.debet,1,5) = '10050' AND SUBSTR(d.kredit,1,5) = '10060'";

    // Əvvəlki iş günü totalları (müqayisə üçün) — sadə sinif aqreqasiyası.
    private const string MuqayiseSql = @"
select
  round(sum(case when substr(ar.licsch,1,1) in ('1','2') then ar.saldo_ish_nacval else 0 end),2) aktiv,
  round(-sum(case when substr(ar.licsch,1,1) in ('3','4') and substr(ar.licsch,1,2) not in ('44','45') then ar.saldo_ish_nacval else 0 end),2) ohdelik,
  round(-sum(case when substr(ar.licsch,1,2) in ('44','45') or substr(ar.licsch,1,1)='5' then ar.saldo_ish_nacval else 0 end),2) kapital,
  round(-sum(case when substr(ar.licsch,1,5)='50130' then ar.saldo_ish_nacval else 0 end),2) menfeet
from odb.arh_saldo_ls ar, licsch ch
where ar.date_oper = (
    select max(c.date_oper) from odb.arh_saldo_ls c
    where c.date_oper < to_date('{TARIX}','dd/mm/yyyy')
      and c.date_oper >= to_date('{TARIX}','dd/mm/yyyy') - 10)
  and ch.licsch = ar.licsch
  and (ch.date_close_licsch is null or ar.date_oper <= ch.date_close_licsch)";

    // Rezident/qeyri-rezident — frm_reziden_ve_qeyri_rezident məntiqi (ABS qalıq).
    private const string RezidentSql = @"
select
  case when (substr(s.licsch,0,3)='409'
             and substr(regexp_substr(l.name_licsch,'\(([^()]*)\)\s*$',1,1,null,1),5,1)='5')
            or substr(s.licsch,0,5)='45029'
       then 'qr' else 'r' end tip,
  round(sum(abs(s.saldo_ish_nacval)),2) mebleg,
  count(*) say
from odb.arh_saldo_ls s, licsch l
where l.licsch = s.licsch
  and s.date_oper = to_date('{TARIX}','dd/mm/yyyy')
  and (l.date_close_licsch is null or l.date_close_licsch >= to_date('{TARIX}','dd/mm/yyyy'))
group by case when (substr(s.licsch,0,3)='409'
             and substr(regexp_substr(l.name_licsch,'\(([^()]*)\)\s*$',1,1,null,1),5,1)='5')
            or substr(s.licsch,0,5)='45029'
       then 'qr' else 'r' end";

    public async Task<MuhasibatBalansDto> BalansAsync(DateTime? tarix = null)
    {
        var t = (tarix ?? DateTime.Now.Date.AddDays(-1)).Date;
        var dto = new MuhasibatBalansDto { Tarix = t };

        try
        {
            var sql = (await SqlAl(AdBalans, BalansSql)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
            var rows = await _oracle.SelectAsync(sql, maxRows: 200000);

            var aktiv   = new Dictionary<string, decimal>();
            var ohdelik = new Dictionary<string, decimal>();
            var valyuta = new Dictionary<string, decimal>();
            decimal kapital = 0m, tesnifsiz = 0m, menfeet = 0m;

            foreach (var r in rows)
            {
                var hesab = Val(r, "hesab")?.ToString() ?? "";
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
                var msql = (await SqlAl(AdMuqayise, MuqayiseSql)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
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
            var sql = (await SqlAl(AdDepozit, DepozitSql)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
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
                var esql = (await SqlAl(AdElaqeli, ElaqeliSql)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
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
            var sql = await SqlAl(AdKredit, KreditSql);
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
            var sql = (await SqlAl(AdBalans, BalansSql)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
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
            var sql = (await SqlAl(AdValyuta, ValyutaSql))
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
            var sql = (await SqlAl(AdRezident, RezidentSql)).Replace("{TARIX}", t.ToString("dd/MM/yyyy"));
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

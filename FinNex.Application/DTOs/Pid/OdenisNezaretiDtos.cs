using System.Globalization;
using FinNex.Domain.Entities.Pid;

namespace FinNex.Application.DTOs.Pid;

public class OdenisNezaretiDto
{
    public int Id { get; set; }
    public BalansNovu BalansNovu { get; set; }
    public string BalansNovuAd   { get; set; } = "";
    public string MusteriAdi     { get; set; } = "";
    public string? HesabNomresi  { get; set; }
    public string? Teyinat       { get; set; }
    public DateTime? SonOdenisTarixi { get; set; }
    public string? OdenisVeziyyeti   { get; set; }
    public string? Qeyd          { get; set; }
}

public class OdenisNezaretiCreateDto
{
    public BalansNovu BalansNovu { get; set; }
    public string MusteriAdi     { get; set; } = "";
    public string? HesabNomresi  { get; set; }
    public string? Teyinat       { get; set; }
    // input type=date "yyyy-MM-dd" göndərir — string alıb servisdə invariant parse edirik
    // (app az-Latn-AZ mədəniyyətindədir, DateTime birbaşa bind olmur)
    public string? SonOdenisTarixi { get; set; }
    public string? OdenisVeziyyeti { get; set; }
    public string? Qeyd          { get; set; }
}

public class OdenisNezaretiUpdateDto : OdenisNezaretiCreateDto
{
    public int Id { get; set; }
}

// ── Oracle-dan canlı "Ödənişə Nəzarət" (Aktiv Müştərilər + ARH_DD son ödəniş) ──
// Mənbə: Admin → Oracle Sorğular-da saxlanılan adlı sorğu ("Ödəniş" + "Nəzarət").
// Yalnız oxuma — Oracle-a heç bir yazı yoxdur.
public class OdenisNezaretSatirDto
{
    public string? Region        { get; set; }
    public string  Musteri       { get; set; } = "";   // name_regnom
    public string  KreditHesabi  { get; set; } = "";   // licschkre
    public string  Ks            { get; set; } = "";   // subschkre (formatlı "00")
    public string? KreditinNovu  { get; set; }
    public decimal? TamQaliq     { get; set; }
    public decimal? Qaliq        { get; set; }
    public decimal? VkQaliq      { get; set; }
    public string? Status        { get; set; }          // item_01
    public string? Item10        { get; set; }          // item_10 (son fəaliyyət)
    public string? Item13        { get; set; }          // item_13 (ödənişlə məşğul olan şirkət)
    public string? SistemSonEmel { get; set; }          // lastpaymentdate_ish (dd.MM.yyyy)

    // AVTOMAT — ARH_DD: müştərinin cari hesabından (DEBET 3/4) kredit hesabına (KREDIT 2121) son köçürmə
    public string?  SonOdenisTarixi  { get; set; }      // MAX(date_oper) (dd.MM.yyyy)
    public decimal? SonOdenisMeblegi { get; set; }      // ən son tarixli əməliyyat(lar)ın məbləği
    public decimal? OdenisCemi       { get; set; }      // SUM(summa_v_nacval) — yalnız kredit ödənişləri
    public decimal? OdenisCariAy     { get; set; }      // cari ayın ödəniş cəmi (odenis_cari_ay)
    public decimal? OdenisKecenAy    { get; set; }      // keçən ayın ödəniş cəmi (odenis_kecen_ay)
    public int?     OdenisSayi       { get; set; }      // COUNT(*) — yalnız kredit ödənişləri
    public decimal? RusumCemi        { get; set; }      // SUM(summa_v_nacval) — məhkəmə rüsumu (KREDIT 7011…)
    public decimal? RusumCariAy      { get; set; }      // cari ayın rüsum cəmi (rusum_cari_ay)
    public decimal? RusumKecenAy     { get; set; }      // keçən ayın rüsum cəmi (rusum_kecen_ay)

    // ── Hesablanmış monitorinq siqnalı ──
    public DateTime? SonOdenisParsed
        => DateTime.TryParseExact((SonOdenisTarixi ?? "").Trim(), "dd.MM.yyyy",
               CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;

    public int? GunEvvel
        => SonOdenisParsed.HasValue ? (int)(DateTime.Today - SonOdenisParsed.Value.Date).TotalDays : (int?)null;

    // İzlənən pəncərədə (sorğudakı tarix filtrindən bəri) heç ödəniş görünmür → əsas risk siqnalı
    public bool OdenisYox => !SonOdenisParsed.HasValue;

    // Son ödəniş tarixinin il/ay komponentləri (il+ay filtri üçün)
    public int? SonOdenisIl => SonOdenisParsed?.Year;
    public int? SonOdenisAy => SonOdenisParsed?.Month;
}

public class OdenisNezaretSiyahiDto
{
    public bool SorguTapildi { get; set; }
    public string? Xeta { get; set; }
    public List<OdenisNezaretSatirDto> Setirler { get; set; } = new();

    // ── Xülasə ──
    public int CemMusteri      => Setirler.Count;
    public int OdenisEdenSay   => Setirler.Count(x => !x.OdenisYox);
    public int OdenisEtmeyenSay => Setirler.Count(x => x.OdenisYox);
}

// ── GRAY LIST — Ödənişə Nəzarət (ARH_DD: debet 45019…, kredit 89150…) ─────────
// Balansdankənar graylist müştərilərinin ödənişləri. Müştəri adı əməliyyatın
// TƏYİNATINDAN (primechanie) çıxarılır — ilk mötərizə içindəki ad.
// Mənbə sorğu: Admin → Oracle Sorğular, adında "Odenis"+"Nezaret"+"Gray".
public class GrayOdenisDto
{
    public DateTime? Tarix   { get; set; }
    public decimal   Mebleg  { get; set; }
    public string?   Teyinat { get; set; }   // primechanie — tam mətn
}

public class GrayMusteriDto
{
    public string Musteri { get; set; } = "(naməlum)";

    public DateTime? SonOdenisTarixi  { get; set; }
    public decimal?  SonOdenisMeblegi { get; set; }
    public decimal   CariAy  { get; set; }   // cari ayın ödəniş cəmi
    public decimal   KecenAy { get; set; }   // keçən ayın ödəniş cəmi
    public decimal   Cemi    { get; set; }   // bütün dövrün cəmi
    public int       Say     { get; set; }   // əməliyyat sayı

    // Drill-down — müştərinin bütün ödənişləri (tarix DESC)
    public List<GrayOdenisDto> Odenisler { get; set; } = new();
}

public class GrayOdenisSiyahiDto
{
    public bool SorguTapildi { get; set; }
    public string? Xeta { get; set; }
    public List<GrayMusteriDto> Setirler { get; set; } = new();

    public int     CemMusteri => Setirler.Count;
    public decimal CemCariAy  => Setirler.Sum(x => x.CariAy);
    public decimal CemKecenAy => Setirler.Sum(x => x.KecenAy);
    public decimal CemUmumi   => Setirler.Sum(x => x.Cemi);
}

// ── GÜNDƏLİK KREDİT ÖDƏNİŞLƏRİ — Ödənişə Nəzarət, 3-cü tab (17.11.2026) ──────
// Mənbə: odb.docdna (CANLI, yalnız bugünkü gün). Debet — kassa (100/250/110),
// müştərinin cari hesabı (sinif 3/4) və ya köçürmə (otvet_ispoln=0). Kredit tərəfi
// MÜŞTƏRİ REGNOM-u üzrə uyğunlaşdırılır (SUBSTR(hesab,10,6)) — ƏSL hesab bərabərliyi
// YOX, çünki real ödəniş çox vaxt ƏVVƏLCƏ müştərinin CARİ hesabına düşür, kredit
// hesabının ÖZÜNƏ hələ KEÇMƏYİB. "Hesabı" sütunu bu iki halı ayırd edir.
//
// ⚠️ Eyni müştərinin BİRDƏN ÇOX aktiv krediti varsa, BİR ödəniş HƏR kredit üçün
// AYRI sətir kimi çıxır (eyni RECNUM-la) — subschkre join-i qəsdən yoxdur (bax
// SQL faylının öz şərhi: docs/sql/pid/PID_OracleSorgular_GunlukKreditOdenisleri.sql).
// ⚠️ "Status" (Tam bağlanıb/Problemli) BURADA YOXDUR — hələ yenidən qurulmayıb.
// ⚠️ Faiz gecikməsi (DPD) BURADA YOXDUR — yalnız əsas borc (VkEsas) göstərilir.
public class GunlukOdenisSatirDto
{
    public DateTime? Tarix        { get; set; }
    public string    Debet        { get; set; } = "";
    public string    Kredit       { get; set; } = "";
    public string?   Hesabi       { get; set; }   // "Əsas (birbaşa)" / "...Cari hesaba daxil olub..."
    public decimal   Mebleg       { get; set; }
    public decimal?  RecNum       { get; set; }   // eyni RECNUM = çox-kreditli müştəridə eyni ödənişin fan-out-u
    public string?   Teyinat      { get; set; }
    public string?   QeydNo       { get; set; }   // regnom kodu (6 rəqəm)
    public string    Adi          { get; set; } = "(naməlum)";
    public string    Kat          { get; set; } = "";   // kreditin öz hesabı (licschkre)
    public string?   Novu         { get; set; }         // girov növü (tipzal)
    public string?   Tipi         { get; set; }         // kredit tipi (tipkre)
    public DateTime? KreditBagliTarixi { get; set; }    // həmişə boş olmalıdır — sorğu yalnız açıq kreditləri gətirir
    public decimal?  VkEsas       { get; set; }         // vaxtı keçmiş əsas qalıq
    public string?   Gecikme      { get; set; }         // "Gecikir" / "Vaxtında"

    public bool CoxKreditliFanOut { get; set; }   // servis doldurur: eyni RECNUM başqa sətirdə də varsa
}

public class GunlukOdenisSiyahiDto
{
    public bool SorguTapildi { get; set; }
    public string? Xeta { get; set; }
    public List<GunlukOdenisSatirDto> Setirler { get; set; } = new();

    public int     CemSetir     => Setirler.Count;
    public decimal CemMebleg    => Setirler.Sum(x => x.Mebleg);
    public int     GecikenSay   => Setirler.Count(x => x.Gecikme == "Gecikir");
    public int     Muxtelif     => Setirler.Select(x => x.QeydNo).Distinct().Count();
}

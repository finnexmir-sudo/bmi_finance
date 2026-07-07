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
    public string? SistemSonEmel { get; set; }          // lastpaymentdate_ish (dd.MM.yyyy)

    // AVTOMAT — ARH_DD: müştərinin cari hesabından (DEBET 3/4) kredit hesabına (KREDIT 2121) son köçürmə
    public string?  SonOdenisTarixi  { get; set; }      // MAX(date_oper) (dd.MM.yyyy)
    public decimal? SonOdenisMeblegi { get; set; }      // ən son tarixli əməliyyat(lar)ın məbləği
    public decimal? OdenisCemi       { get; set; }      // SUM(summa_v_nacval) — izlənən pəncərə
    public int?     OdenisSayi       { get; set; }      // COUNT(*)

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

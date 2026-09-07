namespace FinNex.Application.DTOs.Emeliyyat;

// Siyahı üçün
public class KocurmeListDto
{
    public int       Id            { get; set; }
    public string?   HevaleNo      { get; set; }
    public DateTime? Tarix         { get; set; }
    public string?   GonderenTamAd { get; set; }   // ad soyad ata (birləşdirilmiş)
    public string?   AlanTamAd     { get; set; }
    public decimal?  Mebleg        { get; set; }
    public string?   KocurulenValyuta { get; set; }
    public string?   BankAd        { get; set; }
    public short?    Icra          { get; set; }
    public string?   IcraciAd      { get; set; }
    public int?      YaradanId     { get; set; }

    // ── 20 000 USD limiti — siyahıda görünür ki, «sistem düz işləyib,
    //    işçi filan sənədə görə keçirə bilib» sualı ekrandan cavablansın.
    public string?   GonderenFin   { get; set; }
    public decimal?  UsdEkvivalent { get; set; }
    public string?   SenedNovuAd   { get; set; }
    public string?   LimitQeydi    { get; set; }
}

// Yaratma / redaktə üçün ortaq sahələr
public class KocurmeFormDto
{
    public string?   HevaleNo { get; set; }   // avtomatik ({il}-T-{sıra}); formada oxunur
    public DateTime? Tarix { get; set; }

    public string?   GonderenAd       { get; set; }
    public string?   GonderenSoyad    { get; set; }
    public string?   GonderenAtaAd    { get; set; }
    public string?   GonderenPassport { get; set; }
    public string?   GonderenTelefon  { get; set; }

    public string?   AlanAd       { get; set; }
    public string?   AlanSoyad    { get; set; }
    public string?   AlanAtaAd    { get; set; }
    public string?   AlanPassport { get; set; }
    public string?   AlanTelefon  { get; set; }

    public decimal?  Mebleg           { get; set; }
    public decimal?  RialCbar         { get; set; }
    public decimal?  ValyutaCbar      { get; set; }
    public decimal?  IranRial         { get; set; }
    public string?   MedaxilValyuta   { get; set; }
    public string?   KocurulenValyuta { get; set; }

    public string?   Secim { get; set; }

    // ── 20 000 USD aylıq limiti (07.09.2026) ──────────────────────────────
    /// <summary>Göndərənin FİN kodu — limit bu sahə üzrə hesablanır.</summary>
    public string?   GonderenFin { get; set; }

    /// <summary>Limit aşılanda əsas sənədin növü (açar cədvəldən). Aşılmayıbsa boş.</summary>
    public int?      SenedNovuId { get; set; }

    /// <summary>Limit aşılanda sərbəst izah — məcburi deyil.</summary>
    public string?   LimitQeydi  { get; set; }

    public string?   BankAd    { get; set; }
    public string?   Filial    { get; set; }
    public string?   AlanHesab { get; set; }

    public string?   Elave  { get; set; }
    public string?   Meqsed { get; set; }
    public string?   Qeyd   { get; set; }
}

public class KocurmeCreateDto : KocurmeFormDto { }

// Detal — qeyd + hesablanmış debet/kredit voucher
public class KocurmeDetalDto
{
    public int       Id            { get; set; }
    public string?   HevaleNo      { get; set; }
    public DateTime? Tarix         { get; set; }
    public string?   GonderenTamAd { get; set; }
    public string?   AlanTamAd     { get; set; }
    public string?   GonderenAd    { get; set; }
    public string?   GonderenSoyad { get; set; }
    public string?   GonderenAtaAd { get; set; }
    public string?   GonderenTelefon { get; set; }
    public string?   AlanAd        { get; set; }
    public string?   AlanSoyad     { get; set; }
    public string?   AlanAtaAd     { get; set; }
    public string?   GonderenPassport { get; set; }
    public string?   AlanPassport  { get; set; }
    public string?   Elave         { get; set; }
    public string?   Qeyd          { get; set; }
    public decimal?  Mebleg        { get; set; }
    public string?   MedaxilValyuta   { get; set; }
    public string?   KocurulenValyuta { get; set; }
    public string?   Secim         { get; set; }
    public decimal?  IranRial      { get; set; }
    public decimal?  RialCbar      { get; set; }
    public decimal?  ValyutaCbar   { get; set; }
    public string?   BankAd        { get; set; }
    public string?   Filial        { get; set; }
    public string?   AlanHesab     { get; set; }
    public string?   Meqsed        { get; set; }
    public string?   GonderenFin   { get; set; }
    public decimal?  UsdEkvivalent { get; set; }
    public string?   SenedNovuAd   { get; set; }
    public string?   LimitQeydi    { get; set; }
    public int?      YaradanId     { get; set; }
    public IList<MuhasibatSetirDto> Setirler { get; set; } = new List<MuhasibatSetirDto>();
}

public class KocurmeEditDto : KocurmeFormDto
{
    public int     Id        { get; set; }
    public int?    YaradanId { get; set; }
}

// ══ 20 000 USD AYLIQ LİMİTİ (07.09.2026) ═══════════════════════════════════
//
// Qanun: rezident və qeyri-rezident fiziki şəxsin təqvim ayı ərzində cəmi
// 20 000 ABŞ dolları ekvivalentinədək köçürmələri məqsədi bəyan edilməklə
// aparılır. Həddi aşan hissə üçün əsas sənəd tələb olunur.

/// <summary>Əsaslandırma sənədinin növü — açar siyahısı.</summary>
public class KocurmeSenedNovuDto
{
    public int    Id       { get; set; }
    public string Ad       { get; set; } = "";
    public int    Sira     { get; set; }
    public bool   Aktivdir { get; set; }
}

/// <summary>
/// Bir FİN üzrə bir təqvim ayının vəziyyəti — formada və siyahıda göstərilir.
/// </summary>
public class FinLimitDto
{
    public string   Fin      { get; set; } = "";
    public int      Il       { get; set; }
    public int      Ay       { get; set; }

    /// <summary>Həmin ayda bu FİN üzrə YAZILMIŞ köçürmələrin USD cəmi.</summary>
    public decimal  CemiUsd  { get; set; }

    /// <summary>Cəmə düşən köçürmə sayı — «neçə əməliyyat» sualı üçün.</summary>
    public int      Sayi     { get; set; }

    /// <summary>Limit (20 000) — sabit deyil, servisdən gəlir ki, dəyişəndə tək yer olsun.</summary>
    public decimal  Limit    { get; set; }

    /// <summary>Limitə qalan məbləğ. Aşılıbsa MƏNFİ olur.</summary>
    public decimal  Qaliq => Limit - CemiUsd;

    /// <summary>Yalnız MÖVCUD qeydlərlə limit aşılıbmı (yeni məbləğ hesaba alınmadan).</summary>
    public bool     Asilib => CemiUsd > Limit;
}

/// <summary>
/// Yeni/redaktə olunan məbləğ nəzərə alınmaqla limit yoxlamasının nəticəsi.
/// </summary>
public class FinLimitYoxlamaDto
{
    public FinLimitDto Movcud { get; set; } = new();

    /// <summary>Yoxlanan əməliyyatın USD ekvivalenti.</summary>
    public decimal YeniUsd { get; set; }

    /// <summary>Bu əməliyyatdan SONRAKI cəm.</summary>
    public decimal SonraCem => Movcud.CemiUsd + YeniUsd;

    /// <summary>Bu əməliyyatla limit aşılırmı — sənəd MƏCBURİ olur.</summary>
    public bool SenedTelebOlunur => SonraCem > Movcud.Limit;

    /// <summary>Kurs alına bilmədi (Oracle) — əməliyyat bloklanmalıdır.</summary>
    public bool KursAlinmadi { get; set; }

    /// <summary>Ekranda göstərilən izah.</summary>
    public string? Mesaj { get; set; }
}

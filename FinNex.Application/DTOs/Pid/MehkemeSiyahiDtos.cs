namespace FinNex.Application.DTOs.Pid;

// Canlı siyahıda bir kredit sətri: Oracle datası (canlı) + proqram overlay (SQL)
public class MehkemeKreditSatirDto
{
    // ── Kompozit açar (licschkre + subschkre) ──
    public string KreditHesabi { get; set; } = "";   // licschkre
    public string Ks           { get; set; } = "";   // subschkre — formatlı (məs. "00")

    // ── Oracle: əsas (sarı) sütunlar ──
    public string? Region             { get; set; }
    public string? BorcluAd           { get; set; }
    public decimal? TamQaliq          { get; set; }   // tam qalıq (əsas göstərici)
    public decimal? Qaliq             { get; set; }
    public decimal? VkQaliq           { get; set; }   // vaxtı keçmiş qalıq
    public decimal? FaizMeblegi       { get; set; }
    public decimal? VkFaizMeblegi     { get; set; }
    public string? SonEmeliyyatTarixi { get; set; }
    public string? Status             { get; set; }   // Item_01
    public string? SonFealiyyet       { get; set; }   // Item_10

    // ── Oracle: ikincili (boz) sütunlar ──
    public string?  Faiz            { get; set; }
    public string?  Ehtiyat         { get; set; }
    public decimal? KreditinMeblegi { get; set; }
    public string?  VerilmeTarixi   { get; set; }
    public string?  OverdueGun      { get; set; }
    public string?  Telefon         { get; set; }
    public string?  Mobil           { get; set; }
    public string?  KreditinNovu    { get; set; }
    public string?  GirovunNovu     { get; set; }
    public string?  Sektor          { get; set; }
    public string?  Item02          { get; set; }
    public string?  DogumTarixi     { get; set; }

    // ── Zaminlər (gizli, klikdə açılır) ──
    public List<MehkemeZaminDto> Zaminler { get; set; } = new();

    // ── Proqram overlay (SQL tracking) ──
    public bool IsAcilib     { get; set; }
    public int? MehkemeIsiId { get; set; }
    public string? Qerardad  { get; set; }
    public int  MerheleSayi  { get; set; }
}

public class MehkemeZaminDto
{
    public string? Ad          { get; set; }
    public string? Fin         { get; set; }
    public string? DogumTarixi { get; set; }
    public string? DogumYeri   { get; set; }
    public string? Olke        { get; set; }
    public string? Telefon     { get; set; }
    public string? Unvan       { get; set; }
    public string? Gelir       { get; set; }
    public string? BorcYuku    { get; set; }
}

// Siyahı nəticəsi (konfiqurasiya / xəta vəziyyəti ilə birlikdə)
public class MehkemeSiyahiResultDto
{
    public bool Konfiqurasiyali { get; set; }    // siyahı sorğusu seçilibmi
    public string? Xeta         { get; set; }
    public List<MehkemeKreditSatirDto> Satirlar { get; set; } = new();
}

// Kompozit açar + identity snapshot — "İş aç" / "Qərardad yaz" üçün
public class MehkemeKreditAcarDto
{
    public string KreditHesabi { get; set; } = "";
    public string Ks           { get; set; } = "";
    public string? BorcluAd    { get; set; }
    public decimal? EsasBorc   { get; set; }
}

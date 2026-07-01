namespace FinNex.Application.DTOs.Pid;

// PID — "Məhkəməyə hazırlıq" (Oracle sorğusu → borcalan + zaminlərin ətraflı məlumatı).
// Bildiriş səhifəsi ilə eyni görünüş; düymə yoxdur, zaminlər sətrə klik edildikdə açılır.
// Gələcəkdə İddia Ərizəsi üçün genişlənə bilər (sk/hes açar saxlanılır).

public class MehkemeHazirliqZaminDto
{
    public int      Sira        { get; set; }   // 1..5
    public string   Ad          { get; set; } = "";
    public string?  Fin         { get; set; }
    public string?  Olke        { get; set; }
    public DateTime? DogumTarixi { get; set; }
    public string?  DogumYeri   { get; set; }
    public string?  Unvan       { get; set; }
    public string?  Telefon     { get; set; }
    public decimal? BorcYuku    { get; set; }   // aylıq borc yükü
    public decimal? Gelir       { get; set; }   // aylıq gəlir
}

public class MehkemeHazirliqSetirDto
{
    public string?  Sk  { get; set; }           // subschkre
    public string?  Hes { get; set; }           // licschkre

    public string   Ad           { get; set; } = "";
    public DateTime? VTar        { get; set; }   // v_tar (date_open)
    public decimal? VerKr        { get; set; }
    public decimal? Esas         { get; set; }
    public decimal? Vk           { get; set; }
    public decimal? Faiz         { get; set; }
    public decimal? VkFaiz       { get; set; }
    public decimal? ToplamVkBorc { get; set; }
    public decimal? Ayliq        { get; set; }
    public decimal? UcAyliqCemi  { get; set; }   // uc_ayliq_cemi (3 aylıq cəmi)
    public DateTime? SonOdTar    { get; set; }   // son_od_tar (lastpaymentdate)
    public string?  Item01       { get; set; }
    public string?  Item02       { get; set; }

    public List<MehkemeHazirliqZaminDto> Zaminlar { get; set; } = new();
}

public class MehkemeHazirliqViewDto
{
    public bool    SorguTapildi { get; set; }
    public string? Xeta         { get; set; }
    public List<MehkemeHazirliqSetirDto> Setirler { get; set; } = new();
}

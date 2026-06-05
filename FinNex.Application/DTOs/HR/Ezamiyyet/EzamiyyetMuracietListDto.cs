using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Ezamiyyet
{
    public class EzamiyyetMuracietListDto
    {
        public int      Id            { get; set; }
        public int      IsciId        { get; set; }
        public string   IsciTamAd     { get; set; } = null!;
        public string?  IsciSekil     { get; set; }
        public string?  IsciVezife    { get; set; }
        public string   Baslig        { get; set; } = null!;
        public int      MekanId       { get; set; }
        public string   MekanAd       { get; set; } = null!;
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi   { get; set; }
        public TimeSpan? BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati   { get; set; }
        public string?  SenedYolu     { get; set; }
        public string?  SenedAd       { get; set; }
        public string?  Qeyd          { get; set; }
        public EzamiyyetStatus Status { get; set; }
        public bool?    RehberTesdiq        { get; set; }
        public string?  RehberTamAd         { get; set; }
        public DateTime? RehberTesdiqTarixi { get; set; }
        public string?  RehberQeydi         { get; set; }
        public DateTime YaradilmaTarixi     { get; set; }

        public int GunSayi => (BitmeTarixi.Date - BaslamaTarixi.Date).Days + 1;

        public bool TamGun => BaslamaSaati == null && BitisSaati == null;
    }

    public class EzamiyyetMekanListDto
    {
        public int    Id    { get; set; }
        public string Ad    { get; set; } = null!;
        public bool   Aktiv { get; set; }
        public int    Sayi  { get; set; }
    }

    public class EzamiyyetStatistikDto
    {
        public int    IsciId    { get; set; }
        public string IsciTamAd { get; set; } = null!;
        public string? Departament { get; set; }
        public int    CemiMuraciet  { get; set; }
        public int    Tesdiqlendi   { get; set; }
        public int    Reddedildi    { get; set; }
        public int    Gozleyir      { get; set; }
        public int    CemiGun       { get; set; }
        public string? EnCoxMekan   { get; set; }
    }

    public class EzamiyyetFiltrDto
    {
        public int?      IsciId     { get; set; }
        public string?   IsciAd     { get; set; }
        public int?      MekanId    { get; set; }
        public EzamiyyetStatus? Status { get; set; }
        public DateTime? BaslangicTarix { get; set; }
        public DateTime? SonTarix       { get; set; }
        public int?      DepartamentId  { get; set; }
    }
}

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
        public string?  IsciSobe      { get; set; }
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
        public string?   GeriDonusQeydi      { get; set; }
        public DateTime? CihazCixisVaxti    { get; set; }
        public DateTime? CihazQayidisVaxti  { get; set; }
        public DateTime  YaradilmaTarixi    { get; set; }

        public int GunSayi => (BitmeTarixi.Date - BaslamaTarixi.Date).Days + 1;

        public bool TamGun => BaslamaSaati == null && BitisSaati == null;

        // Faktiki cihaz müddəti (saat) — yalnız EYNİ gün çıxış+qayıdış olduqda hesablanır.
        // Çoxgünlük səfərlərdə saat metrikası mənasızdır (gün ilə ölçülür) → null.
        public double? FaktikiSaat =>
            (CihazCixisVaxti.HasValue && CihazQayidisVaxti.HasValue
             && CihazQayidisVaxti.Value > CihazCixisVaxti.Value
             && CihazQayidisVaxti.Value.Date == CihazCixisVaxti.Value.Date)
                ? Math.Round((CihazQayidisVaxti.Value - CihazCixisVaxti.Value).TotalHours, 2)
                : (double?)null;
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

    // Rəhbər/HR üçün işçi-qruplu ezamiyyət izləmə (cəmi, gün, faktiki saat + detay)
    public class EzamiyyetIsciIzlemeDto
    {
        public int      IsciId        { get; set; }
        public string   IsciAdSoyad   { get; set; } = null!;
        public string?  IsciSekil     { get; set; }
        public string   SobeAdi       { get; set; } = "-";
        public string?  VezifeAdi     { get; set; }

        public int      CemiEzam         { get; set; }
        public int      TesdiqlenibSayi  { get; set; }
        public int      GozlemeSayi      { get; set; }
        public int      ReddSayi         { get; set; }
        public int      CemiGun          { get; set; }

        // Eyni gün çıxış+qayıdışı olan səfərlərin faktiki saatlarının cəmi
        public double   FaktikiSaat      { get; set; }

        public DateTime? SonEzamTarixi   { get; set; }

        public List<EzamiyyetMuracietListDto> Ezamlar { get; set; } = new();
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

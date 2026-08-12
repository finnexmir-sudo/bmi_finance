namespace FinNex.Application.DTOs.Hevale;

// BMI (Oracle) → FinNex (SQL Server) həvalə jurnalı idxalı üçün DTO-lar.
// Məktub idxalı ilə eyni şablon (MektubImportDtos) — fərq yalnız jurnal adlarındadır.

// Bir il üzrə vəziyyət: Oracle-da neçə sətir var, FinNex-ə neçəsi gəlib.
public class HevaleImportIlDto
{
    public int? Il        { get; set; }   // null = Oracle-da tarixi boş olan sətirlər
    public int  OracleSay { get; set; }
    public int  FinNexSay { get; set; }

    // Hələ gəlməmiş sətir sayı (mənfi olarsa 0 — FinNex-də əlavə qeyd ola bilər)
    public int  Catismayan => OracleSay - FinNexSay > 0 ? OracleSay - FinNexSay : 0;
    public bool Tamamdir   => Catismayan == 0;
}

// İki jurnalın ümumi vəziyyəti (idxal səhifəsi bunu göstərir)
public class HevaleImportVeziyyetDto
{
    public List<HevaleImportIlDto> Geden { get; set; } = new();
    public List<HevaleImportIlDto> Gelen { get; set; } = new();

    public int GedenOracle => Geden.Sum(x => x.OracleSay);
    public int GedenFinNex => Geden.Sum(x => x.FinNexSay);
    public int GelenOracle => Gelen.Sum(x => x.OracleSay);
    public int GelenFinNex => Gelen.Sum(x => x.FinNexSay);
}

// Bir ilin idxal nəticəsi
public class HevaleImportNeticeDto
{
    public string Jurnal  { get; set; } = "";   // "geden" | "gelen"
    public int?   Il      { get; set; }
    public int    Oxunan  { get; set; }         // Oracle-dan gələn sətir
    public int    Elave   { get; set; }         // FinNex-ə yazılan
    public int    Kecilen { get; set; }         // artıq mövcud olduğu üçün keçilən
    public int    Xetali  { get; set; }         // açar boş / çevrilmə xətası (sətir atıldı)

    // Oxunan say limitə bərabərdirsə, Oracle sətirləri kəsilmiş ola bilər
    public bool LimiteCatdi { get; set; }
}

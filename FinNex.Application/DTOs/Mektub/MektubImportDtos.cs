namespace FinNex.Application.DTOs.Mektub;

// BMI (Oracle) → FinNex (SQL Server) məktub jurnalı idxalı üçün DTO-lar.

// Bir il üzrə vəziyyət: Oracle-da neçə sətir var, FinNex-ə neçəsi gəlib.
public class MektubImportIlDto
{
    public int? Il         { get; set; }   // null = Oracle-da ili boş olan sətirlər
    public int  OracleSay  { get; set; }
    public int  FinNexSay  { get; set; }

    // Hələ gəlməmiş sətir sayı (mənfi olarsa 0 — FinNex-də əlavə qeyd ola bilər)
    public int  Catismayan => OracleSay - FinNexSay > 0 ? OracleSay - FinNexSay : 0;
    public bool Tamamdir   => Catismayan == 0;
}

// İki jurnalın ümumi vəziyyəti (idxal səhifəsi bunu göstərir)
public class MektubImportVeziyyetDto
{
    public List<MektubImportIlDto> Xaric { get; set; } = new();
    public List<MektubImportIlDto> Daxil { get; set; } = new();

    public int XaricOracle => Xaric.Sum(x => x.OracleSay);
    public int XaricFinNex => Xaric.Sum(x => x.FinNexSay);
    public int DaxilOracle => Daxil.Sum(x => x.OracleSay);
    public int DaxilFinNex => Daxil.Sum(x => x.FinNexSay);
}

// Bir ilin idxal nəticəsi
public class MektubImportNeticeDto
{
    public string Jurnal  { get; set; } = "";   // "xaric" | "daxil"
    public int?   Il      { get; set; }
    public int    Oxunan  { get; set; }         // Oracle-dan gələn sətir
    public int    Elave   { get; set; }         // FinNex-ə yazılan
    public int    Kecilen { get; set; }         // artıq mövcud olduğu üçün keçilən
    public int    Xetali  { get; set; }         // çevrilmə xətası (sətir atıldı)

    // Oxunan say limitə bərabərdirsə, Oracle sətirləri kəsilmiş ola bilər
    public bool LimiteCatdi { get; set; }
}

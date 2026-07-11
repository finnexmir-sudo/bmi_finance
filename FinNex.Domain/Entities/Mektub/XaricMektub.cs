namespace FinNex.Domain.Entities.Mektub
{
    /// <summary>
    /// Xaric olan məktub jurnalı (BMI odb.xaric_mektub qarşılığı).
    /// PK = Id (FinNex identity); BMI sütunları data kimi saxlanılır ki, Oracle
    /// datası 1:1 yüklənə bilsin. Yeni qeydlərdə QeyNom il üzrə max+1 verilir,
    /// Icraci (mətn) cari istifadəçinin adı, YaradanIcraciId (BaseEntity) isə
    /// sahiblik üçün AppUser id-dir.
    /// </summary>
    public class XaricMektub : BaseEntity
    {
        public long?     Kod        { get; set; }   // KOD — köhnə açar (Oracle datası)
        public string?   QeyNom     { get; set; }   // QEY_NOM — Qeydiyyat №
        public string?   GonYer     { get; set; }   // GON_YER — göndərilən yer (təyinat)
        public DateTime? Tarix      { get; set; }   // TARIX — tarix
        public string?   QisaMez    { get; set; }   // QISA_MEZ — qısa məzmun
        public string?   Icraci     { get; set; }   // ICRACI — icraçı (mətn)
        public string?   Dubl       { get; set; }   // DUBL — dublikat işarəsi
        public string?   MektubMetn { get; set; }   // MEKTUB_METN — məktubun tam mətni (CLOB)
        public int?      Il         { get; set; }   // IL — il
        public string?   FaylYolu   { get; set; }   // Yeni yükləmələr üçün DMS nisbi yolu (FinNex)
    }
}

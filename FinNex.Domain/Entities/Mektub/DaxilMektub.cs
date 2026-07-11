namespace FinNex.Domain.Entities.Mektub
{
    /// <summary>
    /// Daxil olan məktub jurnalı (BMI odb.daxil_mektub qarşılığı).
    /// PK = Id (FinNex identity); BMI sütunları data kimi saxlanılır ki, Oracle
    /// datası yüklənə bilsin. Yeni qeydlərdə Nom/Nom1 il üzrə max+1 verilir,
    /// MekUnvan (icraçı nömrəsi) cari istifadəçinin IcraciNo-su, YaradanIcraciId
    /// (BaseEntity) isə sahiblik üçün AppUser id-dir.
    /// </summary>
    public class DaxilMektub : BaseEntity
    {
        public int?      Nom      { get; set; }   // NOM — daxili/köhnə nömrə
        public int?      Nom1     { get; set; }   // NOM1 — Qeydiyyat № (göstərilən)
        public DateTime? DaxTarix { get; set; }   // DAX_TARIX — daxil olma tarixi
        public string?   IdareAdi { get; set; }   // IDARE_ADI — göndərən idarə/təşkilat
        public DateTime? GonTarix { get; set; }   // GON_TARIX — göndərilmə tarixi
        public string?   DaxNom   { get; set; }   // DAX_NOM — məktub №
        public int?      MekUnvan { get; set; }   // MEK_UNVAN — icraçı nömrəsi (Isci.IcraciNo)
        public int?      Il       { get; set; }   // IL — il
        public byte[]?   Mezmun   { get; set; }   // MEZMUN — köhnə binar məzmun (Oracle datası, oxu)
        public string?   FaylYolu { get; set; }   // Yeni yükləmələr üçün DMS nisbi yolu (FinNex)
    }
}

namespace FinNex.Domain.Entities.Hevale
{
    /// <summary>
    /// Gələn həvalə (pul köçürməsi) jurnalı — BMI odb.gelen_hevale qarşılığı.
    /// PK = Id (FinNex identity); BMI sütunları data kimi saxlanılır ki, Oracle
    /// datası 1:1 yüklənə bilsin. Yeni qeydlərdə HevNom il üzrə avtomatik,
    /// Icra (icraçı nömrəsi) cari istifadəçinin IcraciNo-su, YaradanIcraciId
    /// (BaseEntity) isə sahiblik üçün AppUser id-dir.
    /// </summary>
    public class GelenHevale : BaseEntity
    {
        public string?   HevNom   { get; set; }   // HEV_NOM — Həvalə № (açar)
        public string?   HesNom   { get; set; }   // HES_NOM — Hesab №
        public string?   Saa      { get; set; }   // SAA — Soyad Ad Ata
        public string?   TipRes   { get; set; }   // TIP_RES — rezident tipi
        public decimal?  Mebleg   { get; set; }   // MEBLEG — məbləğ
        public string?   ValTip   { get; set; }   // VAL_TIP — valyuta tipi
        public DateTime? Tarix    { get; set; }   // TARIX — tarix
        public string?   MenOlke  { get; set; }   // MEN_OLKE — mənşə ölkə
        public string?   HevTip   { get; set; }   // HEV_TIP — həvalə tipi
        public long?     Dec      { get; set; }   // DEC — Oracle NUMBER(10) (SQL-də [DEC])
        public string?   DecNom   { get; set; }   // DEC_NOM — bəyannamə №
        public string?   GelOlke  { get; set; }   // GEL_OLKE — gələn (mənbə) ölkə
        public string?   GonTip   { get; set; }   // GON_TIP — göndərən tipi
        public string?   AlBank   { get; set; }   // AL_BANK — alan bank
        public short?    Icra     { get; set; }   // ICRA — icraçı (Isci.IcraciNo)
        public string?   FaylYolu { get; set; }   // Yeni yükləmələr üçün DMS nisbi yolu (FinNex — Oracle-da yoxdur)
    }
}

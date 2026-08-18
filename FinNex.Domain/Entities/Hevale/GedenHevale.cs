namespace FinNex.Domain.Entities.Hevale
{
    /// <summary>
    /// Gedən həvalə (pul köçürməsi) jurnalı — BMI odb.geden_hevale qarşılığı.
    /// PK = Id (FinNex identity); BMI sütunları data kimi saxlanılır ki, Oracle
    /// datası 1:1 yüklənə bilsin. Yeni qeydlərdə HevNom il üzrə avtomatik,
    /// Icra (icraçı nömrəsi) cari istifadəçinin IcraciNo-su, YaradanIcraciId
    /// (BaseEntity) isə sahiblik üçün AppUser id-dir.
    /// </summary>
    public class GedenHevale : BaseEntity
    {
        public string?   HevNom     { get; set; }   // HEV_NOM — Həvalə № (açar, YY-T-N)
        public string?   HesNom     { get; set; }   // HES_NOM — Hesab №
        public string?   Saa        { get; set; }   // SAA — Soyad Ad Ata
        public string?   TipRes     { get; set; }   // TIP_RES — rezident tipi (fiziki/hüquqi)
        public decimal?  Mebleg     { get; set; }   // MEBLEG — məbləğ
        public string?   ValTip     { get; set; }   // VAL_TIP — valyuta tipi
        public DateTime? Tarix      { get; set; }   // TARIX — tarix
        public string?   MenOlke    { get; set; }   // MEN_OLKE — mənşə ölkə
        public string?   ContracNom { get; set; }   // CONTRAC_NOM — müqavilə №
        public string?   DeclarNom  { get; set; }   // DECLAR_NOM — bəyannamə №
        public short?    Arayis     { get; set; }   // ARAYIS — arayış (bayraq)
        public string?   Olke       { get; set; }   // OLKE — təyinat ölkə
        public string?   HevTip     { get; set; }   // HEV_TIP — həvalə tipi
        public string?   GonTip     { get; set; }   // GON_TIP — göndərən tipi
        public string?   AlBank     { get; set; }   // AL_BANK — alan bank
        public short?    Icra       { get; set; }   // ICRA — icraçı (Isci.IcraciNo)
        public string?   FaylYolu   { get; set; }   // Yeni yükləmələr üçün DMS nisbi yolu (FinNex — Oracle-da yoxdur)

        /// <summary>
        /// Bu sətri hansı «Pul köçürməsi» (<c>Kocurme</c>) yaratdı — FinNex sahəsi,
        /// BMI-də qarşılığı YOXDUR (18.08.2026).
        ///
        /// <c>null</c> = BMI idxalı və ya Gedən həvalə səhifəsindən əl ilə yazılan sətir.
        /// Dolu   = Əməliyyat → Pul köçürməsi tərəfindən yaradılıb; köçürmə redaktə/
        ///          silinəndə bu sətir də ona uyğun yenilənir.
        ///
        /// NİYƏ NÖMRƏ İLƏ (HevNom) BAĞLAMIRIQ: mövcud datada nömrə hələ unikal deyil —
        /// test məqsədli <c>Kocurme</c> «26-T-1» ilə real BMI idxalı «26-T-1» üst-üstə
        /// düşür. Nömrə ilə bağlasaq test köçürməsinin silinməsi REAL jurnal sətrini
        /// silərdi. Açıq bağ bu riski tamamilə aradan qaldırır.
        /// </summary>
        public int?      KocurmeId  { get; set; }
    }
}

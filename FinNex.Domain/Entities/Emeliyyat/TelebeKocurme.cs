namespace FinNex.Domain.Entities.Emeliyyat
{
    /// <summary>
    /// Tələbə köçürməsi (təhsil haqqı) — BMI "Telebe" forması.
    /// Tələbə məlumatı + kurs/komissiya əsasında 3 debet/kredit muhasibat sətri
    /// yaradılır. Komissiya = Mebleg × Kurs × XH / 100 (minimum 0.5).
    /// Hesablar (35025/45023/45011/67013) standart, dəyişdirilə bilər.
    /// </summary>
    public class TelebeKocurme : BaseEntity
    {
        public string?   HevaleNo    { get; set; }   // Həvalə No (G/H)
        public DateTime? Tarix       { get; set; }
        public string?   Adi         { get; set; }   // tələbə adı
        public string?   Passport    { get; set; }
        public decimal?  Mebleg      { get; set; }
        public string?   BmiFilial   { get; set; }   // BMİ filial
        public string?   RefNo       { get; set; }   // REF
        public string?   UniAd       { get; set; }   // universitet adı
        public string?   AlanBank    { get; set; }   // default "Kapital"
        public string?   TelebeKursu { get; set; }   // "Tələbə kurs" (təhsil dövrü / t/h)
        public decimal?  XH          { get; set; }   // X/H (komissiya faizi), default 0.1
        public decimal?  Kurs        { get; set; }   // valyuta kursu, default 1.68
        public decimal?  Komissiya   { get; set; }   // hesablanmış komissiya (min 0.5)

        // Hesablar (standart, jurnal üçün saxlanılır)
        public string?   Hes35025    { get; set; }
        public string?   Hes45023    { get; set; }
        public string?   Hes45011    { get; set; }
        public string?   Hes67013    { get; set; }

        public short?    Icra        { get; set; }   // icraçı (Isci.IcraciNo)
    }
}

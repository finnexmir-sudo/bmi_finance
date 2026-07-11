namespace FinNex.Domain.Entities.Emeliyyat
{
    /// <summary>
    /// Əməliyyat departamenti — pul köçürməsi əməliyyatı (BMI "Pul köçürməsi" və
    /// "Tələbə köçürməsi" formaları). Novu sahəsi ilə ayrılır ("Pul" / "Telebe").
    /// Göndərən və alan şəxs, valyuta seçimi, bank məlumatı, Həvalə № (YY-T-N).
    /// Icra — cari istifadəçinin IcraciNo-su; YaradanIcraciId (BaseEntity) sahiblik üçün.
    /// </summary>
    public class Kocurme : BaseEntity
    {
        public string?   Novu     { get; set; }   // "Pul" / "Telebe"
        public string?   HevaleNo { get; set; }   // Həvalə № (YY-T-N)
        public DateTime? Tarix    { get; set; }

        // Göndərən
        public string?   GonderenAd       { get; set; }
        public string?   GonderenSoyad    { get; set; }
        public string?   GonderenAtaAd    { get; set; }
        public string?   GonderenPassport { get; set; }
        public string?   GonderenTelefon  { get; set; }

        // Alan
        public string?   AlanAd       { get; set; }
        public string?   AlanSoyad    { get; set; }
        public string?   AlanAtaAd    { get; set; }
        public string?   AlanPassport { get; set; }
        public string?   AlanTelefon  { get; set; }

        // Məbləğ və valyuta
        public decimal?  Mebleg           { get; set; }
        public decimal?  RialCbar         { get; set; }   // Rial (Mərkəzi Bank kursu)
        public decimal?  ValyutaCbar      { get; set; }   // Valyuta (Mərkəzi Bank kursu)
        public decimal?  IranRial         { get; set; }
        public string?   MedaxilValyuta   { get; set; }   // USD / Avro / AZN
        public string?   KocurulenValyuta { get; set; }   // USD / Avro / Rial

        // Seçim: Hesab açmadan / Hesab və mədaxil / Hesabdan
        public string?   Secim { get; set; }

        // Bank məlumatları
        public string?   BankAd    { get; set; }
        public string?   Filial    { get; set; }
        public string?   AlanHesab { get; set; }

        public string?   Elave  { get; set; }
        public string?   Meqsed { get; set; }
        public string?   Qeyd   { get; set; }

        public short?    Icra   { get; set; }   // icraçı (Isci.IcraciNo)
    }
}

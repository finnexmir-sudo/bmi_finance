namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Cihazdan (ADMS) gələn hər XAM giriş/çıxış oxuması.
    ///
    /// Davamiyyət cədvəli günə yalnız ilk giriş + son çıxışı saxlayır — aralıq
    /// punch-lar (icazəyə çıxış, qayıdış) itir. Bu cədvəl BÜTÜN oxumaları saxlayır
    /// ki, icazə/ezamiyyət çıxış-qayıdışı sonradan da (təsdiqdən sonra, qayıdan
    /// işçidə də) dəqiq bərpa oluna bilsin.
    /// </summary>
    public class CihazOxuma : BaseEntity
    {
        public int IsciId { get; set; }

        // Oxumanın tam tarix-vaxtı
        public DateTime Vaxt { get; set; }

        // Cihazın bildirdiyi nov kodu (0/4 = giriş, digər = çıxış) — yalnız məlumat üçün
        public int Nov { get; set; }

        // ADMS-in giriş kimi yozub-yozmadığı (nov 0/4)
        public bool Girisdir { get; set; }
    }
}

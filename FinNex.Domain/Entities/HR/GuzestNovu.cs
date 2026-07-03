namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Güzəştin növü — hansı şəxsi faktla bağlıdır. Vergi hesablaması məbləği
    /// (Guzest.Mebleg) işlədir; məzuniyyət modulu isə bu növə baxaraq öz nəticəsini
    /// çıxarır (məs. Elillik → əsas məzuniyyət 42 gün, M.119). Belə əlillik "adla" yox,
    /// "tiplə" tanınır — güzəşt adı dəyişsə belə məzuniyyət düzgün işləyir.
    /// </summary>
    public enum GuzestNovu
    {
        Diger         = 0,  // adi vergi güzəşti — məzuniyyətə təsir etmir
        Elillik       = 1,  // əlillik — məzuniyyət balansında 42 gün
        MecburiKockun = 2   // məcburi köçkün — yalnız vergi
    }
}

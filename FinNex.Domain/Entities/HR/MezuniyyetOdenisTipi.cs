namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Məzuniyyət ödənişinin vaxtı.
    /// AySonuOdenis — maaşla birlikdə, həmin ayın hesablamasına daxil olur (default).
    /// QabaqcadanOdenis — HR təsdiqindən sonra Mühasib ayrıca ödəyir, maaşda yalnız
    /// kəsinti əks olunur (ödəniş yox).
    /// </summary>
    public enum MezuniyyetOdenisTipi
    {
        AySonuOdenis = 1,
        QabaqcadanOdenis = 2
    }
}

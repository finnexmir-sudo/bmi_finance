namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// BayramGunu cədvəlindəki qeydin növü.
    /// </summary>
    public enum GunTipi
    {
        /// <summary>
        /// İstirahət günü (bayram, milli tətil, və s.).
        /// Default — mövcud qeydlər bu tipə aiddir.
        /// </summary>
        Bayram = 1,

        /// <summary>
        /// İş günü (override) — şənbə və ya bazar günü,
        /// lakin həmin gün işə çıxılır.
        /// </summary>
        IsGunu = 2
    }
}

namespace FinNex.Domain.Entities.HR
{
    public enum QaraJetonBorcuStatus
    {
        Gozleyir = 1,
        Odenildi = 2,
        MuddetiBitib = 3,

        /// <summary>Borcu yaradan Qara Jeton ləğv edilib (08.10.2026) — il sonu
        /// bağışlanması (MuddetiBitib) ilə QARIŞDIRMA, səbəb fərqlidir.</summary>
        LegvEdildi = 4
    }
}

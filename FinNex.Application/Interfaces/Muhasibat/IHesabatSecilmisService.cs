namespace FinNex.Application.Interfaces.Muhasibat
{
    /// <summary>
    /// "Maliyyə Hesabatları" səhifəsindəki ★ Seçilmişlər — hər AppUser özünə aid
    /// bir siyahı saxlayır (29.10.2026). Kataloqun özü (ad/təsvir/link) burada DEYİL —
    /// yalnız "bu kod hansı istifadəçidə seçilib" məlumatı.
    /// </summary>
    public interface IHesabatSecilmisService
    {
        /// <summary>Bu istifadəçinin seçdiyi bütün hesabat kodları (aktiv, silinməmiş).</summary>
        Task<HashSet<string>> SeciliKodlarAsync(int appUserId);

        /// <summary>
        /// Seçimi dəyişir (toggle): seçilməyibsə seçir, seçilibsə geri götürür.
        /// Qaytarır: yeni vəziyyət (true = indi seçilidir).
        /// </summary>
        Task<bool> SecVeyaCixarAsync(int appUserId, string hesabatKod);
    }
}

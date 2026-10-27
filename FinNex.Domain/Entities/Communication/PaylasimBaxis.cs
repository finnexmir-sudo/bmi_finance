using FinNex.Domain;

namespace FinNex.Domain.Entities.Communication
{
    /// <summary>
    /// Hər işçinin (AppUser) "Paylaşımlar" lentini SON NƏ VAXT gördüyünü saxlayır
    /// (27.10.2026) — sidebar-dakı "yeni paylaşım" sayğacının mənbəyidir. TƏK sətir/
    /// istifadəçi (`PaylasimSonHedefKitlesi` ilə EYNİ naxış), /User/Paylasim səhifəsi
    /// hər açılanda üzərinə yazılır.
    /// </summary>
    public class PaylasimBaxis : BaseEntity
    {
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public DateTime SonBaxisTarixi { get; set; }
    }
}

using FinNex.Domain;

namespace FinNex.Domain.Entities.Communication
{
    /// <summary>
    /// Admin-in "Seçilmiş işçilər" rejimində SON işlətdiyi hədəf siyahısı (27.10.2026) —
    /// hər admin (AppUser) üçün TƏK sətir, hər publish-də üzərinə yazılır ki, növbəti
    /// paylaşımda eyni siyahı əvvəlcədən işarəli gəlsin ("bir klik ilə artırardım").
    /// Yalnız UX-rahatlıq keşidir — `MelumatHedef`-in əvəzinə DEYİL, ona əlavədir.
    /// </summary>
    public class PaylasimSonHedefKitlesi : BaseEntity
    {
        public int AdminAppUserId { get; set; }
        public AppUser AdminAppUser { get; set; } = null!;

        /// <summary>Vergüllə ayrılmış Isci.Id siyahısı (məs. "3,7,12").</summary>
        public string IsciIdlerCsv { get; set; } = "";
    }
}

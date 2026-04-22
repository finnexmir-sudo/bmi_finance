using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Kredit komitəsi üzvü. Komitə fiziki olaraq yığışır və qərar verir — burada
    /// yalnız kimlərin üzv olduğu saxlanır. Protokol imzalandıqda hansı üzvün
    /// imzası olduğu KreditQerarImza-da qeydə alınır.
    /// Gələcəkdə "Rol" sahəsi səsvermə qaydalarını (sədr, üzv) dəstəkləmək üçün
    /// istifadə oluna bilər.
    /// </summary>
    public class KomiteUzvu : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public KomiteUzvuRolu Rol { get; set; } = KomiteUzvuRolu.Uzv;

        public DateTime AktivdirFrom { get; set; }
        public DateTime? AktivdirTo { get; set; }
        public string? Qeyd { get; set; }
    }

    public enum KomiteUzvuRolu
    {
        Uzv = 0,
        Sedr = 1,
        SedrMuavini = 2,
        Katib = 3
    }
}

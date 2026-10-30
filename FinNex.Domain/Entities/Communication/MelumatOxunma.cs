using FinNex.Domain;

namespace FinNex.Domain.Entities.Communication
{
    /// <summary>
    /// Bir işçinin (AppUser) konkret bir Paylaşımı HƏQİQƏTƏN AÇIB OXUDUĞUNU qeyd edir
    /// (30.10.2026) — Admin-in "kimlər oxudu, maraq varmı" sualına cavab üçün.
    /// `PaylasimBaxis`-dan FƏRQLİDİR: o, "lentə SON NƏ VAXT baxdı" (tək sətir/işçi,
    /// sayğac üçün), bu isə "HANSI KONKRET postu HƏQİQƏTƏN açıb oxudu" (bir sətir =
    /// bir (Post, İşçi) cütü, unikal indekslə qorunur). Sətir YALNIZ işçi sətri
    /// KLİKLƏYİB tam mətni açanda yaranır — sadəcə lentdə görünmək "oxumaq" sayılmır
    /// (bax User/Paylasim/Index.cshtml-dəki `.py-row-summary` klik handler-i).
    /// </summary>
    public class MelumatOxunma : BaseEntity
    {
        public int MelumatId { get; set; }
        public Melumat Melumat { get; set; } = null!;

        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public DateTime OxunmaTarixi { get; set; }
    }
}

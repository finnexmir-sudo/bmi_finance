namespace FinNex.Domain.Entities.Communication
{
    /// <summary>
    /// Paylaşımlar üçün açar-cədvəl kateqoriya siyahısı (27.10.2026) — "KocurmeSenedNovleri"
    /// ilə eyni naxış: boş başlaya bilər, admin "+ Yeni" ilə əlavə edir, silinmir (deaktiv
    /// edilir — `Aktivdir=false`), köhnə paylaşımların kateqoriyası itməsin.
    /// </summary>
    public class PaylasimKateqoriyasi : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public bool Aktivdir { get; set; } = true;
    }
}

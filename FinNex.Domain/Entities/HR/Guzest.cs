namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Vergi güzəşti kataloqu (HR tərəfindən idarə olunur).
    /// Məsələn: Qaçqın statusu — 100 ₼, Tək valideyn — 50 ₼, Əlil III qrup — 200 ₼.
    ///
    /// Məzənnə Azərbaycan Vergi Məcəlləsinin uyğun maddələrinə görə təyin olunur.
    /// İşçi bir neçə güzəştə aid ola bilər — hesablamada yalnız ən böyüyü götürülür.
    /// </summary>
    public class Guzest : BaseEntity
    {
        public string Ad { get; set; } = string.Empty;
        public decimal Mebleg { get; set; }
        public string? Madde { get; set; } // Məs: "Vergi Məcəlləsi, maddə 102.1.6"
        public bool Aktivdir { get; set; } = true;

        public ICollection<IsciGuzest> IsciGuzestler { get; set; } = new List<IsciGuzest>();
    }
}

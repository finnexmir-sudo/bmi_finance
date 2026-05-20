using FinNex.Domain.Entities;

namespace FinNex.Domain.Entities.AI
{
    public class HRDaxiliQayda : BaseEntity
    {
        public string Kod      { get; set; } = "";   // HR-001, HR-002 ...
        public string Ad       { get; set; } = "";
        public string Mezmun   { get; set; } = "";
        public string Kateqoriya { get; set; } = ""; // Davamiyyət, Məzuniyyət, Əmək haqqı...
        public int    Versiya  { get; set; } = 1;
        public bool   Aktiv    { get; set; } = true; // cari versiya
        public int    YazilanKimId { get; set; }
        public AppUser? YazilanKim { get; set; }
    }
}

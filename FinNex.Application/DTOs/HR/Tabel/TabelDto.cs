namespace FinNex.Application.DTOs.HR.Tabel
{
    public class TabelAyDto
    {
        public int Il      { get; set; }
        public int Ay      { get; set; }
        public int GunSayi { get; set; }
        /// <summary>Bayram ərəfəsi olan iş günlərinin nömrələri (1-indeksli)</summary>
        public HashSet<int> BayramErtesiGunler { get; set; } = new();
        public List<TabelIsciSatiri> Satirlar { get; set; } = new();
    }

    public class TabelIsciSatiri
    {
        public string IsciAd     { get; set; } = null!;
        public string Vezife     { get; set; } = null!;
        public string Departament { get; set; } = null!;
        /// <summary>
        /// Hər gün üçün kod: "8","7","6" — iş saatı; "İ" — istirahət; "B" — bayram;
        /// "M" — məzuniyyət; "X" — xəstəlik; "E" — ezamiyyət;
        /// "" (boş) — işçi həmin gün işləmirdi (işdən ayrıldıqdan sonrakı günlər)
        /// </summary>
        public List<string> GunKodlari  { get; set; } = new();
        public int IsGunSayi     { get; set; }
        public int IsSaatSayi    { get; set; }
        public int MezuniyyetGun { get; set; }
        public int EzamiyyetGun  { get; set; }
        public int XestelikGun   { get; set; }
    }
}

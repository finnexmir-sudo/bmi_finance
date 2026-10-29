namespace FinNex.Application.DTOs.Muhasibat
{
    /// <summary>
    /// "Maliyyə Hesabatları" səhifəsindəki bir kart/sətir — kataloq + istifadəçinin
    /// ★ seçimi birləşdirilmiş görüntü modeli (29.10.2026).
    /// </summary>
    public class HesabatKartDto
    {
        public string Kod { get; set; } = "";
        public string Ad { get; set; } = "";
        public string Tesvir { get; set; } = "";

        /// <summary>Filtr pill-i: "Maliyyə" / "Kreditlər" / "Risk" / "Xərclər".</summary>
        public string Kateqoriya { get; set; } = "";

        public string Ikon { get; set; } = "bi-file-earmark-text";

        public string Area { get; set; } = "";
        public string Controller { get; set; } = "";
        public string Action { get; set; } = "";

        public bool Secilib { get; set; }
    }
}

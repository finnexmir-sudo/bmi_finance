namespace FinNex.Application.DTOs.Muhasibat
{
    /// <summary>
    /// DSMF Vahid Hesabat Forması — hub səhifəsindəki bir komponent sətri
    /// (29.10.2026, istifadəçi tələbi: "Vahid hesabat adını yaz, hazırladıqlarını
    /// onun içində göstər, altısını da"). 6 rəsmi əlavədən hansının hazır,
    /// hansının hələ qurulmadığını göstərir — statik siyahı, kataloq prinsipi.
    /// </summary>
    public class DsmfVahidKomponentDto
    {
        public string ResmiAd { get; set; } = "";
        public string Tesvir { get; set; } = "";
        public bool Hazirdir { get; set; }

        /// <summary>Hazırdırsa keçid üçün — boşdursa (hələ qurulmayıbsa) keçid göstərilmir.</summary>
        public string? Action { get; set; }
    }
}

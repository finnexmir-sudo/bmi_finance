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

        /// <summary>
        /// Bölmə 2/3/4/5 EYNİ `Bolme2345` action-unu paylaşır — bu sahə ona
        /// `bolmeNo` route dəyəri kimi ötürülür ki, "hansını AÇDISA O EXCELƏ
        /// AİD OLANI GÖSTƏR" (29.10.2026, istifadəçi tələbi) işləsin: hər
        /// sətir YALNIZ öz bölməsini açır, 4-ü BİRLİKDƏ yox. Digər komponentlər
        /// (Hissə 1/3) üçün null — onların öz, ayrıca action-u var.
        /// </summary>
        public int? BolmeNo { get; set; }
    }
}

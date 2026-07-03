namespace FinNex.Domain.Entities.HR
{
    public enum DavamiyyetStatus
    {
        Isde = 1,
        Gecikme = 2,
        Qayib = 3,
        Icazeli = 4,
        Xestelik = 5,
        Ezamiyyet = 6,
        /// <summary>
        /// Hərbi çağırış / dövlət vəzifəsi — ödənişli, balansdan düşmür,
        /// maaş modulunda MaasdanKes=false saxlanılır.
        /// </summary>
        OdenisDovletVezifesi = 7,

        /// <summary>
        /// Öz hesabına (ödənişsiz) məzuniyyət. Qayıb DEYİL (cəza/jeton yox).
        /// Maaşda həmin günlərin baza haqqı çıxılır, əvəzinə heç bir ödəniş
        /// (məzuniyyət/xəstəlik haqqı) əlavə olunmur.
        /// </summary>
        OdenissizMezuniyyet = 8
    }

}

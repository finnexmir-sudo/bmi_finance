namespace FinNex.Domain.Entities.HR
{
    public enum MezuniyyetNovu
    {
        Illik = 1,
        Xestelik = 2,
        Ezamiyyet = 3,
        /// <summary>
        /// Hərbi çağırış, məhkəmə şahidliyi və s. dövlət vəzifələri.
        /// Balansdan çıxılmır, maaşdan kəsilmir — 100% ödənişli xüsusi status.
        /// </summary>
        DovletVezifelerininIcrasi = 4,

        /// <summary>
        /// Öz hesabına (ödənişsiz) məzuniyyət — Ə.M. 129.
        /// Balansa DƏYMİR, staja sayılmır. Maaşda həmin günlərin baza haqqı
        /// çıxılır, amma məzuniyyət/xəstəlik haqqı ÖDƏNİLMİR (ödənişsizdir).
        /// İşçi xahişilə (adi təsdiq) və ya HR birbaşa (bank göndərir) yaradıla bilər.
        /// </summary>
        OzHesabina = 5
    }

}

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
        DovletVezifelerininIcrasi = 4
    }

}

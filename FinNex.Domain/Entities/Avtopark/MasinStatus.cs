namespace FinNex.Domain.Entities.Avtopark
{
    /// <summary>
    /// Maşının istifadəyə yararlılığı. Yalnız <see cref="Aktiv"/> maşına
    /// müraciət yazıla bilər — «Təmirdə» və ya «İstifadədən çıxıb» maşın
    /// müraciət formasının açılan siyahısında görünmür.
    /// </summary>
    public enum MasinStatus
    {
        Aktiv = 1,
        Temirde = 2,
        IstifadedenCixib = 3
    }
}

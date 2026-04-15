namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// QabaqcadanOdenis seçilmiş məzuniyyət üçün Mühasib tərəfindən idarə olunan
    /// ödəniş statusu. AySonuOdenis halında TetbiqEdilmir qalır.
    /// </summary>
    public enum MezuniyyetOdenisStatus
    {
        TetbiqEdilmir = 0,
        Gozleyir = 1,
        Odenilib = 2
    }
}

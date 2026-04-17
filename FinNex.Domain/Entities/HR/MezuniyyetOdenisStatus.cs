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
        Odenilib = 2,
        // Mühasib ödənişi təsdiqləyib, faktiki bank köçürməsi məzuniyyətdən
        // bir iş günü əvvəl (PlanliOdenisTarixi) icra olunur. Fon xidməti və
        // ya “İcra et” düyməsi ilə avtomatik Odenilib statusuna keçir.
        PlanliOdenis = 3
    }
}

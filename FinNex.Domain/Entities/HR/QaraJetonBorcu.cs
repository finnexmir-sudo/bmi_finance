namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Qara Jetonun nə müsbət (mükafat) jeton balansından, nə də işçinin cari
    /// ilin "36 Saat Hüququ" jetonundan ödənə bilməyən qalığı.
    ///
    /// Növbəti müsbət jeton verilərkən (JetonService.JetonVerAsync) əvvəlcə bu
    /// borc ödənilir — bax JetonService.QaraJetonBorcunuOdeAsync. Yalnız
    /// yaradıldığı İLİN sonuna qədər qüvvədədir: il dəyişəndə, hələ "Gözləyir"
    /// statusunda qalan borc ödənmədən BAĞIŞLANIR (Status=MuddetiBitib) —
    /// istifadəçi qərarı (07.10.2026).
    ///
    /// FK YOXDUR (qəsdən, sadə int) — CLAUDE.md-dəki "Mueyyenlesdiren/Yaradan"
    /// tipli sahələr nümunəsinə uyğun, cascade-path riskindən qaçmaq üçün.
    /// </summary>
    public class QaraJetonBorcu : BaseEntity
    {
        public int IsciId { get; set; }
        public int Il { get; set; }
        public decimal QalanSaat { get; set; }

        // Borcu yaradan Qara Jeton — IsciJetonu.Id (Nov=Menfi olan sətir).
        public int QaraJetonId { get; set; }

        public QaraJetonBorcuStatus Status { get; set; } = QaraJetonBorcuStatus.Gozleyir;
    }
}

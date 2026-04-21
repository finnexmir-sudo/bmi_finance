using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Təsdiqlənmiş kredit müraciəti üçün müştərinin banka gəlmə randevusu.
    /// Calendar məntiqi — gün seçiləndə o günə təyin edilənlər görünür.
    /// </summary>
    public class KreditRandevu : BaseEntity
    {
        public int KreditMuracietId { get; set; }
        public KreditMuraciet KreditMuraciet { get; set; } = null!;

        public DateTime Tarix { get; set; }      // Tarix + saat
        public int MuddetDeqiqe { get; set; } = 30;  // Təxmini müddət
        public string? Qeyd { get; set; }

        // Status
        public KreditRandevuStatus Status { get; set; } = KreditRandevuStatus.Planli;
        public DateTime? GelmeTarixi { get; set; }  // Faktiki gəldiyi an

        // Təyin edən
        public int TeyinEdenIsciId { get; set; }
        public Isci TeyinEdenIsci { get; set; } = null!;
    }

    public enum KreditRandevuStatus
    {
        Planli = 0,      // Təyin edilib, hələ gəlməyib
        Gelib = 1,       // Gəlib, qəbul olunub
        Gelmeyib = 2,    // Vaxtında gəlməyib
        Tehxir = 3,      // Başqa tarixə keçirilib
        Legv = 4         // Ləğv edilib
    }
}

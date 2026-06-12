namespace FinNex.Domain.Entities.HR
{
    // PID — bir məhkəmə işinə aid iclas (Excel-dəki təkrar tarix/saat sütunlarını əvəz edir).
    public class PidMehkemeIclas : BaseEntity
    {
        public int PidMehkemeIsiId { get; set; }
        public PidMehkemeIsi PidMehkemeIsi { get; set; } = null!;

        public DateTime? Tarix { get; set; }     // iclas tarixi
        public string? Saat { get; set; }        // saat — mətn (Excel-də 14.40, 9.30, 15.15 qarışıq)
        public string? Netice { get; set; }      // iclasın nəticəsi/qeyd
    }
}

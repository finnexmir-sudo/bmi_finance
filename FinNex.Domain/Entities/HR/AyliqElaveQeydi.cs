namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// İşçiyə müəyyən ay üçün təyin olunan əlavə ödənişlər — Bonus və Overtime.
    /// HR ayrıca "Aylıq Əlavə" səhifəsində daxil edir.
    /// Maaş hesablanana qədər dəyişdirilə/silinə bilər; hesablama mərhələsində
    /// MaasHesablamaService bu qeyddən Bonus və Overtime məbləğlərini oxuyur.
    /// Hər (İşçi, İl, Ay) üçün bir qeyd olur.
    /// </summary>
    public class AyliqElaveQeydi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }

        public decimal Bonus { get; set; }
        public decimal Overtime { get; set; }
    }
}

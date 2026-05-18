using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR;

public class GozlenilenXerc : BaseEntity
{
    public string Ad { get; set; } = "";
    public string? Tesvir { get; set; }

    public int KateqoriyaId { get; set; }
    public XercKateqoriyasi Kateqoriya { get; set; } = null!;

    public int? DepartamentId { get; set; }
    public Departament? Departament { get; set; }

    public decimal TəxminiMebleg { get; set; }
    public DateTime GozlenilenTarix { get; set; }

    public GozlenilenXercPrioritet Prioritet { get; set; } = GozlenilenXercPrioritet.Orta;
    public GozlenilenXercStatus Status { get; set; } = GozlenilenXercStatus.Aktiv;

    public int? XercId { get; set; }
    public Xerc? Xerc { get; set; }

    public string? Qeyd { get; set; }
}

using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication;

public class GelenMailIsci : BaseEntity
{
    public int GelenMailId { get; set; }
    public int IsciId { get; set; }
    public int TapalanIsciTarafindan { get; set; }  // rehber AppUser id
    public DateTime TapalanTarix { get; set; }
    public string? Qeyd { get; set; }
    public bool IcraOlundu { get; set; }
    public DateTime? IcraOlunduTarix { get; set; }
    public bool ImtinaEtdi { get; set; }
    public string? ImtinaSebebi { get; set; }
    public bool IsciOxudu { get; set; }
    public DateTime? IsciOxuduTarix { get; set; }

    public virtual GelenMail GelenMail { get; set; } = null!;
    public virtual Isci Isci { get; set; } = null!;
}

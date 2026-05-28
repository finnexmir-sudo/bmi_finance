using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication;

public class IsciMail : BaseEntity
{
    public int GonderenIsciId { get; set; }
    public string Movzu { get; set; } = "";
    public string Metin { get; set; } = "";
    public string MetinHtml { get; set; } = "";
    public bool Taslagdirmi { get; set; } = false;
    public DateTime? GondermeTarixi { get; set; }

    public virtual Isci Gonderen { get; set; } = null!;
    public virtual ICollection<IsciMailAlici> Alicilar { get; set; } = new List<IsciMailAlici>();
}

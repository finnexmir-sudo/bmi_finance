using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication;

public class IsciMailAlici : BaseEntity
{
    public int IsciMailId { get; set; }
    public int AliciIsciId { get; set; }
    public bool Oxunub { get; set; } = false;
    public DateTime? OxunmaTarixi { get; set; }
    public string AliciNovu { get; set; } = "To"; // To / CC
    public bool Arxivlendi { get; set; } = false;
    public bool IsciTarafindenSilindi { get; set; } = false;

    public virtual IsciMail IsciMail { get; set; } = null!;
    public virtual Isci Alici { get; set; } = null!;
}

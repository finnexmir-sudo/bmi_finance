using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.HR;

public class DovriOdenis : BaseEntity
{
    public string Ad { get; set; } = "";

    public int KateqoriyaId { get; set; }
    public XercKateqoriyasi Kateqoriya { get; set; } = null!;

    public int DepartamentId { get; set; }
    public Departament Departament { get; set; } = null!;

    public decimal Mebleg { get; set; }

    public DovruluqNovu Dovruluq { get; set; } = DovruluqNovu.Ay;

    public DateTime BaslamaTarixi { get; set; }
    public DateTime? BitmeTarixi { get; set; }
    public DateTime NovbatiOdenisTarixi { get; set; }

    public bool Aktiv { get; set; } = true;
    public string? Qeyd { get; set; }
}

using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.Sorgular;

public class OracleSorgu : BaseEntity
{
    public string SorguAdi { get; set; } = null!;
    public string? Mahiyyet { get; set; }
    public string SorguMetni { get; set; } = null!;
    public bool Aktiv { get; set; } = true;
    public bool Kataloq { get; set; } = false;   // "Ümumi cari Kataloq" comboboxunda göstərilsin

    public int DepartamentId { get; set; }
    public Departament Departament { get; set; } = null!;
}

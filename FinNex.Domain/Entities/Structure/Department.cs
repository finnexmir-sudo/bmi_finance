using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.Domain.Entities.Structure;

public class Department : BaseEntity
{
    public string Ad { get; set; } = null!;
    public string? Aciqlama { get; set; }
    public ICollection<UserDepartment> UserDepartments { get; set; }
    = new List<UserDepartment>();
    public ICollection<Sened> Senedler { get; set; } = new List<Sened>();
    public ICollection<SenedNovu> SenedNovleri { get; set; } = new List<SenedNovu>();

}

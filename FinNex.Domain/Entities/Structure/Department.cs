namespace FinNex.Domain.Entities.Structure;

public class Department : BaseEntity
{
    public string Ad { get; set; } = null!;
    public string? Aciqlama { get; set; }
    public ICollection<UserDepartment> UserDepartments { get; set; }
    = new List<UserDepartment>();

}

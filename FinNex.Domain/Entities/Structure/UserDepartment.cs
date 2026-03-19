namespace FinNex.Domain.Entities.Structure
{
    public class UserDepartment : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public int DepartmentId { get; set; }
        public Departament Department { get; set; } = null!;

        public bool Esasdir { get; set; } = false;   // Primary department
    }
}

namespace FinNex.Application.DTOs.Structur
{
    public class UserDepartmentListDto
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;
        public bool Esasdir { get; set; }
    }

}

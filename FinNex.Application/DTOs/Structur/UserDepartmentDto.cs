namespace FinNex.Application.DTOs.Structur
{
    public class UserDepartmentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
        public bool Esasdir { get; set; }
    }
    public class UserDepartmentCreateDto
    {
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
        public bool Esasdir { get; set; }
    }
    public class UserDepartmentListDto
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = null!;
        public bool Esasdir { get; set; }
    }
    public class DepartmentListDto
    {
        public int Id { get; set; }

        public string Ad { get; set; } = null!;

        public string? Aciqlama { get; set; }
    }
    public class DepartmentCreateDto
    {
        public string Ad { get; set; } = null!;

        public string? Aciqlama { get; set; }
    }
    public class DepartmentUpdateDto
    {
        public int Id { get; set; }

        public string Ad { get; set; } = null!;

        public string? Aciqlama { get; set; }
    }
    public class DepartmentDetailDto
    {
        public int Id { get; set; }

        public string Ad { get; set; } = null!;

        public string? Aciqlama { get; set; }

        public int UserCount { get; set; }
    }

}

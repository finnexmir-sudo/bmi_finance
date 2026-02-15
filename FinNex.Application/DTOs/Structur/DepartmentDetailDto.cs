namespace FinNex.Application.DTOs.Structur
{
    public class DepartmentDetailDto
    {
        public int Id { get; set; }

        public string Ad { get; set; } = null!;

        public string? Aciqlama { get; set; }

        public int UserCount { get; set; }
    }

}

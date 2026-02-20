namespace FinNex.Application.DTOs.Structur
{
    public class DepartmentListDto
    {
        public int Id { get; set; }

        public string Ad { get; set; } = null!;
        public int IsciSayi { get; set; }

        public string? Aciqlama { get; set; }
    }

}

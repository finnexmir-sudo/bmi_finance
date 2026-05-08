namespace FinNex.Application.DTOs.HR.Jeton
{
    public class IsciJetonuCreateDto
    {
        public int IsciId { get; set; }
        public int JetonTeyinatiId { get; set; }
        public string Sebeb { get; set; } = null!;
        public int Eded { get; set; } = 1;
    }
}

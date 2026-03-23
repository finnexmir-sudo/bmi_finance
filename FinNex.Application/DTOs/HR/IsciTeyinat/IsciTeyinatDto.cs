using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.HR.IsciTeyinat
{
    public class IsciTeyinatDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciTamAd { get; set; } = null!;

        public int DepartamentId { get; set; }
        public string DepartamentAd { get; set; } = null!;

        public int VezifeId { get; set; }
        public string VezifeAd { get; set; } = null!;

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }

        public bool Esasdir { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class IsciTeyinatCreateDto
    {
        [Required]
        public int IsciId { get; set; }

        [Required]
        public int DepartamentId { get; set; }

        [Required]
        public int VezifeId { get; set; }

        [Required]
        public DateTime BaslamaTarixi { get; set; }

        public DateTime? BitmeTarixi { get; set; }

        public bool Esasdir { get; set; } = true;
        public bool Aktivdir { get; set; } = true;
    }

    public class IsciTeyinatUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int IsciId { get; set; }

        [Required]
        public int DepartamentId { get; set; }

        [Required]
        public int VezifeId { get; set; }

        [Required]
        public DateTime BaslamaTarixi { get; set; }

        public DateTime? BitmeTarixi { get; set; }

        public bool Esasdir { get; set; }
        public bool Aktivdir { get; set; }
    }
}
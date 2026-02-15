using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.SenedDovriyyesi.SenedNovu
{
    public class SenedNovuListDto
    {
        public int Id { get; set; }

        public string Kod { get; set; } = null!;
        public string Ad { get; set; } = null!;

        public int DepartmentId { get; set; }
        public string DepartmentAd { get; set; } = null!;

        public bool Aktiv { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
    }
    public class SenedNovuDetailDto
    {
        public int Id { get; set; }

        public string Kod { get; set; } = null!;
        public string Ad { get; set; } = null!;

        public int DepartmentId { get; set; }
        public string DepartmentAd { get; set; } = null!;

        public bool Aktiv { get; set; }

        public DateTime YaradilmaTarixi { get; set; }
        public DateTime? YenilenmeTarixi { get; set; }
    }
    public class SenedNovuCreateDto
    {
        [Required]
        public int DepartmentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Kod { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Ad { get; set; } = null!;
    }
    public class SenedNovuUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Kod { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Ad { get; set; } = null!;

        public bool Aktiv { get; set; }
    }
}

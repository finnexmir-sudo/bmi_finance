using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.Structure.Permission
{
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Kod { get; set; } = null!;
        public string Ad { get; set; } = null!;
        public string? Aciqlama { get; set; }
    }

    public class PermissionCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Kod { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Ad { get; set; } = null!;

        [MaxLength(500)]
        public string? Aciqlama { get; set; }
    }

    public class PermissionUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Kod { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Ad { get; set; } = null!;

        [MaxLength(500)]
        public string? Aciqlama { get; set; }
    }
}
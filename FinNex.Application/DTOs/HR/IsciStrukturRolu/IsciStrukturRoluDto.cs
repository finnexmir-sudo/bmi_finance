using System.ComponentModel.DataAnnotations;
using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.IsciStrukturRolu
{
    public class IsciStrukturRoluDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciTamAd { get; set; } = null!;

        public StrukturRolTipi RolTipi { get; set; }

        public int? DepartamentId { get; set; }
        public string? DepartamentAd { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }

        public bool Aktivdir { get; set; }
    }

    public class IsciStrukturRoluCreateDto
    {
        [Required]
        public int IsciId { get; set; }

        [Required]
        public StrukturRolTipi RolTipi { get; set; }

        public int? DepartamentId { get; set; }

        [Required]
        public DateTime BaslamaTarixi { get; set; }

        public DateTime? BitmeTarixi { get; set; }

        public bool Aktivdir { get; set; } = true;
    }

    public class IsciStrukturRoluUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int IsciId { get; set; }

        [Required]
        public StrukturRolTipi RolTipi { get; set; }

        public int? DepartamentId { get; set; }

        [Required]
        public DateTime BaslamaTarixi { get; set; }

        public DateTime? BitmeTarixi { get; set; }

        public bool Aktivdir { get; set; }
    }
}
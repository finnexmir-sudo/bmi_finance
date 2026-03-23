using System.ComponentModel.DataAnnotations;

namespace FinNex.Application.DTOs.Structure.UserPermission
{
    public class UserPermissionDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string UserAdSoyad { get; set; } = null!;

        public int PermissionId { get; set; }
        public string PermissionKod { get; set; } = null!;
        public string PermissionAd { get; set; } = null!;

        public bool Allowed { get; set; }
    }

    public class UserPermissionCreateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int PermissionId { get; set; }

        public bool Allowed { get; set; } = true;
    }

    public class UserPermissionUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int PermissionId { get; set; }

        public bool Allowed { get; set; }
    }
}
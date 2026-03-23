namespace FinNex.Domain.Entities.HR
{
    public class UserPermission : BaseEntity
    {
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public bool Allowed { get; set; } = true;
    }
}
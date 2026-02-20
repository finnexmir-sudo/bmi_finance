namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class SenedAccess : BaseEntity
    {
        public int SenedId { get; set; }
        public Sened Sened { get; set; } = null!;

        public PrincipalType PrincipalType { get; set; }
        public int PrincipalId { get; set; }

        public AccessPermission Permission { get; set; }
    }

}

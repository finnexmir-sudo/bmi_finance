// FinNex.Domain.Entities.Communication/GorushIshtirakci.cs
using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class GorushIshtirakci : BaseEntity
    {
        public int GorushId { get; set; }
        public Gorush Gorush { get; set; } = null!;

        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public IshtirakciStatus Status { get; set; } = IshtirakciStatus.Gozleyir;
        public string? Qeyd { get; set; }
    }

    public enum IshtirakciStatus
    {
        Gozleyir = 1,
        Qebul = 2,
        Redd = 3,
        IshtiraketmeyecekBildirib = 4
    }
}
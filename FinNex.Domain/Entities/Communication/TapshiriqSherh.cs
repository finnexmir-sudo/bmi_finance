// FinNex.Domain.Entities.Communication/TapshiriqSherh.cs
using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class TapshiriqSherh : BaseEntity
    {
        public int TapshiriqId { get; set; }
        public Tapshiriq Tapshiriq { get; set; } = null!;

        public int MuellifIsciId { get; set; }
        public Isci MuellifIsci { get; set; } = null!;

        public string Metn { get; set; } = "";
    }
}
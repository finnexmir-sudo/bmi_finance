using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class ChatMesaj : BaseEntity
    {
        public int GonderenIsciId { get; set; }
        public Isci GonderenIsci { get; set; } = null!;

        public int AlanIsciId { get; set; }
        public Isci AlanIsci { get; set; } = null!;

        public string Metn { get; set; } = null!;
        public bool Oxunub { get; set; }
        public DateTime GonderilmeTarixi { get; set; } = DateTime.Now;
        public DateTime? OxunmaTarixi { get; set; }

        /// <summary>
        /// Toplu mesajları qruplaşdırmaq üçün. Eyni qrup ID-li mesajlar bir toplu göndərişdəndir.
        /// </summary>
        public Guid? TopluMesajGrupId { get; set; }
    }
}

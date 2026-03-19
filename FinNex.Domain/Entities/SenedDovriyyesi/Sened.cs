using FinNex.Domain.Entities.Structure;

namespace FinNex.Domain.Entities.SenedDovriyyesi
{
    public class Sened : BaseEntity
    {
        public int DepartmentId { get; set; }
        public Departament Department { get; set; } = null!;

        public int SenedNovuId { get; set; }
        public SenedNovu SenedNovu { get; set; } = null!;

        public string Basliq { get; set; } = null!;

        // Axtarış üçün indekslənəcək
        public string AcarSoz { get; set; } = null!;

        public SenedStatusu Status { get; set; } = SenedStatusu.Yeni;

        public MexfilikSeviyesi Mexfilik { get; set; } = MexfilikSeviyesi.Internal;

        // 🔴 ÇOX VACİB – hansı obyektə bağlıdır
        public string? ReferenceType { get; set; }   // "Musteri", "Kredit", "Odenis"
        public int? ReferenceId { get; set; }

        public ICollection<SenedFayl> Fayllar { get; set; } = new List<SenedFayl>();
        public ICollection<SenedTagMap> SenedTagMaps { get; set; } = new List<SenedTagMap>();
        public ICollection<SenedAccess> Accessler { get; set; } = new List<SenedAccess>();

    }

}

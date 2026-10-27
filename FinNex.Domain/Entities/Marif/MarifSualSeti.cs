using FinNex.Domain.Entities.AI;

namespace FinNex.Domain.Entities.Marif
{
    /// <summary>
    /// Daxili/xarici qaydalardan AI ilə hazırlanan, HR tərəfindən təsdiqlənib
    /// işçilərə göndərilən sual dəsti (27.10.2026, pilot — idarəetmə tam Admin
    /// əlindədir, avtomatik cədvəl/təkrar YOXDUR).
    /// </summary>
    public class MarifSualSeti : BaseEntity
    {
        public string Basliq { get; set; } = string.Empty;

        /// <summary>Mənbə — mövcud HR Qanun faylı (ixtiyari, xarici qanunvericilik).</summary>
        public int? HRQanunFaylId { get; set; }
        public HRQanunFayl? HRQanunFayl { get; set; }

        /// <summary>Mənbə — mövcud HR Daxili Qaydası (ixtiyari, bax HRMeslehetciController.Qayda*).</summary>
        public int? HRDaxiliQaydaId { get; set; }
        public HRDaxiliQayda? HRDaxiliQayda { get; set; }

        /// <summary>Mənbə — HR-in özünün yapışdırdığı/yüklədiyi mətn (yuxarıdakı ikisi seçilməyibsə).</summary>
        public string? MenbeMetn { get; set; }

        public MarifSetStatusu Status { get; set; } = MarifSetStatusu.Qaralama;

        public ICollection<MarifSual> Suallar { get; set; } = new List<MarifSual>();
        public ICollection<MarifTesyinat> Tesyinatlar { get; set; } = new List<MarifTesyinat>();
    }
}

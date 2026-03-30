// Domain/Entities/Communication/Xatirlatma.cs
using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    public class Xatirlatma : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci? Isci { get; set; }

        public string Bashliq { get; set; } = null!;
        public string? Qeyd { get; set; }

        public DateTime XatirlatmaTarixi { get; set; }   // nə vaxt xatırladılsın
        public bool OxunubMu { get; set; } = false;
        public bool GonderiilibMi { get; set; } = false; // background job işarəsi

        public XatirlatmaNov Nov { get; set; } = XatirlatmaNov.Fərdi;

        // Əgər sistem tərəfindən yaradılıbsa — hansı entitiyə bağlıdır
        public XatirlatmaEntityTipi? EntityTipi { get; set; }
        public int? EntityId { get; set; }
    }

    public enum XatirlatmaNov
    {
        Fərdi = 1,       // işçinin özü yaratdı
        Sistem = 2        // sistem avtomatik yaratdı (tapşırıq son tarixi, görüş və s.)
    }

    public enum XatirlatmaEntityTipi
    {
        Tapshiriq = 1,
        Gorush = 2,
        Mezuniyyet = 3,
        Icaze = 4
    }
}
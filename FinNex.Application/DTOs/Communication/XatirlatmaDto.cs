using FinNex.Domain.Entities.Communication;

namespace FinNex.Application.DTOs.Communication
{
    public class XatirlatmaDto
    {
        public int Id { get; set; }

        public int IsciId { get; set; }

        public string Bashliq { get; set; } = null!;
        public string? Qeyd { get; set; }

        public DateTime XatirlatmaTarixi { get; set; }

        public bool OxunubMu { get; set; }
        public bool GonderiilibMi { get; set; }

        public XatirlatmaNov Nov { get; set; }

        public XatirlatmaEntityTipi? EntityTipi { get; set; }
        public int? EntityId { get; set; }
    }
    public class XatirlatmaCreateDto
    {

        public int IsciId { get; set; }

        public string Bashliq { get; set; } = null!;
        public string? Qeyd { get; set; }

        public DateTime XatirlatmaTarixi { get; set; }

        public bool OxunubMu { get; set; }
        public bool GonderiilibMi { get; set; }

        public XatirlatmaNov Nov { get; set; }

        public XatirlatmaEntityTipi? EntityTipi { get; set; }
        public int? EntityId { get; set; }
    }
    public class XatirlatmaSistemCreateDto
    {
        public int IsciId { get; set; }
        public string Bashliq { get; set; } = null!;
        public string? Qeyd { get; set; }

        public DateTime XatirlatmaTarixi { get; set; }

        public XatirlatmaNov Nov { get; set; } = XatirlatmaNov.Fərdi;

        public XatirlatmaEntityTipi? EntityTipi { get; set; }
        public int? EntityId { get; set; }
    }
    public class XatirlatmaUpdateDto
    {
        public int Id { get; set; }

        public string Bashliq { get; set; } = null!;
        public string? Qeyd { get; set; }

        public DateTime XatirlatmaTarixi { get; set; }

        public bool OxunubMu { get; set; }
    }
    public class XatirlatmaListDto
    {
        public int Id { get; set; }

        public string Bashliq { get; set; } = null!;
        public string? Qeyd { get; set; }

        public DateTime XatirlatmaTarixi { get; set; }

        public bool OxunubMu { get; set; }

        public string NovAd { get; set; } = null!;

        public int Nov { get; set; }
        public int? EntityTipi { get; set; }
        public int? EntityId { get; set; }
    }
}
// FinNex.Application.DTOs.Communication/GorushDto.cs
using FinNex.Domain.Entities.Communication;

namespace FinNex.Application.DTOs.Communication
{
    public class GorushListDto
    {
        public int Id { get; set; }
        public string Bashliq { get; set; } = "";
        public string TeshkilatciAd { get; set; } = "";
        public int TeshkilatciIsciId { get; set; }
        public DateTime Tarix { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }
        public string? Yer { get; set; }
        public string? OnlineLink { get; set; }
        public GorushNovu Nov { get; set; }
        public GorushStatus Status { get; set; }
        public int IshtirakciSayi { get; set; }
        public bool MenimIshtirakimVar { get; set; }
        public IshtirakciStatus? MenimStatusum { get; set; }

        // Status DB-dən deyil, vaxtdan avtomatik hesablanır.
        // LegvEdildi/Bitdi — həmişə DB-dən; qalanı vaxtla müəyyən edilir.
        public string HesablanmisStatusText
        {
            get
            {
                if (Status == GorushStatus.LegvEdildi) return "Ləğv edildi";
                if (Status == GorushStatus.Bitdi)      return "Tamamlandı";
                return DateTime.Now >= Tarix.Date + BaslamaSaati ? "Davam edir" : "Planlanıb";
            }
        }

        public string HesablanmisStatusClass
        {
            get
            {
                if (Status == GorushStatus.LegvEdildi) return "grsh-status--cancel";
                if (Status == GorushStatus.Bitdi)      return "grsh-status--done";
                return DateTime.Now >= Tarix.Date + BaslamaSaati ? "grsh-status--active" : "grsh-status--plan";
            }
        }
    }

    public class GorushDetailDto : GorushListDto
    {
        public string? Agenda { get; set; }
        public string? Qeydler { get; set; }
        public List<GorushIshtirakciDto> Ishtirakcılar { get; set; } = new();
    }

    public class GorushIshtirakciDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAd { get; set; } = "";
        public string IsciVezife { get; set; } = "";
        public IshtirakciStatus Status { get; set; }
        public string? Qeyd { get; set; }
    }

    public class GorushCreateDto
    {
        public string Bashliq { get; set; } = "";
        public string? Agenda { get; set; }
        public int TeshkilatciIsciId { get; set; }
        public DateTime Tarix { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }
        public string? Yer { get; set; }
        public string? OnlineLink { get; set; }
        public GorushNovu Nov { get; set; } = GorushNovu.Offline;
        public List<int> IshtirakciIsciIdler { get; set; } = new();
    }
    public class GorushEditDto
    {
        public int Id { get; set; }
        public string Bashliq { get; set; } = "";
        public string? Agenda { get; set; }
        public int TeshkilatciIsciId { get; set; }
        public DateTime Tarix { get; set; }
        public TimeSpan BaslamaSaati { get; set; }
        public TimeSpan? BitisSaati { get; set; }
        public string? Yer { get; set; }
        public string? OnlineLink { get; set; }
        public GorushNovu Nov { get; set; }
        public List<int> IshtirakciIsciIdler { get; set; } = new();
    }
}
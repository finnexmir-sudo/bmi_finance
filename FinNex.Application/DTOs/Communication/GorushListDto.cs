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

        public string StatusText => Status switch
        {
            GorushStatus.Planlanib => "Planlanıb",
            GorushStatus.Bashladi => "Başladı",
            GorushStatus.Bitdi => "Bitdi",
            GorushStatus.LegvEdildi => "Ləğv edildi",
            _ => ""
        };

        public string NovText => Nov switch
        {
            GorushNovu.Offline => "Offline",
            GorushNovu.Online => "Online",
            GorushNovu.Hibrid => "Hibrid",
            _ => ""
        };
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
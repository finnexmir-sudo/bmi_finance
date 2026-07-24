// FinNex.Application.DTOs.Communication/TapshiriqDto.cs
using FinNex.Domain.Entities.Communication;

namespace FinNex.Application.DTOs.Communication
{
    public class TapshiriqListDto
    {
        public int Id { get; set; }
        public string Bashliq { get; set; } = "";
        public string? Tesvir { get; set; }
        public string YaradanAd { get; set; } = "";
        public int YaradanIsciId { get; set; }
        public string TeyinOlunanAd { get; set; } = "";
        public int TeyinOlunanIsciId { get; set; }
        public DateTime? SonTarix { get; set; }
        public TapshiriqPrioritet Prioritet { get; set; }
        public TapshiriqStatus Status { get; set; }
        public int TamamlanmaFaizi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
        public bool Gecikibmi => SonTarix.HasValue && SonTarix < DateTime.Today
                                && Status != TapshiriqStatus.Tamamlandi
                                && Status != TapshiriqStatus.LegvEdildi
                                && Status != TapshiriqStatus.ImtinaEdildi;

        public string PrioritetText => Prioritet switch
        {
            TapshiriqPrioritet.Ashagi => "Aşağı",
            TapshiriqPrioritet.Orta => "Orta",
            TapshiriqPrioritet.Yuksek => "Yüksək",
            TapshiriqPrioritet.Kritik => "Kritik",
            _ => ""
        };

        public string StatusText => Status switch
        {
            TapshiriqStatus.Yeni => "Yeni",
            TapshiriqStatus.DavamEdir => "Davam edir",
            TapshiriqStatus.Tamamlandi => "Tamamlandı",
            TapshiriqStatus.LegvEdildi => "Ləğv edildi",
            TapshiriqStatus.Gecikdi => "Gecikdi",
            TapshiriqStatus.ImtinaEdildi => "İmtina edildi",
            _ => ""
        };
    }

    public class TapshiriqDetailDto : TapshiriqListDto
    {
        public string? Qeyd { get; set; }
        public List<TapshiriqSherhDto> Sherhler { get; set; } = new();
    }

    public class TapshiriqSherhDto
    {
        public int Id { get; set; }
        public string MuellifAd { get; set; } = "";
        public int MuellifIsciId { get; set; }
        public string Metn { get; set; } = "";
        public DateTime YaradilmaTarixi { get; set; }
    }

    public class TapshiriqCreateDto
    {
        public string Bashliq { get; set; } = "";
        public string? Tesvir { get; set; }
        public int YaradanIsciId { get; set; }
        public int TeyinOlunanIsciId { get; set; }
        public DateTime? SonTarix { get; set; }
        public TapshiriqPrioritet Prioritet { get; set; } = TapshiriqPrioritet.Orta;
        public string? Qeyd { get; set; }
    }

    public class TapshiriqUpdateDto
    {
        public int Id { get; set; }
        public TapshiriqStatus Status { get; set; }
        public int TamamlanmaFaizi { get; set; }
        public string? Qeyd { get; set; }
    }
    public class TapshiriqEditDto
    {
        public int Id { get; set; }
        public string Bashliq { get; set; } = "";
        public string? Tesvir { get; set; }
        public int YaradanIsciId { get; set; }
        public int TeyinOlunanIsciId { get; set; }
        public DateTime? SonTarix { get; set; }
        public TapshiriqPrioritet Prioritet { get; set; }
        public string? Qeyd { get; set; }
    }
}
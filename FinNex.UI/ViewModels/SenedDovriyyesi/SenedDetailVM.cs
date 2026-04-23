using FinNex.Domain.Entities.SenedDovriyyesi;

namespace FinNex.UI.ViewModels.SenedDovriyyesi;

public class SenedDetailVM
{
    public int Id { get; set; }
    public string? SenedNomresi { get; set; }
    public string Basliq { get; set; } = null!;
    public string AcarSoz { get; set; } = null!;
    public SenedStatusu Status { get; set; }
    public string Sobe { get; set; } = null!;
    public string SenedNovu { get; set; } = null!;
    public DateTime SenedTarixi { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
    public int? YaradanIcraciId { get; set; }
    public string? YaradanAd { get; set; }
    public DateTime? YenilenmeTarixi { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<SenedFaylItemVM> Fayllar { get; set; } = new();
    public List<AuditLogItemVM> AuditLogs { get; set; } = new();
}

public class SenedFaylItemVM
{
    public int Id { get; set; }
    public int VersiyaNo { get; set; }
    public string OriginalAd { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long OlcuBytes { get; set; }
    public string Sha256 { get; set; } = null!;
    public bool AktivVersiya { get; set; }
    public DateTime YaradilmaTarixi { get; set; }

    public string FormattedSize
    {
        get
        {
            if (OlcuBytes < 1024) return $"{OlcuBytes} B";
            if (OlcuBytes < 1024 * 1024) return $"{OlcuBytes / 1024.0:F1} KB";
            return $"{OlcuBytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}

public class AuditLogItemVM
{
    public int Id { get; set; }
    public string Action { get; set; } = null!;
    public string? DetailsJson { get; set; }
    public string? Ip { get; set; }
    public DateTime YaradilmaTarixi { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
}

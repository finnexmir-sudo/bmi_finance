namespace FinNex.UI.Areas.Admin.ViewModels;

public class KassaTesdiqEdiciListVM
{
    public int Id { get; set; }
    public int IsciId { get; set; }
    public string TamAd { get; set; } = null!;
    public string FIN { get; set; } = null!;
    public string? SobeAdi { get; set; }
    public string? VezifeAdi { get; set; }
    public DateTime AktivdirFrom { get; set; }
    public DateTime? AktivdirTo { get; set; }
    public string? Qeyd { get; set; }
    public bool AktivdirIndi =>
        AktivdirFrom <= DateTime.Now && (AktivdirTo == null || AktivdirTo >= DateTime.Now);
}

public class KassaTesdiqEdiciTeyinVM
{
    public int IsciId { get; set; }
    public string? Qeyd { get; set; }
}

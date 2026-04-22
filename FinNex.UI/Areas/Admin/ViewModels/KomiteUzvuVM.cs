using FinNex.Domain.Entities.Kredit;

namespace FinNex.UI.Areas.Admin.ViewModels;

public class KomiteUzvuListVM
{
    public int Id { get; set; }
    public int IsciId { get; set; }
    public string TamAd { get; set; } = null!;
    public string FIN { get; set; } = null!;
    public string? SobeAdi { get; set; }
    public string? VezifeAdi { get; set; }
    public KomiteUzvuRolu Rol { get; set; }
    public DateTime AktivdirFrom { get; set; }
    public DateTime? AktivdirTo { get; set; }
    public string? Qeyd { get; set; }
    public bool AktivdirIndi =>
        AktivdirFrom <= DateTime.Now && (AktivdirTo == null || AktivdirTo >= DateTime.Now);

    public string RolAd => Rol switch
    {
        KomiteUzvuRolu.Sedr => "Sədr",
        KomiteUzvuRolu.SedrMuavini => "Sədr müavini",
        KomiteUzvuRolu.Katib => "Katib",
        _ => "Üzv"
    };
}

public class KomiteUzvuTeyinVM
{
    public int IsciId { get; set; }
    public KomiteUzvuRolu Rol { get; set; } = KomiteUzvuRolu.Uzv;
    public string? Qeyd { get; set; }
}

public class KomiteUzvuRolDeyisVM
{
    public int KomiteUzvuId { get; set; }
    public KomiteUzvuRolu YeniRol { get; set; }
}

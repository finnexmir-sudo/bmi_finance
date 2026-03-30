// Areas/User/ViewModels/Komanda/KomandaVMs.cs
using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.User.ViewModels.Komanda
{
    public class KomandaIcmalVM
    {
        public string RolBasliq { get; set; } = null!;
        public string DepartamentAdi { get; set; } = null!;
        public List<KomandaIsciSatiriVM> Isciler { get; set; } = new();

        public int AktivSayi => Isciler.Count(x => x.Status == IsciStatus.Aktiv);
    }

    public class HrKomandaIcmalVM
    {
        // Statistika
        public int CəmiIsci { get; set; }
        public int AktivIsci { get; set; }
        public int PassivIsci { get; set; }
        public int KisaySayi { get; set; }
        public int QadınSayi { get; set; }

        // Siyahı
        public List<KomandaIsciSatiriVM> Isciler { get; set; } = new();
    }

    public class KomandaIsciSatiriVM
    {
        public int Id { get; set; }
        public string TamAd { get; set; } = null!;
        public string Vezife { get; set; } = null!;
        public IsciStatus Status { get; set; }
        public string StatusReng => Status == IsciStatus.Aktiv ? "success" : "secondary";
    }

    public class IsciKartVM
    {
        public int IsciId { get; set; }
        public string TamAd { get; set; } = null!;
        public string Vezife { get; set; } = null!;
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public DateTime IsheQebulTarixi { get; set; }

        // Statistika
        public int AktivTapshiriqlar { get; set; }
        public int TamamlananTapshiriqlar { get; set; }
        public int YaxinGorushler { get; set; }

        public int IsStaji => (int)((DateTime.Today - IsheQebulTarixi).TotalDays / 365);
    }
}
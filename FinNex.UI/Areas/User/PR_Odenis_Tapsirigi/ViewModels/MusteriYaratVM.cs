using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;

namespace FinNex.UI.Areas.User.PR_Odenis_Tapsirigi.ViewModels
{
    public class MusteriYaratVM
    {
        public string Adi { get; set; } = null!;
        public string Voen { get; set; } = null!;
        public string Hesab { get; set; } = null!;
        public int ValyutaId { get; set; }
    }

    // OdenisTapsirigiSiyahiVM.cs
    public class OdenisTapsirigiSiyahiVM
    {
        public List<OdenisTapsirigi> Melumtlar { get; set; } = new();
        public OdenisTapsirigiFilterDto Filter { get; set; } = new();

        public int CariSehife { get; set; } = 1;
        public int SehifeOlcusu { get; set; } = 15;
        public int UmumiSay { get; set; }
        public int SehifeSayi => (int)Math.Ceiling((double)UmumiSay / SehifeOlcusu);
    }

    // OdenisTapsirigiFilterDto.cs
    public class OdenisTapsirigiFilterDto
    {
        public string? Nomre { get; set; }
        public string? OduyenMusteri { get; set; }
        public string? AlanMusteri { get; set; }
        public string? Valyuta { get; set; }
        public DateTime? Tarixden { get; set; }
        public DateTime? TarixeQeder { get; set; }
    }

    // OdenisTapsirigiDetailVM.cs
    public class OdenisTapsirigiDetailVM
    {
        public OdenisTapsirigi Odenis { get; set; } = null!;
        public bool BugunDuzeli { get; set; }
    }

    // OdenisTapsirigiEditDto.cs
    public class OdenisTapsirigiEditDto
    {
        public int Id { get; set; }
        public decimal Mebleg { get; set; }
        public string? MeblegYazi { get; set; }
        public string Teyinat { get; set; } = null!;
        public string? ElaveInformasiya { get; set; }
        public string? BudceTesnifatininKodu { get; set; }
        public string? BudceSeviyyesininKodu { get; set; }
    }
}

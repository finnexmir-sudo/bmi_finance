using FinNex.Domain.Entities.HR;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasDetailVM
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciTamAd { get; set; } = string.Empty;
        public string? IsciDepartament { get; set; }
        public string? IsciVezife { get; set; }
        public int Il { get; set; }
        public int Ay { get; set; }
        public DateTime HesablanmaTarixi { get; set; }
        public DateTime? TesdiqTarixi { get; set; }
        public DateTime? OdenisTarixi { get; set; }
        public decimal NetMebleg { get; set; }
        public MaasStatus Status { get; set; }

        public List<MaasDetayItemVM> Detallar { get; set; } = new();

        public decimal UmumiGelir => Detallar.Where(d => d.Tip == MaasDetayTipi.Gelir).Sum(d => d.Mebleg);
        public decimal UmumiTutulma => Detallar.Where(d => d.Tip == MaasDetayTipi.Tutulma).Sum(d => d.Mebleg);
    }

    public class MaasDetayItemVM
    {
        public int Id { get; set; }
        public string MaasNovuAd { get; set; } = string.Empty;
        public MaasDetayTipi Tip { get; set; }
        public decimal Mebleg { get; set; }
        public string? Aciqlama { get; set; }
    }
}

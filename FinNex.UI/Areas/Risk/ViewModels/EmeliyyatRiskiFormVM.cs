using FinNex.Application.DTOs.Risk;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.Risk.ViewModels;

/// <summary>Create/Edit formu — dto + dropdown mənbələri bir yerdə ki,
/// controller-in hər action-ında təkrar qurulmasın.</summary>
public class EmeliyyatRiskiFormVM
{
    public EmeliyyatRiskiHadisesiUpdateDto Dto { get; set; } = new();

    public List<SelectListItem> BiznesSaheleri { get; set; } = new();
    public List<SelectListItem> RiskKateqoriyalari1 { get; set; } = new();
    /// <summary>value=RiskKateqoriyasi2 kodu, data-ust1=aid olduğu 1-ci
    /// səviyyə kodu — JS bununla kaskad filtr edir (bax _Form.cshtml).</summary>
    public List<(int Deyer, string Ad, int Ust1)> RiskKateqoriyalari2 { get; set; } = new();
    public List<SelectListItem> TezlikDereceleri { get; set; } = new();
    public List<SelectListItem> TesirDereceleri { get; set; } = new();
    public List<SelectListItem> ZererTesirKateqoriyalari { get; set; } = new();
    public List<SelectListItem> TedbirIcraStatuslari { get; set; } = new();
    public List<SelectListItem> Isciler { get; set; } = new();

    public decimal TedbirZererHeddi { get; set; }
    public bool RedaktedirMi { get; set; }
}

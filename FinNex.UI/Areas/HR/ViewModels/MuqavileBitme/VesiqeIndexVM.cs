using FinNex.Application.DTOs.HR.Vesiqe;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinNex.UI.Areas.HR.ViewModels.MuqavileBitme
{
    /// <summary>
    /// HR → Müddətlər → «Şəxsiyyət vəsiqəsi» sekmesi.
    ///
    /// ⚠️ Tarixlər BMI-dən (Oracle) CANLI oxunur və HEÇ YERƏ YAZILMIR.
    /// Ona görə burada «yenilə/idxal et» düyməsi YOXDUR — səhifəni yeniləmək
    /// kifayətdir.
    ///
    /// KPI-lar `Rows`-dan hesablanır (Müqavilə Bitmə ilə eyni qayda) — say və
    /// siyahı avtomatik tutuşur, filtr view-da təkrarlanmır.
    /// </summary>
    public class VesiqeIndexVM
    {
        public List<VesiqeSetriDto> Rows { get; set; } = new();

        /// <summary>BMI oxunmayıbsa xəta mətni — siyahı boş göstərilir.</summary>
        public string? Xeta { get; set; }

        public int?    DepartamentId { get; set; }
        public string? Search        { get; set; }

        public List<SelectListItem> Departamentler { get; set; } = new();

        public int CemiSay   => Rows.Count;
        public int KecmisSay => Rows.Count(r => r.QalanGun < 0);
        /// <summary>30 gün və az qalıb (keçmişlər daxil deyil).</summary>
        public int TecibiSay => Rows.Count(r => r.QalanGun >= 0 && r.QalanGun <= 30);
        public int DiqqetSay => Rows.Count(r => r.QalanGun > 30 && r.QalanGun <= 90);
        /// <summary>BMI-də tarix tapılmayanlar — «bitib» SAYILMIR.</summary>
        public int YoxdurSay => Rows.Count(r => r.QalanGun == null);
    }
}

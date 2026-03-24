using FinNex.Domain.Entities.HR;
using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasNovuCreateVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad tələb olunur")]
        [StringLength(200, ErrorMessage = "Ad 200 simvoldan artıq ola bilməz")]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Növ tipi tələb olunur")]
        public MaasDetayTipi Tip { get; set; }

        public bool Aktivdir { get; set; } = true;
    }
}

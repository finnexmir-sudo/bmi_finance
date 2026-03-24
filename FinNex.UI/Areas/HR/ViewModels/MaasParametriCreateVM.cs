using FinNex.Domain.Entities.HR;
using System.ComponentModel.DataAnnotations;

namespace FinNex.UI.Areas.HR.ViewModels
{
    public class MaasParametriCreateVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Parametr növü tələb olunur")]
        public MaasParametrNovu Nov { get; set; }

        [Required(ErrorMessage = "Parametr tipi tələb olunur")]
        public MaasParametrTipi Tip { get; set; }

        [Required(ErrorMessage = "Dəyər tələb olunur")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Dəyər 0-dan böyük olmalıdır")]
        public decimal Deyer { get; set; }

        [StringLength(500)]
        public string? Aciqlama { get; set; }

        [Required(ErrorMessage = "Başlama tarixi tələb olunur")]
        public DateTime BaslamaTarixi { get; set; } = DateTime.Today;

        public DateTime? BitmeTarixi { get; set; }

        public bool Aktivdir { get; set; } = true;
    }
}

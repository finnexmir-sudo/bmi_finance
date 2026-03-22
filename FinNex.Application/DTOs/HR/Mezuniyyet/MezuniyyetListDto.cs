using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    public class MezuniyyetListDto
    {
        public int Id { get; set; }
        public string IsciAdSoyad { get; set; } = null!;
        public string SobeAdi { get; set; } = null!;
        public string VezifeAdi { get; set; } = null!;

        public string? EvezEdenIsciAdSoyad { get; set; }  // əlavə edildi

        public string NovText => Nov switch
        {
            MezuniyyetNovu.Illik => "İllik məzuniyyət",
            MezuniyyetNovu.Xestelik => "Xəstəlik məzuniyyəti",
            MezuniyyetNovu.Ezamiyyet => "Ezamiyyət",
            _ => Nov.ToString()
        };

        public MezuniyyetNovu Nov { get; set; }
        public MezuniyyetStatus Status { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; }

        public string WorkflowMerhele => Status switch
        {
            MezuniyyetStatus.Gozlemede => "Gözləmədə",
            MezuniyyetStatus.SobeReisiTesdiqinde => "Şöbə rəisi təsdiqində",
            MezuniyyetStatus.RehberTesdiqinde => "Rəhbər təsdiqində",
            MezuniyyetStatus.HrTesdiqinde => "HR təsdiqində",
            MezuniyyetStatus.Tesdiqlenib => "Təsdiqlənib",
            MezuniyyetStatus.ImtinaEdildi => "İmtina edildi",
            _ => "-"
        };
    }
}
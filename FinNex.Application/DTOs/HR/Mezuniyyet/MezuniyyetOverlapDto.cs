using FinNex.Domain.Entities.HR;

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    /// <summary>
    /// Təsdiq ekranında "bu dövrdə həmkarları" siyahısında göstərilən məzuniyyət.
    /// </summary>
    public class MezuniyyetOverlapDto
    {
        public int Id { get; set; }
        public int IsciId { get; set; }
        public string IsciAdSoyad { get; set; } = "";
        public string SobeAdi { get; set; } = "";
        public string VezifeAdi { get; set; } = "";
        public MezuniyyetNovu Nov { get; set; }
        public MezuniyyetStatus Status { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi { get; set; }
        public int IsGunlerininSayi { get; set; }
        // true = eyni şöbə / aktiv təyinat
        public bool EyniSobe { get; set; }
    }
}

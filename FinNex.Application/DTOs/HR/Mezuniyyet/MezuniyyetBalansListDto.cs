namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    
        public class MezuniyyetBalansListDto
        {
            public string IsciAdSoyad { get; set; } = null!;
            public int Il { get; set; }
            public int QaliqGun { get; set; } // Siyahıda ən çox buna baxılır
        }
}

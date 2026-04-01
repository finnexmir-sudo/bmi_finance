namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
   
        public class MezuniyyetBalansCreateDto
        {
            public int IsciId { get; set; }
            public int Il { get; set; }
            public int Nov { get; set; } // MezuniyyetNovu
            public int ToplamGun { get; set; }
        }
}

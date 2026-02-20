namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
   
        public class MezuniyyetBalansDto
        {
            public int Id { get; set; }
            public int IsciId { get; set; }
            public string IsciAdSoyad { get; set; } = null!;
            public int Il { get; set; }
            public int ToplamGun { get; set; }
            public int IstifadeOlunanGun { get; set; }
            public int QaliqGun { get; set; }
    }
}

namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    
        public class BayramGunuUpdateDto
        {
            public int Id { get; set; }
            public string Ad { get; set; } = null!;
            public DateTime Tarix { get; set; }
            public bool HerIlTeyinOlunur { get; set; }
            public bool MezuniyyetdeHesablanir { get; set; }
        }
}

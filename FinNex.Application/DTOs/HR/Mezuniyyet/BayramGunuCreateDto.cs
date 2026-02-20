namespace FinNex.Application.DTOs.HR.Mezuniyyet
{
    
        public class BayramGunuCreateDto
        {
            public string Ad { get; set; } = null!;
            public DateTime Tarix { get; set; }
            public bool HerIlTeyinOlunur { get; set; }
    }
}



namespace FinNex.Application.DTOs.SenedDovriyyesi.Icaze
{
    public class SenedDovriyyesiIstifadeciIcazesiDto
    {
        public int Id { get; set; }

        public int IstifadeciId { get; set; }
        public string IstifadeciAd { get; set; } = "";

        public int SobeId { get; set; }
        public string SobeAd { get; set; } = "";

        public int IcazeNovu { get; set; }
        public string IcazeNovuAd { get; set; } = "";
    }
    public class SenedDovriyyesiIstifadeciIcazesiCreateDto
    {
        public int IstifadeciId { get; set; }

        public int SobeId { get; set; }

        public int IcazeNovu { get; set; } // 1 = Baxis, 2 = Tam
    }
    public class SenedDovriyyesiIstifadeciIcazesiUpdateDto
    {
        public int Id { get; set; }

        public int IstifadeciId { get; set; }

        public int SobeId { get; set; }

        public int IcazeNovu { get; set; }
    }
}

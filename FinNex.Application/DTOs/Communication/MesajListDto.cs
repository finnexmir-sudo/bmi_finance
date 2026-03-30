namespace FinNex.Application.DTOs.Communication
{
    public class MesajListDto
    {
        public int Id { get; set; }
        public string GonderenAd { get; set; } = "";
        public string AlanAd { get; set; } = "";
        public string Movzu { get; set; } = "";
        public string MetnOnizleme { get; set; } = ""; // İlk 80 simvol
        public bool Oxunub { get; set; }
        public DateTime? OxunmaTarixi { get; set; }
        public DateTime YaradilmaTarixi { get; set; }
        public bool MenimGonderdiyim { get; set; }
        public int? CavabVerdigiMesajId { get; set; }
    }

    public class MesajDetailDto : MesajListDto
    {
        public string Metn { get; set; } = "";
        public int GonderenIsciId { get; set; }
        public int AlanIsciId { get; set; }
        public List<MesajListDto> Cavablar { get; set; } = new();
    }

    public class MesajCreateDto
    {
        public int GonderenIsciId { get; set; }
        public int AlanIsciId { get; set; }
        public string Movzu { get; set; } = "";
        public string Metn { get; set; } = "";
        public int? CavabVerdigiMesajId { get; set; }
    }
}

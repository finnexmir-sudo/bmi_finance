namespace FinNex.Application.DTOs.Paylasim
{
    /// <summary>İşçi lentində və Admin idarəetmə siyahısında göstərilən tam post.</summary>
    public class PaylasimListDto
    {
        public int Id { get; set; }
        public string Basliq { get; set; } = "";
        public string MetnHtml { get; set; } = "";
        public string GonderenAdi { get; set; } = "";
        public DateTime YaradilmaTarixi { get; set; }
        public bool Aktivdir { get; set; }
    }

    public class PaylasimYaratDto
    {
        public string Basliq { get; set; } = "";
        public string MetnHtml { get; set; } = "";
    }
}

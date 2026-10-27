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

        /// <summary>0 — "Hamısına" göndərilib. &gt;0 — yalnız bu sayda işçiyə hədəflənib.</summary>
        public int HedefSayi { get; set; }
    }

    public class PaylasimYaratDto
    {
        public string Basliq { get; set; } = "";
        public string MetnHtml { get; set; } = "";

        /// <summary>true (defolt) — hamısına görünür. false-dursa `SeciliIsciIdler` məcburidir.</summary>
        public bool HamisinaGonder { get; set; } = true;
        public List<int> SeciliIsciIdler { get; set; } = new();
    }

    /// <summary>Hədəf-seçim siyahısında bir işçi sətri — anonim tip/dynamic əvəzinə.</summary>
    public class PaylasimIsciSecimDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = "";
        public string Soyad { get; set; } = "";
    }
}

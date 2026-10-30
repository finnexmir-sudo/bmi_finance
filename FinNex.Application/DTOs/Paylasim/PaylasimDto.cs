namespace FinNex.Application.DTOs.Paylasim
{
    /// <summary>İşçi lentində və Admin idarəetmə siyahısında göstərilən tam post.</summary>
    public class PaylasimListDto
    {
        public int Id { get; set; }
        public string Basliq { get; set; } = "";
        public string MetnHtml { get; set; } = "";

        /// <summary>Üz qabığı şəkli — ixtiyaridir, mətndən AYRI sahədə saxlanılır (28.10.2026).</summary>
        public string? UzQabigiSekilUrl { get; set; }
        public string GonderenAdi { get; set; } = "";
        public DateTime YaradilmaTarixi { get; set; }
        public bool Aktivdir { get; set; }

        /// <summary>0 — "Hamısına" göndərilib. &gt;0 — yalnız bu sayda işçiyə hədəflənib.</summary>
        public int HedefSayi { get; set; }

        public int? KateqoriyaId { get; set; }
        public string? KateqoriyaAdi { get; set; }

        /// <summary>true — hələ "Qaralama"dır, heç kimə görünmür (bax Melumat.Qaralamadir).</summary>
        public bool Qaralamadir { get; set; }

        /// <summary>Bu postu neçə fərqli işçi həqiqətən AÇIB OXUYUB (30.10.2026, Admin siyahısı).</summary>
        public int OxuyanSayi { get; set; }
    }

    /// <summary>Admin-in "Kim oxudu?" siyahısında bir sətir (30.10.2026).</summary>
    public class PaylasimOxuyanDto
    {
        public string AdSoyad { get; set; } = "";
        public DateTime OxunmaTarixi { get; set; }
    }

    public class PaylasimYaratDto
    {
        public string Basliq { get; set; } = "";
        public string MetnHtml { get; set; } = "";

        /// <summary>Üz qabığı şəkli — ixtiyaridir, "Şəkil yüklə" sahəsindən gələn `/dms/...` yolu.</summary>
        public string? UzQabigiSekilUrl { get; set; }

        /// <summary>true (defolt) — hamısına görünür. false-dursa `SeciliIsciIdler` məcburidir.</summary>
        public bool HamisinaGonder { get; set; } = true;
        public List<int> SeciliIsciIdler { get; set; } = new();

        public int? KateqoriyaId { get; set; }

        /// <summary>
        /// true — "Qaralama saxla" düyməsi (heç kimə görünmür). false — "Paylaş"
        /// (dərhal dərc olunur). Redaktədə artıq paylaşılmış (Qaralama DEYİL) postda
        /// bu sahə oxunmur — bax PaylasimService.YenileAsync.
        /// </summary>
        public bool QaralamaOlaraqSaxla { get; set; }
    }

    /// <summary>Hədəf-seçim siyahısında bir işçi sətri — anonim tip/dynamic əvəzinə.</summary>
    public class PaylasimIsciSecimDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = "";
        public string Soyad { get; set; } = "";
    }

    public class PaylasimKateqoriyaDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = "";
    }
}

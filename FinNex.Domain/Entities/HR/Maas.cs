namespace FinNex.Domain.Entities.HR
{
    // MASTER: Hər ay üçün bir sətir
    public class Maas : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public int Il { get; set; }
        public int Ay { get; set; }

        public decimal NetMebleg { get; set; } // O ay işçinin kartına yatan son məbləğ
        public MaasStatus Status { get; set; } = MaasStatus.Layihe;

        public ICollection<MaasDetay> Detallar { get; set; } = new List<MaasDetay>();
    }

    // DETAIL: Maaşın tərkibindəki hər bir hərəkət (Maaş, Vergi, Bonus və s.)
    public class MaasDetay : BaseEntity
    {
        public int MaasId { get; set; }
        public Maas Maas { get; set; } = null!;

        public int MaasNovuId { get; set; }
        public MaasNovu MaasNovu { get; set; } = null!;

        public decimal Mebleg { get; set; } // Rəqəmsal dəyər
        public string? Acıqlama { get; set; } // Məs: "Gecikmə cəriməsi"
    }
    // Maaşın növlərini idarə edən kitabça
    public class MaasNovu : BaseEntity
    {
        public string Ad { get; set; } = null!;
        public bool Gelirdir { get; set; } // true (+), false (-)
        public bool Aktivdir { get; set; } = true;

        public ICollection<MaasDetay> MaasDetallari { get; set; } = new List<MaasDetay>();
    }

    // Maaş dəyişikliklərini izləmək üçün arxiv
    public class IsciMaasTarixcesi : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        public decimal KohneMaas { get; set; }
        public decimal YeniMaas { get; set; }
        public DateTime DeyismeTarixi { get; set; }
        public string? EmrinNomresi { get; set; } // Rəsmi əmr
    }
    public enum MaasStatus
    {
        // 1. Maaş hələ hesablanır, üzərində düzəlişlər edilə bilər.
        Layihe = 1,

        // 2. Hesablanıb bitib, mühasibatlıq və ya rəhbərlik tərəfindən yoxlanılıb təsdiq edilib.
        // Təsdiqlənmiş maaş üzərində dəyişiklik etmək olmaz.
        Tesdiqlendi = 2,

        // 3. Maaş faylı banka göndərilib və ya işçilərin kartına köçürülüb.
        Odenildi = 3,

        // 4. Əgər hesablamada ciddi bir səhv tapılıbsa və ya işçi ilə bağlı problem varsa,
        // həmin ayın maaşı ləğv edilə bilər.
        LegvEdildi = 4
    }
}
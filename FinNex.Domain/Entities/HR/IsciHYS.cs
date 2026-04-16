namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// Həyat Yığım Sığortası (HYS) — işçiyə təyin olunmuş aylıq ödəniş.
    ///
    /// Hesablama məntiqi:
    ///   1. İşçinin maaşından AyliqMebleg çıxılır (vergi və DSMF bazasından azad).
    ///      İTSS və işsizlik sığortası isə tam maaş üzrə hesablanır.
    ///   2. İşəgötürən əlavə olaraq AyliqMebleg × (HysIsegotürenFaizi%) ödəyir.
    ///      Bu məbləğ birbaşa HYS hesabına gedir (işçinin əlinə çatmır).
    ///   3. İşçinin ümumi gelirinə (İsciAyliqQazanc) = maaş + işəgötürən payı yazılır.
    ///   4. AyliqMebleg ≤ maaş × (HysMaxMaasFaizi%) limitinə tabedir.
    /// </summary>
    public class IsciHYS : BaseEntity
    {
        public int IsciId { get; set; }
        public Isci Isci { get; set; } = null!;

        /// <summary>
        /// İşçinin aylıq HYS ödənişi (AZN).
        /// </summary>
        public decimal AyliqMebleg { get; set; }

        public DateTime BaslamaTarixi { get; set; }
        public DateTime? BitmeTarixi { get; set; }

        /// <summary>
        /// Sığorta şirkətinin adı (informativ).
        /// </summary>
        public string? SigortaSirketi { get; set; }

        /// <summary>
        /// Polis / müqavilə nömrəsi (informativ).
        /// </summary>
        public string? PolisNomresi { get; set; }

        public string? Qeyd { get; set; }
        public bool Aktivdir { get; set; } = true;
    }
}

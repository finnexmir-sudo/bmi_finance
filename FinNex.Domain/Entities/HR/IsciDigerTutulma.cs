namespace FinNex.Domain.Entities.HR
{
    /// <summary>
    /// İşçidən "digər tutulma" — məs. işçinin özünü sığortaladığı xəstəlik
    /// sığortası: bank məbləğin yarısını ödəyir, işçinin payı isə müddət
    /// (adətən 12 ay) ərzində hər ay maaşından tutulur.
    ///
    /// DİQQƏT: bu maaş hesablamasında (BrutMebleg/NetMebleg) İSTİFADƏ
    /// EDİLMİR — maaş toxunulmaz qalır. Yalnız əməliyyat yazılışı (provodka)
    /// yaradılanda işçinin həmin aylıq payı net ödənişdən çıxılır və ayrıca
    /// öhdəlik hesabına yazılır.
    ///
    /// Aylıq tutulma = Mebleg / MuddetAy (yaradılarkən hesablanıb saxlanılır).
    /// Bir ay (il, ay) üçün aktivdir: index = (il−BaslamaIl)*12 + (ay−BaslamaAy),
    /// 0 ≤ index &lt; MuddetAy olduqda.
    /// </summary>
    public class IsciDigerTutulma : BaseEntity
    {
        public int IsciId { get; set; }

        /// <summary>İşçidən tutulacaq ümumi məbləğ (işçinin payı).</summary>
        public decimal Mebleg { get; set; }

        /// <summary>Neçə aya bölünür (adətən 12).</summary>
        public int MuddetAy { get; set; } = 12;

        /// <summary>İlk tutulma ili.</summary>
        public int BaslamaIl { get; set; }

        /// <summary>İlk tutulma ayı (1–12).</summary>
        public int BaslamaAy { get; set; }

        /// <summary>Hər ay tutulan məbləğ = Mebleg / MuddetAy (yuvarlaqlaşdırılmış).</summary>
        public decimal AyliqMebleg { get; set; }

        public string? Aciqlama { get; set; }

        public bool Aktiv { get; set; } = true;
    }
}

namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Komitəsiz rədd səbəbləri — açar siyahısı (02.09.2026).
    ///
    /// NİYƏ CƏDVƏL, ENUM YOX: səbəb siyahısı biznes qərarıdır və zamanla artır.
    /// Enum olsaydı hər yeni səbəb üçün kod dəyişikliyi + build + deploy lazım
    /// gələrdi. Cədvəl olduğuna görə Admin öz səhifəsindən əlavə edir.
    ///
    /// NİYƏ SƏRBƏST MƏTN YOX: istifadəçi qərarı — səbəblərə görə hesabat
    /// çıxarmaq mümkün olsun («bu ay 40 müraciətdən 12-si MKR-ə görə rədd
    /// olunub»). Sərbəst mətndən belə hesabat çıxmır. Əlavə izah üçün
    /// `KreditMuraciet.ReddQeyd` sahəsi var — o, sərbəstdir və məcburi deyil.
    ///
    /// SİLİNMİR, DEAKTİV EDİLİR: keçmiş müraciətlər bu sətrə istinad edir.
    /// Silinsə tarixçə «səbəbsiz» qalar. `Aktivdir=false` olan səbəb yeni
    /// rəddlərdə seçim siyahısında görünmür, köhnə qeydlərdə isə görünməyə
    /// davam edir.
    /// </summary>
    public class KreditReddSebebi : BaseEntity
    {
        /// <summary>Səbəbin görünən adı — məs. «MKR mənfi (kredit tarixçəsi)».</summary>
        public string Ad { get; set; } = null!;

        /// <summary>Seçim siyahısındakı sıra. Kiçik rəqəm yuxarıda.</summary>
        public int Sira { get; set; }

        /// <summary>
        /// Yeni rəddlərdə seçilə bilərmi. `false` olsa siyahıdan çıxır, amma
        /// köhnə müraciətlərdə səbəb kimi göstərilməyə davam edir.
        /// </summary>
        public bool Aktivdir { get; set; } = true;
    }
}

namespace FinNex.Domain.Entities.Emeliyyat
{
    /// <summary>
    /// Pul köçürməsində 20 000 USD aylıq limiti aşan əməliyyatın ƏSASLANDIRMA
    /// sənədinin növü — açar siyahısı (07.09.2026).
    ///
    /// QANUN: rezident və qeyri-rezident fiziki şəxsin təqvim ayı ərzində cəmi
    /// 20 000 ABŞ dolları ekvivalentinədək məbləğdə köçürmələri məqsədi bəyan
    /// edilməklə aparılır. Həddi aşan hissə üçün əsas sənəd tələb olunur.
    ///
    /// NİYƏ CƏDVƏL, ENUM YOX: sənəd növləri biznes/qanunvericilik qərarıdır və
    /// artır. Enum olsaydı hər yeni növ üçün build + deploy lazım gələrdi.
    ///
    /// NİYƏ SƏRBƏST MƏTN YOX: istifadəçi qərarı — «hərə nə gəldi yazmasın».
    /// Növ üzrə hesabat çıxarmaq mümkün olsun. Sərbəst izah üçün ayrıca
    /// <c>Kocurme.LimitQeydi</c> sahəsi var (məcburi deyil).
    ///
    /// SİYAHI BOŞ BAŞLAYIR — qəsdən seed edilməyib. Uydurma növ adı yazmaqdansa
    /// ilk lazım olanda operator elə köçürmə formasından əlavə edir və növbəti
    /// dəfə hamı siyahıdan seçir.
    ///
    /// SİLİNMİR, DEAKTİV EDİLİR: keçmiş köçürmələr bu sətrə FK ilə bağlıdır;
    /// silinsə tarixçə «sənədsiz» qalar. <c>Aktivdir=false</c> olan növ yeni
    /// seçimlərdə görünmür, köhnə qeydlərdə görünməyə davam edir.
    /// </summary>
    public class KocurmeSenedNovu : BaseEntity
    {
        /// <summary>Görünən ad — məs. «Təhsil haqqı müqaviləsi».</summary>
        public string Ad { get; set; } = null!;

        /// <summary>Seçim siyahısındakı sıra. Kiçik rəqəm yuxarıda.</summary>
        public int Sira { get; set; }

        /// <summary>Yeni köçürmələrdə seçilə bilərmi.</summary>
        public bool Aktivdir { get; set; } = true;
    }
}

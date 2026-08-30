namespace FinNex.Domain.Entities.Yardim
{
    /// <summary>
    /// Bir SƏHİFƏNİN istifadəçi təlimatı (27.08.2026).
    ///
    /// MƏQSƏD: «bu səhifə necə işləyir?» sualı hər dəfə admin-ə gəlməsin —
    /// izah səhifənin öz üstündə, «?» düyməsinin altında olsun.
    ///
    /// TİP ADI NİYƏ `SehifeYardimi`, sadəcə `Yardim` YOX:
    /// namespace `FinNex.Domain.Entities.Yardim`-dır. Tipin adı da `Yardim`
    /// olsaydı, bu namespace-i işlədən hər faylda `Yardim` sözü artıq TİPİ yox,
    /// NAMESPACE-i göstərərdi (CS0118) və qonşu fayllar kaskad şəkildə build
    /// olmazdı — CLAUDE.md-də bu tələ ayrıca yazılıb.
    /// </summary>
    public class SehifeYardimi : BaseEntity
    {
        /// <summary>
        /// Marşruta bağlı UNİKAL açar: <c>{area}/{controller}/{action}</c>,
        /// hamısı kiçik hərflə (məs. <c>user/mezuniyyet/create</c>).
        /// Area yoxdursa <c>_</c> yazılır (məs. <c>_/home/index</c>).
        ///
        /// NİYƏ MARŞRUTDAN: hər səhifəni əl ilə bağlamaq lazım gəlmir —
        /// «?» düyməsi açarı cari marşrutdan özü hesablayır. Yeni səhifə
        /// əlavə olunanda mexanizm onu avtomatik tanıyır, sadəcə mətni yoxdur.
        /// Açarı `YardimAcar.Qur(...)` helper-i qurur — İKİ YERDƏ hesablama
        /// yazma, yoxsa biri kiçik hərfə salar, o biri salmaz və tapılmaz.
        /// </summary>
        public string Acar { get; set; } = string.Empty;

        /// <summary>
        /// İnsan üçün qısa ünvan — çatda link atmaq üçün (<c>/Yardim/mezuniyyet-muraciet</c>).
        /// Boş qalarsa başlıqdan avtomatik yaradılır. Unikaldır.
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>Səhifənin adı — «?» panelinin və indeksin başlığı.</summary>
        public string Basliq { get; set; } = string.Empty;

        /// <summary>
        /// Modul adı — indeks səhifəsində qruplaşdırma üçün
        /// (məs. «Məzuniyyət», «İcazə», «Davamiyyət»).
        /// </summary>
        public string? Modul { get; set; }

        /// <summary>
        /// Bir cümləlik xülasə — indeks siyahısında başlığın altında görünür.
        /// Uzun mətni açmadan «bu səhifə nə edir» sualına cavab verir.
        /// </summary>
        public string? Xulase { get; set; }

        /// <summary>
        /// Təlimatın özü — HTML (sadə: başlıq, siyahı, cədvəl, qalın).
        /// Admin redaktordan yazır; deploy tələb olunmur.
        /// </summary>
        public string Metn { get; set; } = string.Empty;

        /// <summary>
        /// «Hazırlanır» rejimi. `true` olduqda «?» paneli mətni yox,
        /// «Bu bölmə hazırlanır» xəbərdarlığını göstərir.
        ///
        /// NİYƏ LAZIMDIR: mühasibat/maaş bölməsi qəsdən sonraya saxlanılıb
        /// (maaş modeli dəyişir). Boş panel «sınıb» kimi oxunur — açıq
        /// «hazırlanır» yazısı isə gözləntini düzgün qurur.
        /// </summary>
        public bool Hazirlanir { get; set; }

        /// <summary>
        /// Yalnız Admin görsün (daxili qeyd/prosedur). Default `false` —
        /// təlimatlar adətən bütün istifadəçilər üçündür.
        /// </summary>
        public bool YalnizAdmin { get; set; }

        /// <summary>
        /// «?» neçə dəfə açılıb. Hansı səhifənin həqiqətən qarışıq olduğunu
        /// göstərir — sənəd yazmaq əvəzinə bəzən səhifəni sadələşdirmək lazımdır.
        /// </summary>
        public int BaxisSayi { get; set; }
    }
}

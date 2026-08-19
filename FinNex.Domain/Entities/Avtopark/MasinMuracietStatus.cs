namespace FinNex.Domain.Entities.Avtopark
{
    /// <summary>
    /// Maşın müraciətinin iş axını.
    ///
    /// <code>
    /// İŞÇİ    → Gozlemede
    /// RƏHBƏR  → Tesdiqlenib | ImtinaEdildi
    /// KASSA   → Cixib   (açar verildi)
    /// KASSA   → Qayidib (açar geri alındı)
    /// </code>
    ///
    /// Müraciət edən özü RƏHBƏRdirsə <see cref="Gozlemede"/> mərhələsi ATLANIR
    /// və müraciət yarandığı anda <see cref="Tesdiqlenib"/> olur (bax:
    /// <c>MasinMuracietService.IlkinStatus</c> — qayda TƏK YERDƏDİR, ekran
    /// həmin mənbədən oxuyur).
    ///
    /// DİRİ statuslar (maşını tutan): Gozlemede, Tesdiqlenib, Cixib.
    /// Qayidib / ImtinaEdildi / LegvEdildi maşını AZAD edir.
    /// </summary>
    public enum MasinMuracietStatus
    {
        Gozlemede = 1,
        Tesdiqlenib = 2,
        Cixib = 3,
        Qayidib = 4,
        ImtinaEdildi = 5,
        LegvEdildi = 6
    }
}

namespace FinNex.Domain.Entities.Kredit
{
    /// <summary>
    /// Müqavilə nömrə sayğacı — (Novu, Il) üzrə SON VERİLMİŞ nömrəni saxlayır.
    /// Növbəti nömrə = SonNomre + 1. İl dəyişəndə həmin il üçün sətir olmadığı
    /// üçün nömrələmə avtomatik 1-dən başlayır.
    ///
    /// BMI qarşılığı: odb.muqavile_nomreleri (bir sətir = bir il, hər sayğac ayrı
    /// SÜTUN). Burada normal formaya salınıb — yeni sayğac növü sütun/migration
    /// deyil, sadəcə yeni enum dəyəri + sətirdir. Layihədə eyni şablon artıq var:
    /// `EmrSayghaci` (Nov, Il → SonNomre).
    ///
    /// ⚠️ SEMANTİKA: BMI-də `kr_zaminlik`/`kr_menzil` NÖVBƏTİ nömrəni saxlayırdı,
    /// `kr_zaminler` isə SONUNCUNU. Burada hamısı SONUNCUDUR — köçürmə zamanı
    /// "növbəti" saxlayan sayğaclardan 1 çıxılır (MuqavileSayghacImportService).
    /// </summary>
    public class MuqavileSayghaci : BaseEntity
    {
        public MuqavileNomreNovu Novu     { get; set; }
        public int               Il       { get; set; }
        public int               SonNomre { get; set; }
    }

    /// <summary>
    /// Sayğac növləri — BMI `odb.muqavile_nomreleri` sütunlarının qarşılığı.
    /// Dəyərlər SABİTDİR (bazada rəqəm kimi saxlanılır) — dəyişdirmə, yalnız əlavə et.
    /// </summary>
    public enum MuqavileNomreNovu
    {
        KrZaminlik  = 1,   // KR_ZAMINLIK  — kredit müqaviləsi ({k_mno})
        KrMenzil    = 2,   // KR_MENZIL    — ipoteka müqaviləsi ({i_mno})
        KrZaminler  = 3,   // KR_ZAMINLER  — zaminlik müqaviləsi (running, {zmno1})
        KrSerencam  = 4,   // KR_SERENCAM  — sərəncam
        KrAvtomobil = 5,   // KR_AVTOMOBIL — avtomobil girovu
        Depozit     = 6,   // DEPOZIT      — depozit müqaviləsi
        KrKart      = 7,   // KR_KART      — kart krediti
        KartZamin   = 8,   // KART_ZAMIN   — kart krediti zaminliyi
        KrQizil     = 9    // KR_QIZIL     — qızıl girovu
    }
}

using FinNex.Domain.Entities.HR;

namespace FinNex.Domain.Entities.Communication
{
    /// <summary>
    /// "Paylaşımlar" — admin xaricdən aldığı mənalı/maraqlı məlumatı rich-text
    /// formatında (mətnin içinə şəkil yerləşdirməklə) işçilərlə bölüşür
    /// (27.10.2026, pilot). Elan-dan FƏRQLİDİR — Elan əməliyyat xəbərdarlığıdır
    /// (vacib bayrağı, bitmə tarixi), bu isə sadəcə oxuma məqsədli bilik lentidir.
    /// </summary>
    public class Melumat : BaseEntity
    {
        public string Basliq { get; set; } = null!;

        /// <summary>Rich-text HTML — serverdə HtmlSanitizer ilə təmizlənib saxlanılır.</summary>
        public string MetnHtml { get; set; } = null!;

        public int GonderenIsciId { get; set; }
        public Isci GonderenIsci { get; set; } = null!;

        public bool Aktivdir { get; set; } = true;
    }
}

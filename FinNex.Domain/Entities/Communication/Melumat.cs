using FinNex.Domain;

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

        /// <summary>
        /// Göndərən — AppUser-dir, Isci DEYİL (27.10.2026, düzəliş): Admin hesabının
        /// IsciId-si boş ola bilər (işçi profilinə bağlı deyil), ona görə Isci FK-sı
        /// "İşçi profiliniz tapılmadı" ilə HƏR publish cəhdini bloklayırdı. AppUser
        /// hər autentifikasiya olunmuş sessiyada mütləq mövcuddur.
        /// </summary>
        public int GonderenAppUserId { get; set; }
        public AppUser GonderenAppUser { get; set; } = null!;

        public bool Aktivdir { get; set; } = true;
    }
}

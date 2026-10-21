namespace FinNex.Application.Interfaces.Communication;

public interface IGelenMailImapSyncer
{
    Task<int> SyncNowAsync(string imapHost, string email, string password, int sahibUserId, CancellationToken ct = default);

    /// <summary>
    /// FinNex-də "oxundu" işarələnən mailləri serverin öz IMAP qutusunda da
    /// `\Seen` bayrağı ilə işarələyir (16.10.2026) — ki eyni qutuya qoşulan
    /// başqa müştərilər (Outlook və s.) də onu oxunmuş görsün.
    ///
    /// ƏN YÜKSƏK ÖNCƏLİK: SƏHV MAILI İŞARƏLƏMƏ. Message-ID header üzrə axtarır
    /// (saxlanmış UID yoxdur — bax `GelenMail.MessageId`), tapılmasa SAKİT keçir,
    /// heç nəyi "ən yaxın" sayıb işarələmir. Server əlçatmaz olsa və ya hər hansı
    /// mail tapılmasa, HEÇ VAXT istisna atmır — bu, best-effort əməliyyatdır,
    /// yerli "oxundu" statusunu bloklamamalıdır.
    ///
    /// 21.10.2026 — HƏQİQƏTƏN bayraqlanan Message-Id-ləri QAYTARIR (əvvəl `void`
    /// idi, çağıran tərəf heç vaxt bilmirdi hansı mesajlar üçün uğurlu olub).
    /// `GelenMailService` bunu `GelenMail.ServerdeSeenIsarelendi`-ni YALNIZ
    /// həqiqətən təsdiqlənmiş mesajlar üçün `true` yazmaq üçün işlədir — qalanlar
    /// `false` qalır ki, mail yenidən açılanda TƏKRAR cəhd edilsin.
    /// </summary>
    Task<HashSet<string>> OxunduIsaretleServerdeAsync(string imapHost, string email, string password,
        IReadOnlyCollection<string> messageIds, CancellationToken ct = default);
}

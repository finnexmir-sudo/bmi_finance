using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace FinNex.Application.Services.Communication
{
    /// <summary>
    /// `AppUser.MailSmtpParol` üçün ortaq açma köməkçisi.
    ///
    /// ⚠️ NİYƏ LAZIMDIR — `IDataProtector.Unprotect` **istisna atır** (adətən
    /// `CryptographicException`), əgər:
    ///   · Data Protection açar dəstəsi dəyişibsə (publish/recycle-dan sonra
    ///     açarlar yenidən yaradılıbsa — `Program.cs`-dəki 3.5 blokuna bax);
    ///   · saxlanmış mətn ümumiyyətlə qorunmuş dəyər deyilsə (əl ilə yazılıb).
    ///
    /// Real hadisə (10.09.2026): açarlar qalıcı saxlanmırdı və hər publish-dən
    /// sonra köhnə şifrələr açılmaz olurdu. İki fərqli təzahür verdi:
    ///   · `ProfileController.MailSina` — istisna tutulmurdu → **«Server xətası (500)»**;
    ///   · `GelenMailSyncService` — `catch { }` içində udulurdu → gələn mail və
    ///     bildiriş **səssizcə** dayanmışdı, heç bir izi yox idi.
    ///
    /// Ona görə açma HƏMİŞƏ bu metoddan keçir: nə partlayır, nə susur —
    /// `null` qaytarır və çağıran tərəf istifadəçiyə nə edəcəyini deyir.
    /// </summary>
    public static class MailParolQoruyucu
    {
        /// <summary>
        /// İstifadəçiyə göstəriləcək standart mətn. Bir yerdə saxlanılır ki,
        /// bütün ekranlarda eyni cümlə çıxsın.
        /// </summary>
        public const string AcilmadiMesaji =
            "Saxlanmış mail şifrəsi oxunmadı (server açarları yenilənib). " +
            "Profil → Mail Ayarları bölməsində şifrəni bir dəfə yenidən yazıb " +
            "«Yadda Saxla» edin.";

        /// <summary>
        /// Qorunmuş şifrəni açır. Alınmasa `null` qaytarır — İSTİSNA ATMIR.
        /// Boş/`null` giriş də `null` qaytarır (şifrə heç saxlanmayıb).
        /// </summary>
        public static string? Ac(IDataProtector protector, string? qorunmus)
        {
            if (string.IsNullOrWhiteSpace(qorunmus)) return null;

            try
            {
                return protector.Unprotect(qorunmus);
            }
            catch (CryptographicException)
            {
                // Açar dəstəsi dəyişib və ya dəyər qorunmuş mətn deyil.
                return null;
            }
            catch (FormatException)
            {
                // Base64 deyil — bazaya əl ilə düz mətn yazılıbsa belə olur.
                return null;
            }
        }
    }
}

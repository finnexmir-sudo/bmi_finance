using Microsoft.AspNetCore.Http;

namespace FinNex.Application.DTOs.HR.Ezamiyyet
{
    public class EzamiyyetMuracietCreateDto
    {
        public string   Baslig        { get; set; } = null!;
        public int?     MekanId       { get; set; }
        public string?  YeniMekanAd   { get; set; }
        public DateTime BaslamaTarixi { get; set; }
        public DateTime BitmeTarixi   { get; set; }
        public string?  BaslamaSaati  { get; set; }
        public string?  BitisSaati    { get; set; }
        public string?  Qeyd          { get; set; }
        public IFormFile? Sened       { get; set; }

        /// <summary>
        /// Ezamiyyətə maşınla gedirsə — seçilən maşın (01.09.2026).
        /// `null` / `0` = maşın lazım deyil.
        ///
        /// Ayrıca «maşın lazımdır» bayrağı QƏSDƏN YOXDUR — checkbox yalnız
        /// formada bölməni açır, həqiqəti bu sahə deyir. İki yerdə saxlasaq
        /// biri dəyişəndə o biri köhnə qalar (CLAUDE.md — jeton miqdarı tələsi).
        /// </summary>
        public int?     MasinId       { get; set; }
    }
}

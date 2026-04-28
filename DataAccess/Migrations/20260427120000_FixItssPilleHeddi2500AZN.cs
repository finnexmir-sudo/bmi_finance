using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// İTSS (İcbari Tibbi Sığorta) pillə həddi 8000 AZN-dən 2500 AZN-ə dəyişdirilir.
    ///
    /// Köhnə (yanlış seed):
    ///   İşçi:        0–8000 → 2%,    8000+ → 160 + 0.5%
    ///   İşəgötürən:  0–8000 → 2%,    8000+ → 160 + 0.5%
    ///
    /// Yeni (düzgün qayda):
    ///   İşçi:        0–2500 → 2%,    2500+ → 50  + 0.5%
    ///   İşəgötürən:  0–2500 → 2%,    2500+ → 50  + 0.5%
    ///
    /// Misal: 3500 AZN GROSS üzrə İTSS = 50 + (3500−2500) × 0.5% = 55 (əvvəl 70 idi).
    ///
    /// Yalnız seed dövründə əlavə olunmuş 4 sətir yenilənir (Id 9, 10, 11, 12).
    /// İstifadəçinin sonradan UI-dən dəyişdirdiyi dəyərlərə toxunulmur — şərt olaraq
    /// köhnə dəyər mövcuddursa yenilənir.
    /// </summary>
    public partial class FixItssPilleHeddi2500AZN : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // İTSS İşçi (Nov=4): 1-ci pillə 0–8000 → 0–2500
            migrationBuilder.Sql(@"
                UPDATE VergiPilleleri
                   SET YuxariHedd = 2500,
                       Aciqlama   = N'2026: 0–2500 AZN → 2%'
                 WHERE Id = 9 AND Nov = 4 AND AsagiHedd = 0 AND YuxariHedd = 8000;
            ");
            // İTSS İşçi (Nov=4): 2-ci pillə 8000+ → 2500+ (sabit məbləğ 160 → 50)
            migrationBuilder.Sql(@"
                UPDATE VergiPilleleri
                   SET AsagiHedd   = 2500,
                       SabitMebleg = 50,
                       Aciqlama    = N'2026: 2500+ AZN → 50 + 0.5%'
                 WHERE Id = 10 AND Nov = 4 AND AsagiHedd = 8000 AND SabitMebleg = 160;
            ");
            // İTSS İşəgötürən (Nov=9): 1-ci pillə 0–8000 → 0–2500
            migrationBuilder.Sql(@"
                UPDATE VergiPilleleri
                   SET YuxariHedd = 2500,
                       Aciqlama   = N'2026: 0–2500 AZN → 2%'
                 WHERE Id = 11 AND Nov = 9 AND AsagiHedd = 0 AND YuxariHedd = 8000;
            ");
            // İTSS İşəgötürən (Nov=9): 2-ci pillə 8000+ → 2500+ (sabit məbləğ 160 → 50)
            migrationBuilder.Sql(@"
                UPDATE VergiPilleleri
                   SET AsagiHedd   = 2500,
                       SabitMebleg = 50,
                       Aciqlama    = N'2026: 2500+ AZN → 50 + 0.5%'
                 WHERE Id = 12 AND Nov = 9 AND AsagiHedd = 8000 AND SabitMebleg = 160;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE VergiPilleleri
                   SET YuxariHedd = 8000,
                       Aciqlama   = N'2026: 0–8000 AZN → 2%'
                 WHERE Id = 9 AND Nov = 4 AND AsagiHedd = 0 AND YuxariHedd = 2500;
            ");
            migrationBuilder.Sql(@"
                UPDATE VergiPilleleri
                   SET AsagiHedd   = 8000,
                       SabitMebleg = 160,
                       Aciqlama    = N'2026: 8000+ AZN → 160 + 0.5%'
                 WHERE Id = 10 AND Nov = 4 AND AsagiHedd = 2500 AND SabitMebleg = 50;
            ");
            migrationBuilder.Sql(@"
                UPDATE VergiPilleleri
                   SET YuxariHedd = 8000,
                       Aciqlama   = N'2026: 0–8000 AZN → 2%'
                 WHERE Id = 11 AND Nov = 9 AND AsagiHedd = 0 AND YuxariHedd = 2500;
            ");
            migrationBuilder.Sql(@"
                UPDATE VergiPilleleri
                   SET AsagiHedd   = 8000,
                       SabitMebleg = 160,
                       Aciqlama    = N'2026: 8000+ AZN → 160 + 0.5%'
                 WHERE Id = 12 AND Nov = 9 AND AsagiHedd = 2500 AND SabitMebleg = 50;
            ");
        }
    }
}

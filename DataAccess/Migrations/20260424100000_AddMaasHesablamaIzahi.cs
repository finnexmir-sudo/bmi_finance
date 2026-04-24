using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// Maas cədvəlinə HesablamaIzahi sütunu (nvarchar(max), nullable) əlavə edir.
    /// Hər hesablama zamanı addım-addım izahlar (List&lt;HesablamaIzahiDto&gt;) JSON
    /// kimi saxlanır ki, Detal səhifəsində mühasibə görsənsin — yekun rəqəmin
    /// hansı addımlardan (esas, qayıb kəsintisi, məzuniyyət, xəstəlik və s.)
    /// necə alındığını audit edə bilsin.
    ///
    /// İdempotentdir: köhnə DB-lərdə sütunu yaradır, yeni DB-lərdə (artıq
    /// varsa) heç nə etmir.
    /// </summary>
    public partial class AddMaasHesablamaIzahi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'HesablamaIzahi' AND Object_ID = Object_ID('Maaslar')
                )
                BEGIN
                    ALTER TABLE Maaslar ADD HesablamaIzahi nvarchar(max) NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE Name = 'HesablamaIzahi' AND Object_ID = Object_ID('Maaslar')
                )
                BEGIN
                    ALTER TABLE Maaslar DROP COLUMN HesablamaIzahi;
                END
            ");
        }
    }
}

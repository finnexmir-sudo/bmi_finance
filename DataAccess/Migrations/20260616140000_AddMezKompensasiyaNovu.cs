using FinNex.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinNex.DataAccess.Migrations
{
    /// <summary>
    /// "Məzuniyyət Kompensasiyası" maaş növü (Tip = Gelir).
    /// İstifadə edilməmiş əmək məzuniyyəti günlərinə görə kompensasiya maaş
    /// hesablamasında ayrıca gəlir detalı kimi yazılsın deyə əlavə olunur.
    /// İdempotentdir — artıq mövcuddursa heç nə etmir. NET/brüt DƏYİŞMİR
    /// (məbləğ onsuz da esasBrut-a daxildir; bu yalnız ayrıca detal/sütun yaradır).
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260616140000_AddMezKompensasiyaNovu")]
    public partial class AddMezKompensasiyaNovu : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.Sql(@"
                IF NOT EXISTS (SELECT * FROM [dbo].[MaasNovleri] WHERE [Ad] = N'Məzuniyyət Kompensasiyası')
                BEGIN
                    INSERT INTO [dbo].[MaasNovleri] ([Ad], [Tip], [Aktivdir], [Silinib], [YaradilmaTarixi])
                    VALUES (N'Məzuniyyət Kompensasiyası', 1, 1, 0, GETDATE());
                END
            ");
        }

        protected override void Down(MigrationBuilder m)
        {
            m.Sql("DELETE FROM [dbo].[MaasNovleri] WHERE [Ad] = N'Məzuniyyət Kompensasiyası';");
        }
    }
}

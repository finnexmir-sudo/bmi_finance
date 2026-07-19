/* ============================================================================
   MÜHASİBAT — Yerləşdirilmiş vəsaitlər (bank yerləşdirmələri) Oracle sorğusu
   ----------------------------------------------------------------------------
   Mənbə: arh_licsch_rs (RS = "razmeşşenie sredstv" — yerləşdirilmiş vəsait).
   Kredit üçün arh_licschkre-nin PARALELİ: bankın BAŞQA banklara / AMB-yə
   qoyduğu vəsaitlər (aktivin o biri tərəfi).

   Bir sətir = bir yerləşdirmə (say azdır — ~15). Servis C#-da bucket edir:
     - hesab5 = SUBSTR(licsch_rs,1,5) → 11xxx = AMB (overnight), 15xxx = banklararası
     - ad     = licsch.name_licsch (kontragent bank adı)
     - valyuta= SUBSTR(licsch_rs,6,2)
     - kurs   = func_get_kurval(valyuta, date_oper) → AZN
     - esas   = al.summa (cari qalıq; AZN valyutada kurs=1)
     - faiz   = al.procstav_rs (yerləşdirmə faiz dərəcəsi — gəlir)
     - ehtiyat_faiz = al.procstavrez (ehtiyat dərəcəsi — problemli kontragent)
     - planbaglanma = date_planclose (qalıq müddət/maturity + vaxtı keçmiş üçün)

   Açıq üzrə (date_close is null) — Kredit prinsipinin eynisi (CLAUDE.md):
   qalığı 0 olsa belə açıqdırsa görünür. Drill-down eyni sorğunu işlədir.

   Test (16/07/2026): 15 açıq yerləşdirmə, ~46.28M AZN
     - 11110 AMB overnight ≈ 40.82M (1, ehtiyatsız)
     - banklararası ≈ 5.47M (14, ~99% ehtiyatlanmış, 6-sı vaxtı keçmiş)
   Oracle YALNIZ SELECT. Azərbaycan hərfi YOXDUR. {TARIX} servis tərəfindən əvəz olunur.

   İdempotent: yoxdursa INSERT, varsa SorguMetni UPDATE (yeni ehtiyat_faiz sütunu üçün).
   ============================================================================ */
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;
SELECT @DepId = DepartamentId FROM OracleSorgular
WHERE  SorguAdi = N'Muhasibat — Balans qaliqlari' AND ISNULL(Silinib,0)=0;
IF @DepId IS NULL
    SELECT TOP 1 @DepId = Id FROM Departamentler
    WHERE (Ad LIKE N'%ühasib%' OR Ad LIKE N'%aliyy%') AND ISNULL(Silinib,0)=0 ORDER BY Id;

DECLARE @Sql NVARCHAR(MAX) = N'select
  al.licsch_rs muqavile,
  substr(al.licsch_rs,1,5) hesab5,
  l.name_licsch ad,
  substr(al.licsch_rs,6,2) valyuta,
  round(odb.func_get_kurval(substr(al.licsch_rs,6,2),al.date_oper),6) kurs,
  al.summa esas,
  nvl(al.procstav_rs,0) faiz,
  nvl(al.procstavrez,0) ehtiyat_faiz,
  al.date_open acilma,
  al.date_planclose planbaglanma,
  al.srok muddet
from arh_licsch_rs al, licsch l
where al.licsch_rs = l.licsch(+)
  and al.date_oper = to_date(''{TARIX}'',''dd/mm/yyyy'')
  and (al.date_close is null or al.date_close > to_date(''{TARIX}'',''dd/mm/yyyy''))
  and length(al.licsch_rs) = 20';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = N'Muhasibat — Yerlesdirme' AND ISNULL(Silinib,0)=0)
    UPDATE OracleSorgular
    SET    SorguMetni = @Sql,
           Mahiyyet = N'Yerləşdirilmiş vəsaitlər (arh_licsch_rs) — kontragent/valyuta/faiz/ehtiyat/müddət',
           Aktiv = 1
    WHERE  SorguAdi = N'Muhasibat — Yerlesdirme' AND ISNULL(Silinib,0)=0;
ELSE
    INSERT INTO OracleSorgular (SorguAdi, Mahiyyet, SorguMetni, Aktiv, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (N'Muhasibat — Yerlesdirme', N'Yerləşdirilmiş vəsaitlər (arh_licsch_rs) — kontragent/valyuta/faiz/ehtiyat/müddət',
            @Sql, 1, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Yerləşdirmə sorğusu əlavə olundu / yeniləndi (ehtiyat_faiz daxil).';

SELECT CASE WHEN SorguMetni LIKE '%ehtiyat_faiz%' THEN 'YENİ (ehtiyat_faiz var)' ELSE 'KÖHNƏ' END AS veziyyet
FROM   OracleSorgular WHERE SorguAdi = N'Muhasibat — Yerlesdirme' AND ISNULL(Silinib,0)=0;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'XƏTA: ' + ERROR_MESSAGE();
END CATCH

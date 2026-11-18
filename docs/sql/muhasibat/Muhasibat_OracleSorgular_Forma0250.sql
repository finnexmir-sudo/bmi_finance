/* ============================================================================
   FORMA 0250 — GENERAL LEDGER (CBAR)  | Mühasibat — OracleSorgular INSERT/UPDATE
   (SQL Server-də işlət, Oracle-da YOX). Script TƏKRAR İŞLƏDİLƏ BİLƏR — artıq
   mövcud olan sətri YENİLƏYİR (IF EXISTS...UPDATE), yoxdursa yaradır.

   Mənbə: istifadəçinin BMI-dən gətirdiyi, AMB-yə (CBAR) aylıq təqdim olunan
   "Forma 0250" Baş Kitab (General Ledger) sorğusu — balanslı hesablar
   (arh_saldo_ls) + balansdankənar (arh_dd, 6/7/8/9, 50130 istisna), KOD
   xəritəsi ilə aqreqasiya olunur. SQL mətni YALNIZ iki yerdə dəyişdirilib:

     &d1 / &d2  →  {D1} / {D2}   (servis tərəfindən dd-MM-yyyy formatında
                                   əvəz edilir — dövrün BAŞ/SON tarixi)

   Qalan hər şey — KOD xəritəsi, şərhlər, düstur — mənbə sorğudan HƏRFİ
   köçürülüb (yalnız T-SQL literal üçün tək dırnaqlar ikiqat edilib).

   ⚠️ YALNIZ-OXU — Oracle-a heç nə yazılmır (SELECT, CLAUDE.md-nin "Oracle
   Verilənlər Bazası" qaydasına uyğun).
   ========================================================================== */

SET NOCOUNT ON;
BEGIN TRY
BEGIN TRAN;

DECLARE @DepId INT;

/* Mühasibat / Maliyyə departamentini tap (digər Muhasibat sorğuları ilə EYNİ axtarış) */
SELECT TOP 1 @DepId = Id
FROM   Departamentler
WHERE  (Ad LIKE N'%ühasib%' OR Ad LIKE N'%aliyy%')
  AND  ISNULL(Silinib,0) = 0
ORDER BY Id;

IF @DepId IS NULL
BEGIN
    INSERT INTO Departamentler (Ad, Aciqlama, YaradilmaTarixi, Silinib)
    VALUES (N'Mühasibatlıq', N'Maliyyə / mühasibat hesabatları', GETDATE(), 0);
    SET @DepId = SCOPE_IDENTITY();
END

DECLARE @Ad NVARCHAR(200), @Mahiyyet NVARCHAR(MAX), @Sql NVARCHAR(MAX);

SET @Ad       = N'Muhasibat — Forma 0250 GL';
SET @Mahiyyet = N'Forma 0250 — General Ledger (CBAR). KOD üzrə aylıq qalıq/dövriyyə hesabatı — balanslı (arh_saldo_ls) + balansdankənar (arh_dd) hesablar, {D1}/{D2} dövr aralığı.';
SET @Sql = N'WITH
/* =====================================================================
   FORMA 0250 — GENERAL LEDGER (CBAR)  | Excel RTL goruntu sirasi (KOD en axirda)
   Dovr: {D1} .. {D2} (dd-mm-yyyy; sentyabr: 01-09-2026 / 30-09-2026)
   BALANSLI (1-5): arh_saldo_ls | BALANSDANKENAR (6/7/8/9): arh_dd (kredit-debet, 50130 istisna)
   ILLIK: cari ilin 01-yanvarindan. Hereket + -> KREDIT (gelir), - -> DEBET (xerc, giris -)
   KODLAR sheet 08 + sentyabr duzelisleri; ''1''->''90100''; ''61014''->''61414'' (yazi xetasi).
   SENTYABR DUZELISLERI (muhasib 0250 ile tutusdurma):
     - AZN sutri ucun closing_base = 0 (474 KOD-da 83M stray inval duzeldi)
     - 628: 25011,25019 (AZN), 25021 (EUR) elave; 478: 21142 elave; 1292: 67020 EUR elave
     - VALYUTA her zaman hesabin oz 6-7 reqeminden gelir (val_ad); map-den YALNIZ KOD goturulur
       (map5_kod). Belece 87026 -> 490 IRR (datanin heqiqi valyutasi), 45023 -> 1454 datanin reqemi.
       Qeyd: muhasib bezi hesablari sehven EUR/USD kimi gosterir (mes. 87026 EUR) - biz reqemden gidirik.
     - ISARE (muhasib 0250 ile tutusdurulmus):
         QALIQLAR (acilis/qapanis/baza) = -1 * xam = DEBET terefi menfi, KREDIT terefi musbet
           (bazada debet qaliq +, kredit qaliq - saxlanir; 50110 debet qaliq oldugu ucun cixisda -);
         DOVRIYYELER = hesabin novune/ilk reqemine gore (1,2 menfi, 3,4,5 musbet);
         balansdankenar (6-9) kredit-debet isaresini saxlayir (gelir +, xerc -).
   Valyuta: 00=AZN 01=USD 02=EUR 03=RUB 04=IRR 05=AED
   ===================================================================== */
bal AS (
    select substr(licsch,1,5) hes5, licsch hes20, substr(licsch,6,2) val_kod,
           sum(obd_n) debet, sum(obk_n) kredit,
           min(vhd_n) keep (dense_rank first order by date_oper) opening,
           min(ish_n) keep (dense_rank last  order by date_oper) closing_l,
           min(ish_i) keep (dense_rank last  order by date_oper) closing_b
    from (
        select l.licsch, l.date_oper,
               l.oboroti_debet_nacval obd_n, l.oboroti_kredit_nacval obk_n,
               l.saldo_vhd_nacval vhd_n, l.saldo_ish_nacval ish_n, l.saldo_ish_inval ish_i
        from odb.arh_saldo_ls l
        where l.date_oper between to_date(''{D1}'',''dd-mm-yyyy'') and to_date(''{D2}'',''dd-mm-yyyy'')
          and substr(l.licsch,1,1) not in (''6'',''7'',''8'',''9'')
    )
    group by substr(licsch,1,5), licsch, substr(licsch,6,2)
),
val AS (
    select ''00'' val_kod, ''AZN'' val_ad from dual union all
    select ''01'',''USD'' from dual union all
    select ''02'',''EUR'' from dual union all
    select ''03'',''RUB'' from dual union all
    select ''04'',''IRR'' from dual union all
    select ''05'',''AED'' from dual
),
-- KURS: son yazilan tarixin ({D2}) kursu, odb.func_get_kurval ile; valyuta uzre bir defe hesablanir
kurslar AS (
    select ''AZN'' valyuta, 1 kurs from dual
    union all select ''USD'', odb.func_get_kurval(''01'', ish_gun_cari1(to_date(''{D2}'',''dd-mm-yyyy''))) from dual
    union all select ''EUR'', odb.func_get_kurval(''02'', ish_gun_cari1(to_date(''{D2}'',''dd-mm-yyyy''))) from dual
    union all select ''RUB'', odb.func_get_kurval(''03'', ish_gun_cari1(to_date(''{D2}'',''dd-mm-yyyy''))) from dual
    union all select ''IRR'', odb.func_get_kurval(''04'', ish_gun_cari1(to_date(''{D2}'',''dd-mm-yyyy''))) from dual
    union all select ''AED'', odb.func_get_kurval(''05'', ish_gun_cari1(to_date(''{D2}'',''dd-mm-yyyy''))) from dual
),
kod_map(acc, acc_len, kod, valyuta) AS (
    select ''10010'' acc,5 acc_len,''602'' kod,''AZN'' valyuta from dual
    union all
    select ''10030'' acc,5 acc_len,''602'' kod,''AZN'' valyuta from dual
    union all
    select ''10050'' acc,5 acc_len,''602'' kod,''AZN'' valyuta from dual
    union all
    select ''10020'' acc,5 acc_len,''602'' kod,''USD'' valyuta from dual
    union all
    select ''10060'' acc,5 acc_len,''602'' kod,''USD'' valyuta from dual
    union all
    select ''10020'' acc,5 acc_len,''602'' kod,''EUR'' valyuta from dual
    union all
    select ''10060'' acc,5 acc_len,''602'' kod,''EUR'' valyuta from dual
    union all
    select ''11010000010000200000'' acc,20 acc_len,''81'' kod,''AZN'' valyuta from dual
    union all
    select ''11010000020000200000'' acc,20 acc_len,''77'' kod,''AZN'' valyuta from dual
    union all
    select ''11010000030000200000'' acc,20 acc_len,''77'' kod,''AZN'' valyuta from dual
    union all
    select ''11010000040000200000'' acc,20 acc_len,''77'' kod,''AZN'' valyuta from dual
    union all
    select ''11010000050000200000'' acc,20 acc_len,''77'' kod,''AZN'' valyuta from dual
    union all
    select ''11020010020000200000'' acc,20 acc_len,''77'' kod,''USD'' valyuta from dual
    union all
    select ''11020020010000200000'' acc,20 acc_len,''81'' kod,''EUR'' valyuta from dual
    union all
    select ''11020010010000200000'' acc,20 acc_len,''81'' kod,''USD'' valyuta from dual
    union all
    select ''11020020020000200000'' acc,20 acc_len,''77'' kod,''EUR'' valyuta from dual
    union all
    select ''11110'' acc,5 acc_len,''94'' kod,''AZN'' valyuta from dual
    union all
    select ''11112'' acc,5 acc_len,''94'' kod,''AZN'' valyuta from dual
    union all
    select ''14030'' acc,5 acc_len,''94'' kod,''AZN'' valyuta from dual
    union all
    select ''14032'' acc,5 acc_len,''94'' kod,''AZN'' valyuta from dual
    union all
    select ''15025020000001800000'' acc,20 acc_len,''621'' kod,''EUR'' valyuta from dual
    union all
    select ''15025020000126400000'' acc,20 acc_len,''621'' kod,''EUR'' valyuta from dual
    union all
    select ''15025020000975000000'' acc,20 acc_len,''621'' kod,''EUR'' valyuta from dual
    union all
    select ''15025030000975000000'' acc,20 acc_len,''621'' kod,''EUR'' valyuta from dual
    union all
    select ''15025020000001900000'' acc,20 acc_len,''638'' kod,''EUR'' valyuta from dual
    union all
    select ''15025040001451500000'' acc,20 acc_len,''638'' kod,''IRR'' valyuta from dual
    union all
    select ''15025040001774000000'' acc,20 acc_len,''638'' kod,''IRR'' valyuta from dual
    union all
    select ''15025050000001700000'' acc,20 acc_len,''638'' kod,''AED'' valyuta from dual
    union all
    select ''15213'' acc,5 acc_len,''622'' kod,''AZN'' valyuta from dual
    union all
    select ''15910'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''15911'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''20910'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''21910'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''21911'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''21912'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''21913'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''23911'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''27013'' acc,5 acc_len,''1274'' kod,''AZN'' valyuta from dual
    union all
    select ''20360'' acc,5 acc_len,''657'' kod,''AZN'' valyuta from dual
    union all
    select ''20660'' acc,5 acc_len,''657'' kod,''AZN'' valyuta from dual
    union all
    select ''20690'' acc,5 acc_len,''657'' kod,''AZN'' valyuta from dual
    union all
    select ''21140'' acc,5 acc_len,''657'' kod,''AZN'' valyuta from dual
    union all
    select ''21210'' acc,5 acc_len,''657'' kod,''AZN'' valyuta from dual
    union all
    select ''21240'' acc,5 acc_len,''657'' kod,''AZN'' valyuta from dual
    union all
    select ''21245'' acc,5 acc_len,''657'' kod,''AZN'' valyuta from dual
    union all
    select ''13012'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''13032'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''14082'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''15022'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''15027'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''15212'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''15214'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''15222'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''15224'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''15227'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''15622'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''15624'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20362'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20364'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20392'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20394'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20632'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20634'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20644'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20652'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20654'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20662'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20664'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20682'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20684'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20692'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20694'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''20764'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21072'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21074'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21077'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21079'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21112'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21114'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21117'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21119'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21122'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21127'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21129'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21142'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21144'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21212'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21214'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21217'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21219'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21222'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21224'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21227'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21229'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21242'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21244'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21247'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21249'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21252'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21254'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21257'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''21259'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''23124'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''23302'' acc,5 acc_len,''478'' kod,''AZN'' valyuta from dual
    union all
    select ''15623'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''20363'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''20393'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''20643'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''20653'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''20663'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''20693'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''20763'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21073'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21113'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21118'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21123'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21128'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21143'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21213'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21218'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21223'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21228'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21243'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21248'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''21253'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''21258'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual
    union all
    select ''23123'' acc,5 acc_len,''648'' kod,''AZN'' valyuta from dual   -- butun valyuta (sari)
    union all
    select ''24010'' acc,5 acc_len,''625'' kod,''AZN'' valyuta from dual
    union all
    select ''25100'' acc,5 acc_len,''625'' kod,''AZN'' valyuta from dual
    union all
    select ''25101'' acc,5 acc_len,''625'' kod,''AZN'' valyuta from dual
    union all
    select ''25019000000000300002'' acc,20 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25011'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25019'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25021'' acc,5 acc_len,''628'' kod,''EUR'' valyuta from dual
    union all
    select ''25052'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25059'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25121'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25122'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25123'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25159'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25270'' acc,5 acc_len,''628'' kod,''AZN'' valyuta from dual
    union all
    select ''25019000000000300003'' acc,20 acc_len,''467'' kod,''AZN'' valyuta from dual
    union all
    select ''27012'' acc,5 acc_len,''467'' kod,''AZN'' valyuta from dual
    union all
    select ''28020'' acc,5 acc_len,''466'' kod,''AZN'' valyuta from dual
    union all
    select ''28030'' acc,5 acc_len,''466'' kod,''AZN'' valyuta from dual
    union all
    select ''28040'' acc,5 acc_len,''466'' kod,''AZN'' valyuta from dual
    union all
    select ''28050'' acc,5 acc_len,''466'' kod,''AZN'' valyuta from dual
    union all
    select ''28060'' acc,5 acc_len,''466'' kod,''AZN'' valyuta from dual
    union all
    select ''28110'' acc,5 acc_len,''466'' kod,''AZN'' valyuta from dual
    union all
    select ''28120'' acc,5 acc_len,''466'' kod,''AZN'' valyuta from dual
    union all
    select ''28130'' acc,5 acc_len,''466'' kod,''AZN'' valyuta from dual
    union all
    select ''28021'' acc,5 acc_len,''1276'' kod,''AZN'' valyuta from dual
    union all
    select ''28031'' acc,5 acc_len,''1276'' kod,''AZN'' valyuta from dual
    union all
    select ''28041'' acc,5 acc_len,''1276'' kod,''AZN'' valyuta from dual
    union all
    select ''28051'' acc,5 acc_len,''1276'' kod,''AZN'' valyuta from dual
    union all
    select ''28061'' acc,5 acc_len,''1276'' kod,''AZN'' valyuta from dual
    union all
    select ''28111'' acc,5 acc_len,''1276'' kod,''AZN'' valyuta from dual
    union all
    select ''28121'' acc,5 acc_len,''1276'' kod,''AZN'' valyuta from dual
    union all
    select ''28131'' acc,5 acc_len,''1276'' kod,''AZN'' valyuta from dual
    union all
    --45270000050000400000
    select ''35020'' acc,5 acc_len,''1485'' kod,''EUR'' valyuta from dual
    union all
    select ''35025010000001600000'' acc,20 acc_len,''1513'' kod,''USD'' valyuta from dual
    union all
    select ''35025020000001600000'' acc,20 acc_len,''1513'' kod,''EUR'' valyuta from dual
    union all
    select ''35026'' acc,5 acc_len,''1513'' kod,''EUR'' valyuta from dual
    union all
    select ''35025020000263300000'' acc,20 acc_len,''1488'' kod,''EUR'' valyuta from dual
    union all
    select ''35025020001033300000'' acc,20 acc_len,''1488'' kod,''EUR'' valyuta from dual
    union all
    select ''35938'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''38939'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''39010'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''39939'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40030'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40040'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40050'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40060'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40065'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40070'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40090'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40932'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40933'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40935'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''40939'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''41010'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''41015'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''41040'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''41045'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''41930'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''41931'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''41932'' acc,5 acc_len,''1445'' kod,''AZN'' valyuta from dual
    union all
    select ''35940'' acc,5 acc_len,''1485'' kod,''USD'' valyuta from dual
    union all
    select ''38943'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''38949'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''39020'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''39080'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''39940'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''39949'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40140'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40150'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40160'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40170'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40190'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40942'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40943'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40944'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40945'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40946'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''40949'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''41020'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''41025'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''41940'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''41941'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''41942'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''41943'' acc,5 acc_len,''1445'' kod,''USD'' valyuta from dual
    union all
    select ''39020'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''39080'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''39940'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''40140'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''40150'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''40160'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''40170'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''40190'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''40943'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''40945'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''41020'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''41025'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''41940'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''41941'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''41942'' acc,5 acc_len,''1445'' kod,''EUR'' valyuta from dual
    union all
    select ''44510'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all
    select ''45011'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all
    select ''45013'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all
    select ''45019'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all
    select ''45050'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all
    select ''45079'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all
    --bura elave edildi
    select ''35770'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all
    select ''35772'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all
    select ''45010'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all

    select ''45021'' acc,5 acc_len,''1454'' kod,''USD'' valyuta from dual
    union all
    select ''45021'' acc,5 acc_len,''1454'' kod,''EUR'' valyuta from dual
    union all
    select ''45021'' acc,5 acc_len,''1454'' kod,''RUB'' valyuta from dual
    union all
    select ''45021'' acc,5 acc_len,''1454'' kod,''IRR'' valyuta from dual
    union all
    select ''45021'' acc,5 acc_len,''1454'' kod,''AED'' valyuta from dual
    union all
    select ''45023'' acc,5 acc_len,''1454'' kod,''USD'' valyuta from dual
    union all
    select ''45029'' acc,5 acc_len,''1454'' kod,''EUR'' valyuta from dual
    union all

    select ''45080'' acc,5 acc_len,''1454'' kod,''USD'' valyuta from dual
    union all
    select ''45080'' acc,5 acc_len,''1454'' kod,''EUR'' valyuta from dual
    union all

    select ''45150'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all

    select ''45159'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all

    select ''45160'' acc,5 acc_len,''1454'' kod,''USD'' valyuta from dual
    union all
    select ''45160'' acc,5 acc_len,''1454'' kod,''EUR'' valyuta from dual
    union all

    select ''45089'' acc,5 acc_len,''1454'' kod,''EUR'' valyuta from dual
    union all

    select ''45232'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all

    select ''45270'' acc,5 acc_len,''1454'' kod,''AZN'' valyuta from dual
    union all

    --''45270000020000400000'' ,''45270000040000400000'' istisna

    select ''45280'' acc,5 acc_len,''1454'' kod,''USD'' valyuta from dual
    union all
    -- ''45280010010000400000'',''45280010020000400000'' istisna

    select ''45280'' acc,5 acc_len,''1454'' kod,''EUR'' valyuta from dual
    union all

    --

    select ''45100'' acc,5 acc_len,''1512'' kod,''AZN'' valyuta from dual
    union all
    select ''45103'' acc,5 acc_len,''1512'' kod,''AZN'' valyuta from dual
    union all
    select ''45105'' acc,5 acc_len,''1512'' kod,''AZN'' valyuta from dual
    union all
    select ''45110'' acc,5 acc_len,''1512'' kod,''AZN'' valyuta from dual


    union all
    select ''45270000020000400000'' acc,20 acc_len,''1462'' kod,''AZN'' valyuta from dual
    union all
    select ''45270000040000400000'' acc,20 acc_len,''1462'' kod,''AZN'' valyuta from dual
    --

    union all
    select ''45280010010000400000'' acc,20 acc_len,''1462'' kod,''USD'' valyuta from dual
    union all
    select ''45280010020000400000'' acc,20 acc_len,''1462'' kod,''USD'' valyuta from dual

    union all
    select ''49025'' acc,5 acc_len,''1518'' kod,''EUR'' valyuta from dual
    union all
    select ''50020'' acc,5 acc_len,''1500'' kod,''AZN'' valyuta from dual
    union all
    select ''50110'' acc,5 acc_len,''957'' kod,''AZN'' valyuta from dual
    union all
    select ''60210'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''60310'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''61012'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''61414'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''63215'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''63219'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''63710'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''64012'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''64017'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''64032'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''64037'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''64710'' acc,5 acc_len,''1284'' kod,''AZN'' valyuta from dual
    union all
    select ''66220'' acc,5 acc_len,''1315'' kod,''AZN'' valyuta from dual
    union all
    select ''68010'' acc,5 acc_len,''1315'' kod,''AZN'' valyuta from dual
    union all
    select ''67010'' acc,5 acc_len,''1292'' kod,''AZN'' valyuta from dual
    union all
    select ''67012'' acc,5 acc_len,''1292'' kod,''AZN'' valyuta from dual
    union all
    select ''67013'' acc,5 acc_len,''1292'' kod,''AZN'' valyuta from dual
    union all
    select ''67019'' acc,5 acc_len,''1292'' kod,''AZN'' valyuta from dual
    union all
    select ''67034'' acc,5 acc_len,''1292'' kod,''AZN'' valyuta from dual
    union all
    select ''67039'' acc,5 acc_len,''1292'' kod,''AZN'' valyuta from dual
    union all
    select ''67020'' acc,5 acc_len,''1292'' kod,''USD'' valyuta from dual
    union all
    select ''67020'' acc,5 acc_len,''1292'' kod,''EUR'' valyuta from dual
    union all
    select ''67022'' acc,5 acc_len,''1292'' kod,''USD'' valyuta from dual
    union all
    select ''67023'' acc,5 acc_len,''1292'' kod,''USD'' valyuta from dual
    union all
    select ''67022'' acc,5 acc_len,''1292'' kod,''EUR'' valyuta from dual
    union all
    select ''67023'' acc,5 acc_len,''1292'' kod,''EUR'' valyuta from dual
    union all
    select ''70014'' acc,5 acc_len,''1307'' kod,''AZN'' valyuta from dual
    union all
    select ''70110'' acc,5 acc_len,''1307'' kod,''AZN'' valyuta from dual
    union all
    select ''86220'' acc,5 acc_len,''505'' kod,''AZN'' valyuta from dual
    union all
    select ''88010'' acc,5 acc_len,''505'' kod,''AZN'' valyuta from dual
    union all
    select ''87010'' acc,5 acc_len,''490'' kod,''AZN'' valyuta from dual
    union all
    select ''87016'' acc,5 acc_len,''490'' kod,''AZN'' valyuta from dual
    union all
    select ''87017'' acc,5 acc_len,''490'' kod,''AZN'' valyuta from dual
    union all
    select ''87030'' acc,5 acc_len,''490'' kod,''AZN'' valyuta from dual
    union all
    select ''87026'' acc,5 acc_len,''490'' kod,''EUR'' valyuta from dual
    union all
    select ''89021'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''89115'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''89119'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''89150'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''89151'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''89160'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''89161'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''89180'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90020'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90021'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90022'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90025'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90029'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90039'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90040'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90041'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90042'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90048'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90050'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90051'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90052'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90053'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90054'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90060'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90061'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90070'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90071'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90073'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90074'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90075'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90080'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90081'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90082'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90083'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90085'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90087'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90089'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90090'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90111'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90112'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90114'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90115'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90116'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90118'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90119'' acc,5 acc_len,''474'' kod,''AZN'' valyuta from dual
    union all
    select ''90100'' acc,5 acc_len,''474'' kod,''USD'' valyuta from dual
    union all
    select ''90100'' acc,5 acc_len,''474'' kod,''EUR'' valyuta from dual
),
map20 AS (select acc, kod from kod_map where acc_len=20),
map5  AS (select acc, kod, valyuta from kod_map where acc_len=5),
map5_kod AS (               -- hesab -> KOD. Valyuta map-den DEYIL, hesabin oz 6-7 reqeminden gelir.
    select acc, max(kod) kod
    from map5 group by acc
),

on_mapped AS (
    -- 20 reqemli hesablar: kod map20-den, valyuta hesabin oz reqemlerinden (01/02/..)
    -- sgn = DOVRIYYE isaresi (hesab novune/ilk reqeme gore): 1,2 -> -1, 3,4,5 -> +1.
    -- QALIQLAR ise final-de universal -1*xam ile cevrilir (debet qaliq -, kredit qaliq +).
    select ''ON'' src, v.val_ad valyuta, m20.kod kod,
           b.opening, b.debet, b.kredit, b.closing_l, b.closing_b,
           case when substr(b.hes20,1,1) in (''1'',''2'') then -1 else 1 end sgn
    from bal b
    join map20 m20 on m20.acc = b.hes20
    left join val v on v.val_kod = b.val_kod
    union all
    -- 5 reqemli hesablar: KOD map5_kod-dan (hesab uzre), VALYUTA hesabin oz reqeminden (val_ad)
    select ''ON'' src, v.val_ad valyuta, mk.kod kod,
           b.opening, b.debet, b.kredit, b.closing_l, b.closing_b,
           case when substr(b.hes5,1,1) in (''1'',''2'') then -1 else 1 end sgn
    from bal b
    left join val v on v.val_kod = b.val_kod
    left join map5_kod mk on mk.acc = b.hes5
    where not exists (select 1 from map20 m20 where m20.acc = b.hes20)
),

offbal_raw AS (
    select substr(a.kredit,1,5) hes5, substr(a.kredit,6,2) val_kod,
           a.date_oper,  a.summa_v_nacval amt_n,  a.summa_v_inval amt_i
    from odb.arh_dd a
    where substr(a.kredit,1,1) in (''6'',''7'',''8'',''9'')
      and substr(a.debet,1,5) <> ''50130''
      and a.date_oper >= trunc(to_date(''{D1}'',''dd-mm-yyyy''),''YYYY'')
      and a.date_oper <  to_date(''{D2}'',''dd-mm-yyyy'')+1
    union all
    select substr(a.debet,1,5) hes5, substr(a.debet,6,2) val_kod,
           a.date_oper, -a.summa_v_nacval,        -a.summa_v_inval
    from odb.arh_dd a
    where substr(a.debet,1,1) in (''6'',''7'',''8'',''9'')
      and substr(a.kredit,1,5) <> ''50130''
      and a.date_oper >= trunc(to_date(''{D1}'',''dd-mm-yyyy''),''YYYY'')
      and a.date_oper <  to_date(''{D2}'',''dd-mm-yyyy'')+1
),
offbal AS (
    select hes5, val_kod,
           sum(case when date_oper < to_date(''{D1}'',''dd-mm-yyyy'') then amt_n else 0 end) opening,
           sum(amt_n) closing_l,
           sum(amt_i) closing_b
    from offbal_raw
    group by hes5, val_kod
),
off_mapped AS (
    -- balansdankenar (6-9): isare artiq kredit-debet mentiqinden gelir (gelir +, xerc -), sgn=1
    -- KOD map5_kod-dan, VALYUTA hesabin oz reqeminden (val_ad)
    select ''OFF'' src, v.val_ad valyuta, mk.kod kod,
           o.opening, 0 debet, 0 kredit, o.closing_l, o.closing_b, 1 sgn
    from offbal o
    left join val v on v.val_kod = o.val_kod
    left join map5_kod mk on mk.acc = o.hes5
),

all_mapped AS (
    -- xam deyerler + sgn (balansli hesabin hedef isaresi); isare final-de tetbiq olunur
    select src, valyuta, kod, opening, debet, kredit, closing_l, closing_b, sgn from on_mapped
    union all
    select src, valyuta, kod, opening, debet, kredit, closing_l, closing_b, sgn from off_mapped
),
agg AS (
    select kod, valyuta, max(src) src, max(sgn) sgn,
           sum(opening) op, sum(debet) db, sum(kredit) cr,
           sum(closing_l) cl, sum(closing_b) cb
    from all_mapped
    where kod is not null
    group by kod, valyuta
    -- butun qaliqlar/dovriyyeler 0 olan sətirler verilmir
    having abs(sum(opening)) + abs(sum(closing_l))
         + abs(sum(debet))   + abs(sum(kredit)) >= 0.005
)
/* BALANSLI (src=ON) isare qaydasi (muhasib 0250 ile uygun):
     - QALIQLAR (acilis/qapanis/baza): universal * (-1) = DEBET/KREDIT terefine gore isare.
       (Bazada debet qaliq musbet, kredit qaliq menfi saxlanir; -1*xam -> debet qaliq -, kredit qaliq +.
        Numune: 50110 bazada +20258575 (debet qaliq) -> cixisda -20258575. Istisnaya ehtiyac yoxdur.)
     - DOVRIYYELER (debet/kredit): hesabin novune/ilk reqemine gore -> 1,2 menfi, 3,4,5 musbet (sgn).
   BALANSDANKENAR (src=OFF): oz kredit-debet isaresi (gelir +, xerc -) saxlanir, deyismir.
   KURS: rate = son yazilan tarixin ({D2}) kursu (func_get_kurval, oldugu kimi).
         closing_local = AZN kitab deyeri (saldo_ish_nacval) -- esas deyer, deyismir.
         closing_base  = valyuta mebleği = closing_local / kurs (AZN cari kursa bolunur).
         DIQQET: inval (saldo_ish_inval) ISLEDILMIR -- memorial hesablarda (90100) inval kohne
         kursda qalir (141 = 276.78/1.963), muhasib ise AZN/cari_kurs edir (276.78/1.9264=143.68). */
select
    case when agg.valyuta=''AZN'' then null
         else k.kurs end                                                       as rate,
    -- closing_base = AZN (closing_local) / cari kurs; AZN -> 0
    case when agg.valyuta=''AZN'' then 0
         when nvl(k.kurs,0) = 0 then null
         when src=''OFF''     then round(cl / k.kurs, 2)
         else                    round(-cl / k.kurs, 2) end                    as closing_base,
    -- closing_local = AZN kitab deyeri (saldo_ish_nacval), isare ile
    case when src=''OFF'' then round(cl,2)
         else                round(-cl,2) end                                  as closing_local,
    round(case when src=''OFF'' then greatest(cl-op,0)
               else sgn*cr end, 2)                                             as credit_turnover,
    round(case when src=''OFF'' then greatest(op-cl,0)
               else sgn*db end, 2)                                             as debit_turnover,
    case when src=''OFF'' then round(op,2)
         else                round(-op,2) end                                  as opening_local,
    agg.valyuta  as currencytype,
    agg.kod      as kod
from agg
join kurslar k on k.valyuta = agg.valyuta
order by to_number(agg.kod), agg.valyuta;
';

IF EXISTS (SELECT 1 FROM OracleSorgular WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0)
    UPDATE OracleSorgular
       SET SorguMetni = @Sql, Mahiyyet = @Mahiyyet, Aktiv = 1, DepartamentId = @DepId
     WHERE SorguAdi = @Ad AND ISNULL(Silinib,0) = 0;
ELSE
    INSERT INTO OracleSorgular
           (SorguAdi, Mahiyyet, SorguMetni, Aktiv, Kataloq, DepartamentId, YaradilmaTarixi, Silinib)
    VALUES (@Ad, @Mahiyyet, @Sql, 1, 0, @DepId, GETDATE(), 0);

COMMIT TRAN;
PRINT N'Muhasibat — Forma 0250 GL sorğusu yazıldı/yeniləndi.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH

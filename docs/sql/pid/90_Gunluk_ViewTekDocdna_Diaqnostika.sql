/* ============================================================================
   PİD Gündəlik Kredit Ödənişləri — view_tek_docdna diaqnostikası
   ----------------------------------------------------------------------------
   Yalnız SELECT — heç nəyi dəyişmir. PL/SQL Developer-də, FOXPRO (tətbiq) hesabı
   ilə icra et, nəticələri göndər.

   Məqsəd: köçürmə (otvet_ispoln=0) sətirləri odb.docdna-da debet=NULL görünür
   (əvvəlki NVL(LENGTH(d.debet),0) düzəlişi buna görə edildi). İstifadəçi:
   "view_tek_docdna cedvelinden baxmaliydin,otvet_ispoln 0 olan" — yəni real
   mənbə BU view-dir, odb.docdna YOX (və ya docdna-nın özü natamamdır).
   ============================================================================ */

-- 1) view_tek_docdna sütunları (adları TƏXMİN ETMƏ, buradan oxu)
SELECT column_name, data_type, data_length
  FROM all_tab_columns
 WHERE table_name = 'VIEW_TEK_DOCDNA'
 ORDER BY column_id;

-- 2) otvet_ispoln sütunu bu view-də varmı, bugünkü köçürmə sətirləri necə görünür
SELECT *
  FROM odb.view_tek_docdna
 WHERE otvet_ispoln = 0
   AND ROWNUM <= 10;

-- 3) Eyni gün üçün docdna ilə view_tek_docdna-nın otvet_ispoln=0 say fərqi
SELECT
  (SELECT COUNT(*) FROM odb.docdna          WHERE otvet_ispoln = 0) AS docdna_sayi,
  (SELECT COUNT(*) FROM odb.view_tek_docdna WHERE otvet_ispoln = 0) AS view_tek_docdna_sayi
FROM dual;

-- 4) view_tek_docdna-da debet/kredit sütunlarının adı docdna ilə EYNİDİRMİ —
--    (nəticə 1-dən sonra, sütun adları bilinəndə bu sorğunu ADLARA GÖRƏ düzəlt)
-- SELECT debet, kredit, recnum, summa_v_nacval, date_oper, primechanie
--   FROM odb.view_tek_docdna
--  WHERE otvet_ispoln = 0
--    AND ROWNUM <= 10;

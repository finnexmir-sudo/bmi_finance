/* ============================================================================
   IFRS 9 — A1 sahə mapping üçün: cari portfeldəki bütün index_otrasli kodları
   ----------------------------------------------------------------------------
   AMB A1 cədvəli kreditləri belə qruplaşdırır:
     1. Biznes: 1.1 Sənaye · 1.2 Kənd təs. · 1.3 Tikinti · 1.4 Nəqliyyat ·
                1.5 İnformasiya/Rabitə · 1.6 Ticarət · 1.7 Digər
     2. İstehlak: 2.1 Təmir · 2.2 Avtomobil · 2.3 Məişət · 2.4 Kart · 2.5 Digər
     3. Daşınmaz əmlak (ipoteka)
     4. Digər

   Hər index_otrasli kodunun bu sətirlərdən hansına düşdüyünü təyin etmək üçün
   siyahını çıxarırıq (kod + ad + tipkredita + say + qalıq).
   ============================================================================ */
SELECT ar.index_otrasli                 AS sahe_kodu,
       io.name_index_otrasli            AS sahe_adi,
       ar.tipkredita                    AS tip,   -- 1 hüquqi, 2 fiziki, 3 sahibkar
       COUNT(*)                         AS say,
       ROUND(SUM((ar.summa+ar.summa_19)
             *ROUND(odb.func_get_kurval(substr(ar.licschkre,6,2),ar.date_oper),6)),2) AS qaliq
FROM   arh_licschkre ar
JOIN   index_otrasli io ON io.index_otrasli = ar.index_otrasli
WHERE  ar.date_oper = (SELECT MAX(date_oper) FROM arh_licschkre
                       WHERE date_oper <= TO_DATE('&tarix','DD-MM-YYYY'))
  AND  ar.date_close IS NULL
  AND  LENGTH(ar.licschkre)=20
GROUP  BY ar.index_otrasli, io.name_index_otrasli, ar.tipkredita
ORDER  BY ar.tipkredita, ar.index_otrasli;

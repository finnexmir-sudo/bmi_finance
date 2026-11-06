-- ============================================================================
-- GOAML — AYSEL-in "Təsdiq et" / Save zamanı özü doldurduğu sahələrin
-- MƏNBƏYİNİ (hansı SELECT/cədvəldən oxuduğunu) tapmaq üçün diaqnostika
-- ============================================================================
-- MƏQSƏD: operator AYSEL-də bir namizədi "Təsdiq et" edib "Save" edəndə
-- proqram ekranda bəzi KYC sahələrini (ad, ünvan, sənəd №...) ÖZÜ doldurur —
-- bu, bank reyestrlərindən (V_FIZIKI / VIEW_HUQUQI_SHEXS_ALL / LICSCH_ALL_2
-- və s.) oxuyan daxili sorğulardır. Bu skript HƏMİN sorğuların mətnini canlı
-- Oracle-un SQL keşindən (v$sql) tutur — eynilə əvvəlki sessiyada "Update New"
-- kaskadının INSERT/UPDATE mətninin tapıldığı ÜSULLA.
--
-- NECƏ İŞLƏT:
--   1. Bu skripti PL/SQL Developer-də, ƏVVƏLKİ v$sql araşdırmasında işlətdiyin
--      EYNİ hesabla aç (ODB və ya DBA — v$ cədvəlləri adi istifadəçiyə
--      GÖRÜNMƏYƏ bilər, FOXPRO-nun bu cədvəllərə icazəsi olmaya bilər).
--      HƏLƏ İCRA ETMƏ.
--   2. AYSEL-i aç, BİR namizəd seç. O şəxsin FİN-ini / VÖEN-ini / hesab
--      nömrəsini bir kənara yaz (B hissəsi üçün lazım olacaq).
--   3. AYSEL-də "Təsdiq et" düyməsinə bas, sonra "Save" et.
--   4. DƏRHAL (1 dəqiqədən artıq gözləmə — v$sql keşi tez dolub köçürülür)
--      aşağıdakı A hissəsini icra et.
--   5. Nəticəni TAM — hər sətrin SQL_TEXT sütununu QISALTMADAN (PL/SQL
--      Developer-də hücrəyə klikləyib aşağı paneldə tam mətni gör, yaxud
--      sağ-klik → "View") — mənə göndər.
--   6. A çox sətir qaytarsa (başqa istifadəçilərin trafiki qarışa bilər),
--      B hissəsini 2-ci addımda yazdığın FİN/VÖEN/hesab ilə işlət — bu,
--      HƏMİN DƏYƏRİ bind parametri kimi oxuyan sorğu(lar)ı dəqiq tapır,
--      SQL_TEXT-də ':B1' kimi yer tutucu olsa belə.
-- ============================================================================

-- ── A) Son fəaliyyətdən bəri icra olunan, "reyestr axtarışına bənzəyən"
--      sorğular (açar sözlərə görə süzülüb, schema-ya görə YOX — AYSEL-in
--      hansı hesabla bağlandığını dəqiq bilmirik) ──────────────────────────
select s.sql_id,
       s.last_active_time,
       s.executions,
       s.parsing_schema_name,
       s.module,
       s.program,
       s.sql_text
from   v$sql s
where  s.last_active_time >= sysdate - (10/1440)   -- son 10 dəqiqə
  and  (
         upper(s.sql_text) like '%V_FIZIKI%'
      or upper(s.sql_text) like '%HUQUQI_SHEXS%'
      or upper(s.sql_text) like '%LICSCH_ALL%'
      or upper(s.sql_text) like '%REGNOM%'
      or upper(s.sql_text) like '%DOCFIO%'
      or upper(s.sql_text) like '%PINCODE%'
      or upper(s.sql_text) like '%GOAML_CLIENTS%'
      )
order by s.last_active_time desc;

-- Çox sətir çıxırsa, AYSEL-in bağlandığı hesabı bilirsənsə bunu əlavə et:
-- and s.parsing_schema_name = '<AYSEL_HESABI>'

-- ── B) DƏQIQ bu FİN/VÖEN/hesabın dəyərini oxuyan sorğu(lar) — bind
--      dəyərinə görə tapır (v$sql_bind_capture hər bağlı dəyəri saxlayır) ──
-- '<BURAYA_FIN_VEYA_HESAB_YAZ>' yerinə 2-ci addımda qeyd etdiyin dəyəri yaz.
select bc.sql_id,
       bc.position,
       bc.value_string,
       s.last_active_time,
       s.sql_text
from   v$sql_bind_capture bc
join   v$sql s on s.sql_id = bc.sql_id and s.child_number = bc.child_number
where  bc.value_string = '<BURAYA_FIN_VEYA_HESAB_YAZ>'
  and  s.last_active_time >= sysdate - (10/1440)
order by s.last_active_time desc;

-- ============================================================================
-- A/B boş qayıdarsa, ehtimal iki şeydən biridir:
--   1) Pəncərə (10 dəqiqə) ötüb, köhnə sorğu keşdən köçürülüb — 10/1440-i
--      30/1440-ə qədər böyüt, addım 3-4-ü təkrarla, sorğunu İCRA ANINDAN
--      DƏRHAL sonra işlət.
--   2) AYSEL "Təsdiq et"-i server tərəfdə YOX, formanın öz client-side
--      PL/SQL-ində (fmb-in daxilində) işlədir — bu halda sorğu Oracle-un
--      SQL keşinə HEÇ DÜŞMÜR, v$sql bunu görmür. Belə olsa, AYSEL-in forma
--      faylının (.fmb) özünə baxmaq (Forms Builder-də) lazım gələcək —
--      ayrıca araşdırma tələb edir.
-- ============================================================================

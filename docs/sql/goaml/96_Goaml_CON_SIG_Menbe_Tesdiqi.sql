-- ═══════════════════════════════════════════════════════════════════════
--  GOAML — CON/SIG-in ƏSL mənbəyini TƏSDİQLƏMƏK üçün ikinci dalğa
--
--  95-ci faylın nəticəsi göstərdi:
--    - REGNOM-un ÖZÜNDƏ (ingiliscə adlı sütunlar: NAME/SURNAME/PATRONYMIC/
--      BIRTHDATE/BIRTHADDRESS/GENDER/REGISTRATIONADDRESS/EXPIREDATE/
--      GIVENDATE/GIVENORGANIZATOIN/CITIZENSHIP/EMPLOYER/PIN/DOCUMENTNUMBER)
--      CON/SIG-in tələb etdiyi demək olar HƏR SAHƏ var — amma bu, HESAB
--      SƏVİYYƏLİ (müştərinin ÜMUMİ profili), əməliyyata bağlı DEYİL.
--    - `DOCFIO` cədvəlinin sütunları (DATE_OPER+DEBET+KREDIT+RECNUM açarı,
--      FIRST_NAME/MIDDLE_NAME/LAST_NAME/BIRTHDATE/BIRTH_PLACE/SSN/
--      NATIONALITY1/ADDRESS.../ISSUE_DATE/EXPIRY_DATE/ISSUED_BY/
--      ISSUE_COUNTRY) GOAML-in CON/SIG/TO adlandırma konvensiyasına demək
--      olar HƏRFİ uyğundur VƏ açarı bizim namizəd əməliyyatın öz açarı
--      (DATE_OPER/DEBET/KREDIT/RECNUM) ilə EYNİDİR — yəni bu, ƏMƏLİYYATA
--      BAĞLI (həmin konkret əməliyyatı kim aparıb), hesaba bağlı DEYİL.
--
--  Sual: AYSEL real təsdiq edəndə CON/SIG-i HANSI mənbədən doldurur —
--  DOCFIO (əməliyyata bağlı) yoxsa REGNOM (hesaba bağlı)? Bunu real,
--  ARTIQ TƏSDİQLƏNMİŞ bir `GOAML_CLIENTS` sətri ilə YOXLAYIRIQ: adı/soyadı
--  dolu olan bir sətir tapıb, onun açarı ilə HƏR İKİ mənbəni sorğulayırıq —
--  hansı mənbənin adı həmin sətirlə ÜST-ÜSTƏ düşürsə, ƏSL mənbə odur.
--
--  İCRA (PL/SQL Developer-də, ayrı-ayrı): ────────────────────────────────


-- ── A) `GOAML_CLIENTS`-dən CON adı DOLU olan son 5 sətir — açarı ilə birgə ──
select date_oper, filialnumber, recnum, debet, kredit,
       first_name_con, middle_name_con, last_name_con,
       first_name_sig, middle_name_sig, last_name_sig
from odb.goaml_clients
where first_name_con is not null or first_name_sig is not null
order by date_oper desc
fetch first 5 rows only;


-- ── B) A-dan çıxan İLK sətrin DATE_OPER/DEBET/KREDIT/RECNUM-unu aşağıya
--      köçürün, DOCFIO-da eyni açarla axtarın — adlar üst-üstə düşürmü? ────
select *
from odb.docfio
where date_oper = TO_DATE('__A-DAN_KOCUR__', 'DD.MM.YYYY')
  and recnum = __A-DAN_KOCUR__
  and (debet = '__A-DAN_KOCUR__' or kredit = '__A-DAN_KOCUR__');


-- ── C) EYNİ sətrin DEBET/KREDİT hesablarını REGNOM-dan (FİZİKİ olub-olmadığını
--      yoxlayaraq) sorğulayın — adlar REGNOM-dakı ilə də üst-üstə düşürmü? ──
select r.fizik, r.name, r.surname, r.patronymic, r.birthdate, r.pin
from odb.regnom r, odb.licsch_all_2 l
where l.licsch in ('__A-DAN_DEBET__', '__A-DAN_KREDIT__')
  and l.pincode = r.pincode;


-- ── D) `NAME_GOAML_CLIENTS` view-unun tərifi — bəlkə bu view artıq hazır
--      CON/SIG-i avtomatik qurur, biz onu birbaşa oxuya bilərik ───────────
select text from all_views where owner = 'ODB' and view_name = 'NAME_GOAML_CLIENTS';


-- ── E) HƏR EHTİMALA QARŞI — FİZİKİ (FIZIK=1) bir hesab üçün REGNOM-un
--      ingiliscə sütunlarının HƏQİQƏTƏN dolu olub-olmadığını görək (95-ci
--      faylın test hesabları hər ikisi hüquqi şəxs çıxdı, fiziki nümunə
--      hələ görməmişik). BIRTHDATE-in HANSI FORMATDA (DD.MM.YYYY? YYYY-MM-DD?)
--      yazıldığını da bu sorğudan görəcəyik. ────────────────────────────
select pincode, name, surname, patronymic, birthdate, birthaddress, gender,
       registrationaddress, citizenship, employer, documentnumber,
       givendate, expiredate, givenorganizatoin
from odb.regnom
where fizik = 1 and name is not null
fetch first 5 rows only;

/* ═══════════════════════════════════════════════════════════════════════════
   AML → «Hesab üzrə sorğu» — ŞAPKA (giriş/son qalıq) BOŞ GƏLİR
   Diaqnostika · 31.08.2026
   ───────────────────────────────────────────────────────────────────────────
   ORACLE — YALNIZ SELECT. Bu faylda heç bir yazı əməliyyatı YOXDUR.

   PROBLEM: ekranda «Hesabın adı», «Giriş qalığı», «Son qalıq» — üçü də «—».
   Əməliyyat sətirləri isə gəlir (3 sətir). Yəni əsas sorğu işləyir,
   yalnız şapka sorğusu (AML_HESAB_SORGU_QALIQ) 0 sətir qaytarır.

   NİYƏ ÜÇÜ BİRDƏN İTİR: şapka sorğusu ÜÇ dəsti DAXİLİ JOIN edir —
       p  (TARIX1-dəki qalıq sətri)
       k  (TARIX2-nin cari iş günündəki qalıq sətri)
       ac (odb.accounts — hesabın adı)
   Biri boş olsa nəticə tamamilə boş olur və ad da itir.
   Aşağıdakı addımlar hansının boş olduğunu göstərir.

   DƏYƏRLƏR ekrandakı sorğudan götürülüb — başqa hesabda yoxlayırsınızsa
   üçünü də dəyişin:
       HESAB  = 41010000000008700000
       TARIX1 = 28/08/2026
       TARIX2 = 31/08/2026
   ═══════════════════════════════════════════════════════════════════════════ */


/* ── ADDIM 0 — «cari iş günü» funksiyası nə qaytarır? ──────────────────────
   Şapkada son qalıq üçün TARIX2 birbaşa YOX, odb.ish_gun_cari1(TARIX2) ilə
   axtarılır. Bu gün 31.08.2026-dır — GÜNÜN SONUNA qalıqlar adətən gün
   bağlananda yazılır, yəni bugünkü sətir hələ olmaya bilər.
   Bu, ən güclü şübhəmizdir.                                                  */
select to_char(odb.ish_gun_cari1(to_date('31/08/2026','dd/mm/yyyy')),'dd/mm/yyyy') cari_is_gunu
  from dual;


/* ── ADDIM 1 — «p» dəsti: TARIX1-də qalıq sətri VARMI? ────────────────────
   Boş qayıtsa → giriş qalığı tapılmır (səbəb budur).                        */
select t.licsch,
       to_char(t.date_oper,'dd/mm/yyyy') date_oper,
       t.saldo_vhd_nacval,     -- günün ƏVVƏLİNƏ (əsl «giriş qalığı»)
       t.saldo_ish_nacval,     -- günün SONUNA
       t.saldo_vhd_inval,
       t.saldo_ish_inval
  from odb.arh_saldo_ls t
 where t.licsch = '41010000000008700000'
   and t.date_oper = to_date('28/08/2026','dd/mm/yyyy');


/* ── ADDIM 2 — «k» dəsti: son qalıq sətri VARMI? ──────────────────────────
   Şapkadakı şərtin EYNİSİ (ish_gun_cari1 ilə).
   Boş qayıtsa → ADDIM 0-dakı tarixdə hələ qalıq yazılmayıb.                 */
select t.licsch,
       to_char(t.date_oper,'dd/mm/yyyy') date_oper,
       t.saldo_ish_nacval,
       t.saldo_ish_inval
  from odb.arh_saldo_ls t
 where t.licsch = '41010000000008700000'
   and t.date_oper = odb.ish_gun_cari1(to_date('31/08/2026','dd/mm/yyyy'));


/* ── ADDIM 3 — bu hesabda HANSI günlərdə qalıq sətri var? ─────────────────
   Ən son 15 gün. ADDIM 1/2 boş çıxsa, buradan görünəcək ki, sətirlər
   hansı tarixlərə qədər yüklənib (məs. ən son 29.08 və ya 30.08).          */
select to_char(t.date_oper,'dd/mm/yyyy') date_oper,
       t.saldo_vhd_nacval,
       t.saldo_ish_nacval
  from odb.arh_saldo_ls t
 where t.licsch = '41010000000008700000'
   and t.date_oper >= to_date('01/08/2026','dd/mm/yyyy')
 order by t.date_oper desc
 fetch first 15 rows only;
/* Oracle 11g-dirsə «fetch first» işləmir — onda bu variantı işlədin:
select * from (
  select to_char(t.date_oper,'dd/mm/yyyy') date_oper, t.saldo_vhd_nacval, t.saldo_ish_nacval
    from odb.arh_saldo_ls t
   where t.licsch = '41010000000008700000'
     and t.date_oper >= to_date('01/08/2026','dd/mm/yyyy')
   order by t.date_oper desc)
 where rownum <= 15;
*/


/* ── ADDIM 4 — «ac» dəsti: odb.accounts-da bu hesab VARMI? ────────────────
   Boş qayıtsa → ad tapılmır VƏ (daxili join olduğu üçün) qalıqlar da itir.  */
select ac.licsch, ac.name_latin
  from odb.accounts ac
 where ac.licsch = '41010000000008700000';


/* ── ADDIM 5 — accounts cədvəlində sütun adları düzdürmü? ─────────────────
   «licsch» / «name_latin» adları fərqlidirsə ADDIM 4 səhvsiz, amma boş
   qayıdar (yaxud ORA-00904 verər).                                          */
select column_name, data_type
  from all_tab_columns
 where owner = 'ODB' and table_name = 'ACCOUNTS'
 order by column_id;


/* ── ADDIM 6 — modulun işlətdiyi ƏSL sorğu (olduğu kimi) ──────────────────
   Təsdiq üçün: bu, FinNex-in Oracle-a göndərdiyi sorğunun eynisidir.
   0 sətir qaytarmalıdır (ekranda «—» göstərilir).                           */
select ac.name_latin, p.gir_qaliq, k.son_qaliq
  from (select t.licsch,
               case when substr(t.licsch,6,2) = '00' then abs(t.saldo_ish_nacval)
                    else abs(t.saldo_ish_inval) end gir_qaliq
          from odb.arh_saldo_ls t
         where t.licsch = '41010000000008700000'
           and t.date_oper = to_date('28/08/2026','dd/mm/yyyy')) p,
       (select t.licsch,
               case when substr(t.licsch,6,2) = '00' then abs(t.saldo_ish_nacval)
                    else abs(t.saldo_ish_inval) end son_qaliq
          from odb.arh_saldo_ls t
         where t.licsch = '41010000000008700000'
           and t.date_oper = odb.ish_gun_cari1(to_date('31/08/2026','dd/mm/yyyy'))) k,
       odb.accounts ac
 where p.licsch = k.licsch
   and p.licsch = ac.licsch;


/* ═══════════════════════════════════════════════════════════════════════════
   NƏTİCƏNİ NECƏ OXUMAQ

   ADDIM 1 boş, ADDIM 2 dolu   → giriş tarixində qalıq sətri yoxdur
   ADDIM 1 dolu, ADDIM 2 boş   → son tarix hələ yüklənməyib (ən çox ehtimal)
   ADDIM 4 boş                 → hesab odb.accounts-da yoxdur (ad da ona görə boş)
   Hamısı dolu, ADDIM 6 boş    → join şərtində problem var

   ═══════════════════════════════════════════════════════════════════════════
   AYRICA — SORĞUDA İKİ MƏSƏLƏ (nəticədən ASILI OLMAYARAQ):

   1) «GİRİŞ QALIĞI» SƏHV SAHƏDƏN OXUNUR.
      Sorğu giriş qalığı üçün də saldo_ish_* işlədir — o, günün SONUNA
      qalıqdır (docs/sql/muhasibat/Muhasibat_Hesabatlar_Xerite.md).
      Günün ƏVVƏLİNƏ qalıq saldo_vhd_*-dır. Yəni indiki «giriş qalığı»
      əslində 28.08-in AXŞAM qalığıdır — bir günlük sürüşmə.
      Yoxlama (ikisini yan-yana göstərir):

      select to_char(t.date_oper,'dd/mm/yyyy') gun,
             t.saldo_vhd_nacval acilis, t.saldo_ish_nacval baglanis
        from odb.arh_saldo_ls t
       where t.licsch = '41010000000008700000'
         and t.date_oper = to_date('28/08/2026','dd/mm/yyyy');

   2) DAXİLİ JOIN ÜÇÜNÜ BİR-BİRİNƏ BAĞLAYIR.
      Bir tarixdə sətir olmasa hesabın ADI da itir — halbuki ad qalıqdan
      tamamilə asılı deyil. Üç hissə müstəqil skalyar alt-sorğuya ayrılmalıdır
      ki, biri boş olanda o birilər görünsün.

   Hər ikisini düzəldəcəyəm — amma ƏVVƏLCƏ yuxarıdakı 6 addımın nəticəsi
   lazımdır ki, hansı tarix qaydasının düzgün olduğunu (məs. «son qalıq»
   üçün mövcud SON günə düşmək olarmı) sizinlə razılaşdıraq.
   ═══════════════════════════════════════════════════════════════════════════ */

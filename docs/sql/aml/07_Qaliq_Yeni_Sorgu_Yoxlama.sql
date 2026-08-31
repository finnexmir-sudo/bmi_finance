/* ═══════════════════════════════════════════════════════════════════════════
   AML → «Hesab üzrə sorğu» — ŞAPKANIN YENİ SORĞUSU (yoxlama) · 31.08.2026
   ───────────────────────────────────────────────────────────────────────────
   ORACLE — YALNIZ SELECT.

   ƏVVƏLCƏ BUNU İŞLƏDİN. Gözlənilən nəticə:

       NAME_LATIN   GIR_QALIQ   SON_QALIQ
       <hesabın adı>     0,62     1272,13

   Rəqəmlər tutuşmursa modulu DƏYİŞMƏYİN — əvvəlcə danışaq.
   Tutuşursa: 08_Qaliq_Sorgu_Update.sql-i SQL Server-də işlədin.

   ── NƏ DƏYİŞDİ (üç düzəliş) ──────────────────────────────────────────────

   1) SON QALIQ artıq DƏQİQ TARİXƏ bağlı deyil.
      Köhnə: date_oper = ish_gun_cari1(TARIX2) → bu gün (31/08) → sətir YOX.
      Yeni:  date_oper <= TARIX2 olan SONUNCU gün → 28/08 → 1272,13.
      Səbəb: günün sonuna qalıq gün bağlananda yazılır; bugünkü tarixi
      soruşmaq həmişə boş qaytarır. Həftəsonu/bayramda da eyni problem idi.

   2) GİRİŞ QALIĞI düzgün gündən oxunur.
      Köhnə: TARIX1-in saldo_ish (günün SONUNA) → 28/08-in AXŞAM qalığı
             (1272,13) «giriş» kimi görünürdü — bir günlük sürüşmə.
      Yeni:  TARIX1-dən ƏVVƏLKİ sonuncu günün saldo_ish → 27/08 → 0,62.
      Bu, bank çıxarışının klassik qaydasıdır: giriş qalığı = əvvəlki
      günün bağlanış qalığı. (28/08-in saldo_vhd-si də elə −0,62-dir —
      ikisi eynidir, amma bu forma həftəsonunda da düzgün işləyir.)

   3) ÜÇ HİSSƏ BİR-BİRİNDƏN ASILI DEYİL.
      Köhnə: ad + giriş + son DAXİLİ JOIN idi — biri boş olsa üçü də itirdi
             (ona görə «Hesabın adı» da «—» görünürdü).
      Yeni:  «from dual» + üç müstəqil skalyar alt-sorğu → HƏMİŞƏ 1 sətir,
             biri boş olsa yalnız o boş qalır.

   `max(...) keep (dense_rank last order by ...)` sətir olmayanda NULL
   qaytarır (0 sətir yox) — skalyar alt-sorğu üçün məhz bu lazımdır.
   ═══════════════════════════════════════════════════════════════════════════ */

select
  (select max(ac.name_latin)
     from odb.accounts ac
    where ac.licsch = '41010000000008700000')                              name_latin,

  (select max(case when substr(t.licsch,6,2) = '00'
                   then abs(t.saldo_ish_nacval)
                   else abs(t.saldo_ish_inval) end)
            keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '41010000000008700000'
      and t.date_oper < to_date('28/08/2026','dd/mm/yyyy'))                gir_qaliq,

  (select max(case when substr(t.licsch,6,2) = '00'
                   then abs(t.saldo_ish_nacval)
                   else abs(t.saldo_ish_inval) end)
            keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '41010000000008700000'
      and t.date_oper <= to_date('31/08/2026','dd/mm/yyyy'))               son_qaliq
  from dual;


/* ── ƏLAVƏ YOXLAMA — qalıqların hansı günlərdən götürüldüyü ───────────────
   Yuxarıdakı rəqəmlərin HANSI tarixdən gəldiyini göstərir. Mühasibə izah
   lazım olsa bu cədvəl kifayətdir.                                        */
select 'GİRİŞ (TARIX1-dən əvvəlki son gün)' hansi,
       to_char(max(t.date_oper),'dd/mm/yyyy') gun,
       max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper) qaliq
  from odb.arh_saldo_ls t
 where t.licsch = '41010000000008700000'
   and t.date_oper < to_date('28/08/2026','dd/mm/yyyy')
union all
select 'SON (TARIX2-yə qədər son gün)',
       to_char(max(t.date_oper),'dd/mm/yyyy'),
       max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper)
  from odb.arh_saldo_ls t
 where t.licsch = '41010000000008700000'
   and t.date_oper <= to_date('31/08/2026','dd/mm/yyyy');


/* ── SƏRHƏD HALI — hesab ümumiyyətlə yoxdursa ─────────────────────────────
   Yeni forma 1 sətir qaytarmalıdır, üç sütun da NULL. Köhnəsi 0 sətir
   qaytarırdı və modul «hesab tapılmadı» ilə «əməliyyat yoxdur»-u ayırd edə
   bilirdi — servisdə həmin şərt yeni formaya uyğunlaşdırıldı.             */
select
  (select max(ac.name_latin) from odb.accounts ac
    where ac.licsch = '99999999999999999999')                              name_latin,
  (select max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '99999999999999999999'
      and t.date_oper < to_date('28/08/2026','dd/mm/yyyy'))                gir_qaliq,
  (select max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '99999999999999999999'
      and t.date_oper <= to_date('31/08/2026','dd/mm/yyyy'))               son_qaliq
  from dual;

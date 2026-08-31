/* ═══════════════════════════════════════════════════════════════════════════
   AML → «Hesab üzrə sorğu» — ŞAPKANIN YENİ SORĞUSU (yoxlama) · 31.08.2026
   ───────────────────────────────────────────────────────────────────────────
   ORACLE — YALNIZ SELECT.

   ── TƏRİF (istifadəçinin qərarı, 31.08.2026) ─────────────────────────────
       GİRİŞ QALIĞI = BAŞLAMA tarixindəki son (günün sonuna) qalıq
       SON QALIĞI   = BİTMƏ  tarixindəki son (günün sonuna) qalıq

   Yəni hər ikisi `saldo_ish_*` sahəsindən oxunur — biri TARIX1-də, biri
   TARIX2-də. Sahə dəyişmir, yalnız tarix dəyişir.

   ⚠️ MƏNİM İLK VARİANTIM SƏHV İDİ: giriş qalığını «TARIX1-dən ƏVVƏLKİ günün
   bağlanışı» kimi (0,62) götürmüşdüm — bu, klassik bank çıxarışı qaydasıdır,
   amma BURADA belə deyil. Düzəldildi.

   ── NƏ DƏYİŞDİ (köhnə sorğuya nisbətən) ──────────────────────────────────

   1) DƏQİQ TARİX ƏVƏZİNƏ «HƏMİN TARİXƏ QƏDƏR SONUNCU GÜN».
      Köhnə: date_oper = ish_gun_cari1(TARIX2) → 31/08 → sətir YOX → şapka boş.
      Yeni:  date_oper <= TARIX2 olan sonuncu gün → 28/08.
      Səbəb: günün sonuna qalıq gün bağlananda yazılır — bugünkü tarix həmişə
      boşdur; həftəsonu/bayramda da sətir olmur. Tarix əməliyyat günüdürsə
      nəticə dəyişmir (öz sətrini tapır), deyilsə axırıncı bağlanmış günü verir.
      Eyni qayda TARIX1-ə də tətbiq olunur.

   2) ÜÇ HİSSƏ BİR-BİRİNDƏN ASILI DEYİL.
      Köhnə: ad + giriş + son DAXİLİ JOIN idi — biri boş olsa üçü də itirdi
             (ona görə «Hesabın adı» da «—» görünürdü).
      Yeni:  «from dual» + üç müstəqil skalyar alt-sorğu → HƏMİŞƏ 1 sətir.

   `max(...) keep (dense_rank last order by ...)` sətir olmayanda NULL
   qaytarır (0 sətir yox) — skalyar alt-sorğu üçün məhz bu lazımdır.

   ── GÖZLƏNİLƏN NƏTİCƏ (28.08 – 31.08.2026) ───────────────────────────────
       NAME_LATIN                      GIR_QALIQ    SON_QALIQ
       HUSEYNOV SAMIR MIRHUSEYN OGLU    1272,13      1272,13

   İKİSİ EYNİ ÇIXIR — SƏHV DEYİL: giriş = 28/08-in son qalığı, 31/08-ə qədər
   isə sonrakı bağlanmış gün yoxdur (29/30 həftəsonu, 31 hələ bağlanmayıb),
   ona görə son qalıq da elə 28/08-dəndir. Aşağıdakı 2-ci sorğu bunu
   tarixləri ilə göstərir; 3-cü sorğu isə ikisinin fərqləndiyi dövrü verir.
   ═══════════════════════════════════════════════════════════════════════════ */

/* ── 1) Modulun işlədəcəyi ƏSL sorğu ──────────────────────────────────────── */
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
      and t.date_oper <= to_date('28/08/2026','dd/mm/yyyy'))               gir_qaliq,

  (select max(case when substr(t.licsch,6,2) = '00'
                   then abs(t.saldo_ish_nacval)
                   else abs(t.saldo_ish_inval) end)
            keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '41010000000008700000'
      and t.date_oper <= to_date('31/08/2026','dd/mm/yyyy'))               son_qaliq
  from dual;


/* ── 2) Rəqəmlər HANSI gündən gəlir ───────────────────────────────────────
   Mühasibə izah lazım olsa bu cədvəl kifayətdir.
   Gözlənilən: hər ikisi 28/08/2026 · 1272,13                              */
select 'GİRİŞ (28.08-ə qədər son bağlanmış gün)' hansi,
       to_char(max(t.date_oper),'dd/mm/yyyy') gun,
       max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper) qaliq
  from odb.arh_saldo_ls t
 where t.licsch = '41010000000008700000'
   and t.date_oper <= to_date('28/08/2026','dd/mm/yyyy')
union all
select 'SON (31.08-ə qədər son bağlanmış gün)',
       to_char(max(t.date_oper),'dd/mm/yyyy'),
       max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper)
  from odb.arh_saldo_ls t
 where t.licsch = '41010000000008700000'
   and t.date_oper <= to_date('31/08/2026','dd/mm/yyyy');


/* ── 3) İKİSİNİN FƏRQLƏNDİYİ DÖVR — 20.08 – 28.08 ────────────────────────
   Tərifin işlədiyini göstərir: giriş 20/08-in qalığı, son 28/08-in qalığı.
   Gözlənilən: GIR_QALIQ 0,62 · SON_QALIQ 1272,13                          */
select
  (select max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '41010000000008700000'
      and t.date_oper <= to_date('20/08/2026','dd/mm/yyyy'))               gir_qaliq,
  (select max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '41010000000008700000'
      and t.date_oper <= to_date('28/08/2026','dd/mm/yyyy'))               son_qaliq
  from dual;


/* ── 4) SƏRHƏD HALI — hesab ümumiyyətlə yoxdursa ─────────────────────────
   1 sətir qaytarmalıdır, üç sütun da NULL (əvvəl 0 sətir idi).
   Servisdə «hesab tapılmadı» şərti buna görə sətir sayına yox, məzmuna
   baxır — bu, kod dəyişikliyidir, build tələb edir.                       */
select
  (select max(ac.name_latin) from odb.accounts ac
    where ac.licsch = '99999999999999999999')                              name_latin,
  (select max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '99999999999999999999'
      and t.date_oper <= to_date('28/08/2026','dd/mm/yyyy'))               gir_qaliq,
  (select max(abs(t.saldo_ish_nacval)) keep (dense_rank last order by t.date_oper)
     from odb.arh_saldo_ls t
    where t.licsch = '99999999999999999999'
      and t.date_oper <= to_date('31/08/2026','dd/mm/yyyy'))               son_qaliq
  from dual;

/**
 * FinNex — Toplu Maaş Hesablaması
 * maas-toplu.js — Compact table with expandable detail rows
 */
(function () {
    'use strict';

    // ── Pilləli vergi dərəcələri (serverdən JSON) ──────────────
    // Enum: 1=GelirVergisi, 2=DsmfIsci, 3=IssizlikIsci,
    // 4=ItssIsci, 7=DsmfIsegoturen, 8=IssizlikIsegoturen, 9=ItssIsegoturen
    let PILLELER = {};
    let VERGI_GUZESTI = 200;
    let FIRST_BRACKET_MAX = 2500;
    let HYS_ISV_FAIZ = 15;
    try {
        const rawEl = document.getElementById('mthVergiConfig');
        if (rawEl) {
            const cfg = JSON.parse(rawEl.textContent);
            PILLELER = cfg.pilleler || {};
            VERGI_GUZESTI = parseFloat(cfg.vergiGuzesti) || 200;
            HYS_ISV_FAIZ = parseFloat(cfg.hysIsvFaiz) || 15;
        }
        const toolbarEl = document.querySelector('.mth-toolbar[data-first-bracket-max]');
        if (toolbarEl) {
            FIRST_BRACKET_MAX = parseFloat(toolbarEl.dataset.firstBracketMax) || 2500;
        }
    } catch (e) { console.error('Vergi konfiqurasiyası yüklənmədi:', e); }

    // Flat fallback — əgər pillə yoxdursa
    const FLAT = { issizlik: 0.005, issizlikIsv: 0.005 };

    /* ── Pilləli hesablama (server ilə eyni məntiq) ──────────── */
    function pilleliHesabla(mebleg, nov) {
        const pilleler = PILLELER[nov] || [];
        if (mebleg <= 0 || pilleler.length === 0) return null;

        // AsagiHedd üzrə sıralı
        const sorted = [...pilleler].sort((a, b) => a.AsagiHedd - b.AsagiHedd);

        let pille = null;
        for (const p of sorted) {
            if (mebleg >= p.AsagiHedd &&
                (p.YuxariHedd === null || p.YuxariHedd === undefined || mebleg < p.YuxariHedd)) {
                pille = p;
                break;
            }
        }
        // Məbləğ ən yuxarı pillədən də böyükdürsə → sonuncu
        if (!pille) pille = sorted[sorted.length - 1];
        if (!pille) return null;

        return Math.round(
            (pille.SabitMebleg + (mebleg - pille.AsagiHedd) * (pille.Faiz / 100)) * 100
        ) / 100;
    }

    function hesablaTutulma(mebleg, nov, flatFaiz) {
        if (mebleg <= 0) return 0;
        const pilleli = pilleliHesabla(mebleg, nov);
        if (pilleli !== null) return pilleli;
        // Fallback: flat faiz
        return Math.round(mebleg * flatFaiz * 100) / 100;
    }

    const chkAll = document.getElementById('mthChkAll');
    if (chkAll) chkAll.style.pointerEvents = 'auto';
    const selChip = document.getElementById('mthSelChip');
    const btnCalc = document.getElementById('mthBtnCalc');
    const overlay = document.getElementById('mthOverlay');
    const btnOk = document.getElementById('mthBtnOk');
    const btnNo = document.getElementById('mthBtnNo');
    const mainForm = document.getElementById('mthForm');

    const rows = Array.from(document.querySelectorAll('tr.mth-row[data-isci]'));

    /* ── Row data ─────────────────────────────────────────────── */
    function rd(row) {
        const esas = parseFloat(row.dataset.esas || 0);
        const isciGuzest = parseFloat(row.dataset.isciGuzest || 0) || 0;
        const isciGuzestAd = row.dataset.isciGuzestAd || '';
        const hys = parseFloat(row.dataset.hys || 0) || 0;
        const avans = parseFloat(row.dataset.avans || 0) || 0;
        // Məzuniyyət + Xəstəlik preview üçün server-tərəfi yüklənmiş data
        // (mezKesinti və xesKesinti server-də FerdiHesablaAsync ilə eyni düsturla
        // hesablanır → preview GROSS/NET həqiqi hesablanacaq rəqəmlə üst-üstə düşür)
        const mezGun = parseInt(row.dataset.mezGun || 0) || 0;
        const mezOdenis = parseFloat(row.dataset.mezOdenis || 0) || 0;
        const mezKesinti = parseFloat(row.dataset.mezKesinti || 0) || 0;
        const xesSirketGun = parseInt(row.dataset.xesSirketGun || 0) || 0;
        const xesDsmfGun = parseInt(row.dataset.xesDsmfGun || 0) || 0;
        const xesSirketOdenis = parseFloat(row.dataset.xesSirketOdenis || 0) || 0;
        const xesDsmfOdenis = parseFloat(row.dataset.xesDsmfOdenis || 0) || 0;
        const xesKesinti = parseFloat(row.dataset.xesKesinti || 0) || 0;
        const qayibGun = parseInt(row.dataset.qayibGun || 0) || 0;
        const qayibKesinti = parseFloat(row.dataset.qayibKesinti || 0) || 0;
        // Məzuniyyət avansı vergisi (server-tərəfi hesablanmış, ödənilmiş).
        // Faktiki ödənilmiş NET-dən (mavNet) implied cəmi vergi (mavBrut − mavNet)
        // server-in standalone hesabladığı vergi cəmindən fərqli ola bilər
        // (TutulmalariHesablaAsync-in im üçün standartGuzest tətbiqi səbəbiylə).
        // Burada növ üzrə dəyərləri uyğunlaşdırırıq ki, cəm = faktiki vergi olsun.
        const mavBrut   = parseFloat(row.dataset.mavBrut   || 0) || 0;
        let   mavGelirV = parseFloat(row.dataset.mavGelirv || 0) || 0;
        let   mavDsmf   = parseFloat(row.dataset.mavDsmf   || 0) || 0;
        let   mavIss    = parseFloat(row.dataset.mavIss    || 0) || 0;
        let   mavItss   = parseFloat(row.dataset.mavItss   || 0) || 0;
        const mavNet    = parseFloat(row.dataset.mavNet    || 0) || 0;
        const _mavTaxRaw     = mavGelirV + mavDsmf + mavIss + mavItss;
        const _mavTaxImplied = Math.max(0, mavBrut - mavNet);
        if (mavBrut > 0 && _mavTaxRaw > 0 && _mavTaxImplied > 0) {
            const _scale = _mavTaxImplied / _mavTaxRaw;
            mavGelirV = Math.round(mavGelirV * _scale * 100) / 100;
            mavDsmf   = Math.round(mavDsmf   * _scale * 100) / 100;
            mavIss    = Math.round(mavIss    * _scale * 100) / 100;
            mavItss   = Math.round(mavItss   * _scale * 100) / 100;
        }
        const mavTutulma = mavGelirV + mavDsmf + mavIss + mavItss;
        const chk = row.querySelector('.mth-checkbox');
        // .mth-inp--b (bonus) həmişə name atributuna malikdir;
        // overtime input-unda isə heç vaxt name olmur (forma submit edilmir).
        // Hər iki class variantını axtarırıq ki, serverin compile etdiyi
        // view versiyasından asılı olmayaq.
        const allBInputs = Array.from(row.querySelectorAll('.mth-inp--b, .mth-inp--ov'));
        const bInp  = allBInputs.find(inp => inp.name) || null;
        const ovInp = allBInputs.find(inp => !inp.name) || null;
        const cInp  = row.querySelector('.mth-inp--c');
        // Fərqli gəlir input əsas sırada deyil, əlaqəli detail sırasında yerləşir
        const detailRow = document.querySelector(`tr.mth-detail-row[data-detail-for="${row.dataset.isci}"]`);
        // Standart fərqli gəlir (IH-07/VM — tam vergiyə cəlb) və konfiqurasiyalı (data-cfg) ayrılır
        const fgInps = detailRow ? Array.from(detailRow.querySelectorAll('.mth-inp--fg:not([data-cfg])')) : [];
        const cfgInps = detailRow ? Array.from(detailRow.querySelectorAll('.mth-inp--fg[data-cfg]')) : [];
        const done = row.classList.contains('done');

        const bonus = parseFloat(bInp?.value || 0) || 0;
        const overtime = parseFloat(ovInp?.value || 0) || 0;
        const cerime = parseFloat(cInp?.value || 0) || 0;
        const ferqliGelir = fgInps.reduce((s, inp) => s + (parseFloat(inp.value || 0) || 0), 0);

        // Konfiqurasiyalı gəlirlər — hər birinin bayraqlarına görə baza töhfələri (server ilə eyni)
        let cfgCemi = 0, cfgVergi = 0, cfgDsmf = 0, cfgIss = 0, cfgItss = 0, cfgGuz = 0;
        cfgInps.forEach(inp => {
            const m = parseFloat(inp.value || 0) || 0;
            if (!m) return;
            cfgCemi += m;
            const azad = parseFloat(inp.dataset.azad || 0) || 0;
            if (inp.dataset.vergi === '1') cfgVergi += Math.max(0, m - azad);
            if (inp.dataset.dsmf === '1') cfgDsmf += m;
            if (inp.dataset.iss === '1') cfgIss += m;
            if (inp.dataset.itss === '1') cfgItss += m;
            if (inp.dataset.guz === '1') cfgGuz += m;
        });

        // İşəgötürən HYS payı (əvvəlcə hesablanır — brüt-ə daxildir)
        const hysIsv  = Math.round(hys * (HYS_ISV_FAIZ / 100) * 100) / 100;

        // GROSS = əsas maaş − məzuniyyət kəsintisi + məzuniyyət ödənişi
        //        + xəstəlik şirkət ödənişi − xəstəlik kəsintisi
        //        + bonus + fərqli gəlir − cərimə
        // (FerdiHesablaAsync ilə eyni düstur. Diqqət: işəgötürən HYS payı GROSS-da
        //  GÖSTƏRİLMİR — şirkət xərcidir, işçinin işlədiyi pul deyil. Yalnız bəzi
        //  vergi bazalarında (İTSS, İşsizlik) süni olaraq nəzərə alınır.)
        const esasBrut = Math.max(
            esas - mezKesinti + mezOdenis
                 - xesKesinti + xesSirketOdenis
                 - qayibKesinti
                 + bonus + overtime + ferqliGelir + cfgCemi - cerime,
            0);
        const brut = esasBrut;  // GROSS = işlədiyi məbləğ (preview ilə eyni)

        // Vergi bazaları — server (FerdiHesablaAsync) ilə eyni:
        //   Xəstəlik vərəqəsi şirkət ödənişi YALNIZ gəlir vergisinə cəlb olunur,
        //   DSMF / İşsizlik / İTSS bazalarından çıxılır.
        //
        //   HYS:
        //     - İşçi HYS payı: vergi+DSMF-dən AZAD, İTSS+İşsizlikdən cəlb olunur.
        //     - İşəgötürən HYS payı: vergi+DSMF-dən AZAD, İTSS+İşsizlikdən cəlb OLUNUR.
        //
        //     vergiBazasi = esasBrut − HYS                 (gəlir vergisi; xəstəlik daxil)
        //     dsmfBazasi  = esasBrut − HYS − Xəstəlik      (DSMF işçi/işəgötürən)
        //     itssBazasi  = esasBrut + HYSişv − Xəstəlik   (İTSS+İşsizlik; işəgötürən HYS DAXİL)
        // Konfiqurasiyalı gəlir esasBrut-a tam daxildir; hər baza üçün yalnız o haqqa cəlb
        // olunan hissə qalır (cfgCemi çıxılır, müvafiq hissə geri əlavə olunur). cfg=0-da əvvəlki kimi.
        const vergiBazasi    = Math.max(0, esasBrut - hys - cfgCemi + cfgVergi);
        const dsmfBazasi     = Math.max(0, esasBrut - hys - xesSirketOdenis - cfgCemi + cfgDsmf);
        const issizlikBazasi = Math.max(0, esasBrut + hysIsv - xesSirketOdenis - cfgCemi + cfgIss);
        const itssBazasi     = Math.max(0, esasBrut + hysIsv - xesSirketOdenis - cfgCemi + cfgItss);

        // Standart güzəşt — maaş + məz.avansı COMBINED brüt ≤ 2500 olmalıdır
        // (FerdiHesablaAsync ilə eyni məntiq: brutMaasGuzestYoxlama = brutMaas + mezuniyyetAvansBrutu)
        const standartGuzest = brut > 0 && (brut + mavBrut - (cfgCemi - cfgGuz)) <= FIRST_BRACKET_MAX ? VERGI_GUZESTI : 0;
        const vergilenecek = Math.max(0, vergiBazasi - standartGuzest - isciGuzest);

        // NET MAAŞ — məzuniyyət avansı varsa, BIRLƏŞDİRİLMIŞ vergi əsasında hesablanır.
        // Fərdi vergilər də eyni əsasda tapılır: combined − mav (server-tərəfli).
        //   salaryNet     = combinedNet − mavNet (faktiki ödənilmiş)
        //   salaryGelirV  = cGelirV − mavGelirV
        //   ... (hər növ üçün eyni düstur)
        // Nəticə: novlər üzrə cəm + HYS + avans = Cəmi tutulma = brut − net  ✓
        let gelirV, dsmf, iss, itss, tutulma, net;
        // Hesablama addımları üçün ara dəyərlər (mav kartında göstərilir)
        let mavCVergiBazasi = 0, mavCombined = 0, mavCTaxes = 0, mavSalaryTaxes = 0;
        if (mavBrut > 0) {
            const cVergiBazasi  = Math.max(0, esasBrut + mavBrut - hys - cfgCemi + cfgVergi);
            const cDsmfBazasi   = Math.max(0, esasBrut + mavBrut - hys - xesSirketOdenis - cfgCemi + cfgDsmf);
            const cIssBazasi    = Math.max(0, esasBrut + mavBrut + hysIsv - xesSirketOdenis - cfgCemi + cfgIss);
            const cItssBazasi   = Math.max(0, esasBrut + mavBrut + hysIsv - xesSirketOdenis - cfgCemi + cfgItss);
            const cVergilenecek = Math.max(0, cVergiBazasi - standartGuzest - isciGuzest);
            const cGelirV = hesablaTutulma(cVergilenecek, 1, 0);
            const cDsmf   = hesablaTutulma(cDsmfBazasi,   2, 0);
            const cIss    = hesablaTutulma(cIssBazasi,    3, FLAT.issizlik);
            const cItss   = hesablaTutulma(cItssBazasi,   4, 0);
            // Maaşa düşən hissə = cəmi − avans payı (server-tərəfli server side avans vergiləri)
            gelirV = Math.max(0, Math.round((cGelirV - mavGelirV) * 100) / 100);
            dsmf   = Math.max(0, Math.round((cDsmf   - mavDsmf)   * 100) / 100);
            iss    = Math.max(0, Math.round((cIss    - mavIss)     * 100) / 100);
            itss   = Math.max(0, Math.round((cItss   - mavItss)    * 100) / 100);
            // Cəmi NET: kombinasiyalı − faktiki ödənilmiş avans NET
            const cTaxes     = cGelirV + cDsmf + cIss + cItss;
            const combinedNet = (esasBrut + mavBrut) - cTaxes - hys;
            net     = Math.max(combinedNet - mavNet - avans, 0);
            // "Cəmi tutulma" = sıralanan 4 vergi + avans (HYS ayrı kartda göstərilir;
            //  preview-da da "Cəmi tutulmalar" yalnız 4 vergidir).
            tutulma = gelirV + dsmf + iss + itss + avans;
            // Hesablama addımları üçün
            mavCVergiBazasi = cVergiBazasi;
            mavCombined     = esasBrut + mavBrut;
            mavCTaxes       = cTaxes;
            mavSalaryTaxes  = Math.round((gelirV + dsmf + iss + itss) * 100) / 100;
        } else {
            gelirV  = hesablaTutulma(vergilenecek, 1, 0);
            dsmf    = hesablaTutulma(dsmfBazasi,   2, 0);
            iss     = hesablaTutulma(issizlikBazasi, 3, FLAT.issizlik);
            itss    = hesablaTutulma(itssBazasi,   4, 0);
            tutulma = gelirV + dsmf + iss + itss + avans;
            // NET = brut (işlədiyi) − 4 vergi − HYS işçi − avans.
            // hysIsv brut-da deyil, ona görə burada da çıxılmır (şirkət xərcidir).
            net = Math.max(brut - tutulma - hys, 0);
        }

        // İşəgötürən xərcləri — məzuniyyət avansı varsa BIRLƏŞMIŞ baza üzərindən
        // (FerdiHesablaAsync ilə eyni: aylıq cəmi gəlir 3088.23 → DSMF işv 429.52)
        let dsmfIsvBaza, itssIsvBaza, issizlikIsvBaza;
        if (mavBrut > 0) {
            dsmfIsvBaza     = Math.max(0, esasBrut + mavBrut - hys - xesSirketOdenis - cfgCemi + cfgDsmf);
            issizlikIsvBaza = Math.max(0, esasBrut + mavBrut + hysIsv - xesSirketOdenis - cfgCemi + cfgIss);
            itssIsvBaza     = Math.max(0, esasBrut + mavBrut + hysIsv - xesSirketOdenis - cfgCemi + cfgItss);
        } else {
            dsmfIsvBaza     = dsmfBazasi;
            issizlikIsvBaza = issizlikBazasi;
            itssIsvBaza     = itssBazasi;
        }
        const dsmfIsv = hesablaTutulma(dsmfIsvBaza, 7, 0);
        const itssIsv = hesablaTutulma(itssIsvBaza, 9, 0);
        const issIsv  = hesablaTutulma(issizlikIsvBaza, 8, FLAT.issizlikIsv);
        const sirketCemi = dsmfIsv + issIsv + itssIsv + hysIsv;

        return {
            esas, bonus, overtime, cerime, ferqliGelir, brut, vergilenecek,
            vergiBazasi, dsmfBazasi, itssBazasi,
            standartGuzest, isciGuzest, isciGuzestAd,
            hys, hysIsv, avans,
            mezGun, mezOdenis, mezKesinti,
            xesSirketGun, xesDsmfGun, xesSirketOdenis, xesDsmfOdenis, xesKesinti,
            qayibGun, qayibKesinti,
            mavBrut, mavGelirV, mavDsmf, mavIss, mavItss, mavNet, mavTutulma,
            mavGunluk: mezGun > 0 && mavBrut > 0 ? Math.round(mavBrut / mezGun * 100) / 100 : 0,
            mavCombined, mavCVergiBazasi, mavCTaxes, mavSalaryTaxes,
            mavTaxImplied: _mavTaxImplied,
            gelirV, dsmf, iss, itss, tutulma, net,
            dsmfIsv, issIsv, itssIsv, sirketCemi,
            checked: !!chk?.checked && !done, done
        };
    }

    /* ── Format ───────────────────────────────────────────────── */
    const fmt = v => v > 0 ? v.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₼' : '—';

    /* ── Get the detail row for a main row ────────────────────── */
    function getDetailRow(row) {
        const isciId = row.dataset.isci;
        return document.querySelector('tr.mth-detail-row[data-detail-for="' + isciId + '"]');
    }

    /* ── Update preview cells for one row (main + detail) ────── */
    function updateRow(row) {
        const d = rd(row);
        const detailRow = getDetailRow(row);

        // Helper: set text in main row or detail row
        const set = (sel, val, cls) => {
            // Search in main row
            const el = row.querySelector(sel);
            if (el) {
                el.textContent = val;
                if (cls) el.className = cls;
            }
            // Also search in detail row
            if (detailRow) {
                const el2 = detailRow.querySelector(sel);
                if (el2) {
                    el2.textContent = val;
                    if (cls) el2.className = cls;
                }
            }
        };

        // Main row cells
        set('[data-p="brut"]', fmt(d.brut), d.brut > 0 ? 'n' : 'n n--d');
        set('[data-p="net"]', fmt(d.net), d.net > 0 ? 'n n--au' : 'n n--d');

        // Vergi güzəşti breakdown (standart + işçi + vergilənəcək)
        // Məzuniyyət — gün sayı / ödəniş / kəsinti (server ilə eyni düstur)
        set('[data-p="mezgun"]', d.mezGun > 0 ? d.mezGun + ' gün' : '—', d.mezGun > 0 ? 'n n--b' : 'n n--d');
        set('[data-p="mez"]', fmt(d.mezOdenis), d.mezOdenis > 0 ? 'n n--b' : 'n n--d');
        set('[data-p="mezkes"]', fmt(d.mezKesinti), d.mezKesinti > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="qayibgun"]', d.qayibGun > 0 ? d.qayibGun + ' gün' : '—', d.qayibGun > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="qayibkes"]', fmt(d.qayibKesinti), d.qayibKesinti > 0 ? 'n n--r' : 'n n--d');

        // Xəstəlik
        const xesSirketText = d.xesSirketGun > 0
            ? d.xesSirketGun + ' gün / ' + fmt(d.xesSirketOdenis)
            : '—';
        set('[data-p="xessirket"]', xesSirketText, d.xesSirketGun > 0 ? 'n n--b' : 'n n--d');
        const xesDsmfText = d.xesDsmfGun > 0
            ? d.xesDsmfGun + ' gün / ' + fmt(d.xesDsmfOdenis)
            : '—';
        set('[data-p="xesdsmf"]', xesDsmfText, d.xesDsmfGun > 0 ? 'n' : 'n n--d');

        set('[data-p="standartguzest"]', fmt(d.standartGuzest), d.standartGuzest > 0 ? 'n n--g' : 'n n--d');
        set('[data-p="isciguzest"]', fmt(d.isciGuzest), d.isciGuzest > 0 ? 'n n--g' : 'n n--d');
        set('[data-p="vergilenecek"]', fmt(d.vergilenecek), d.vergilenecek > 0 ? 'n n--au' : 'n n--d');

        // Detail row cells
        set('[data-p="gelirv"]', fmt(d.gelirV), d.gelirV > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="dsmf"]', fmt(d.dsmf), d.dsmf > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="issizlik"]', fmt(d.iss), d.iss > 0 ? 'n' : 'n n--d');
        set('[data-p="itss"]', fmt(d.itss), d.itss > 0 ? 'n' : 'n n--d');
        set('[data-p="tutulma"]', fmt(d.tutulma), d.tutulma > 0 ? 'n n--r' : 'n n--d');

        // Avans (kredit)
        set('[data-p="avans"]', fmt(d.avans), d.avans > 0 ? 'n n--r' : 'n n--d');

        // Məzuniyyət avansı vergisi informasiya sətri (İşçi tutulmaları kartında)
        set('[data-p="mav-tutulma-info"]', fmt(d.mavTutulma), d.mavTutulma > 0 ? 'n' : 'n n--d');

        // Məzuniyyət avansı vergisi (server-tərəfi, ödənilmiş)
        set('[data-p="mav-brut"]',   fmt(d.mavBrut),   d.mavBrut   > 0 ? 'n n--b' : 'n n--d');
        set('[data-p="mav-gelirv"]', fmt(d.mavGelirV), d.mavGelirV > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="mav-dsmf"]',   fmt(d.mavDsmf),   d.mavDsmf   > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="mav-iss"]',    fmt(d.mavIss),    d.mavIss    > 0 ? 'n'       : 'n n--d');
        set('[data-p="mav-itss"]',   fmt(d.mavItss),   d.mavItss   > 0 ? 'n'       : 'n n--d');
        set('[data-p="mav-net"]',    fmt(d.mavNet),    d.mavNet    > 0 ? 'n n--au' : 'n n--d');

        // Məzuniyyət hesablama addımları (yalnız mavBrut > 0 olan işçilər üçün)
        if (d.mavBrut > 0) {
            const n2 = v => v.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            set('[data-p="mav-h-gunluk"]',   d.mavGunluk > 0 ? fmt(d.mavGunluk) + '/gün' : '—', 'n n--b');
            set('[data-p="mav-h-im"]',       fmt(d.brut),  d.brut > 0 ? 'n' : 'n n--d');
            set('[data-p="mav-h-combined"]', fmt(d.mavCombined),     d.mavCombined > 0 ? 'n n--b' : 'n n--d');
            set('[data-p="mav-h-vergibaz"]', fmt(d.mavCVergiBazasi), d.mavCVergiBazasi > 0 ? 'n n--au' : 'n n--d');
            set('[data-p="mav-h-sguzest"]',  fmt(d.standartGuzest),  d.standartGuzest > 0 ? 'n n--g' : 'n n--d');
            set('[data-p="mav-h-ctaxes"]',   fmt(d.mavCTaxes),       d.mavCTaxes > 0 ? 'n n--r' : 'n n--d');
            set('[data-p="mav-h-salarytax"]', fmt(d.mavSalaryTaxes), d.mavSalaryTaxes > 0 ? 'n n--r' : 'n n--d');
            set('[data-p="mav-h-taxshare"]', fmt(d.mavTaxImplied),  d.mavTaxImplied > 0 ? 'n n--r' : 'n n--d');
            set('[data-p="mav-h-net"]',      fmt(d.mavNet),         d.mavNet > 0 ? 'n n--au' : 'n n--d');
            // Formula açıqlamaları (hər addımda hesablamanın necə alındığını göstərir)
            set('[data-p="mav-hf-1"]', d.mezGun > 0 && d.mavGunluk > 0
                ? n2(d.mavBrut) + ' ÷ ' + d.mezGun + ' gün' : '');
            set('[data-p="mav-hf-2"]', n2(d.esas) + ' / AyIsGün × işlənmiş gün');
            set('[data-p="mav-hf-3"]', n2(d.brut) + ' + ' + n2(d.mavBrut));
            set('[data-p="mav-hf-4"]', d.hys > 0
                ? n2(d.mavCombined) + ' − ' + n2(d.hys) + ' (HYS)'
                : n2(d.mavCombined) + ' (HYS yoxdur)');
            set('[data-p="mav-hf-5"]', d.standartGuzest > 0
                ? 'gəlir ' + n2(d.mavCombined) + ' ≤ ' + n2(FIRST_BRACKET_MAX)
                : 'gəlir ' + n2(d.mavCombined) + ' > ' + n2(FIRST_BRACKET_MAX));
            set('[data-p="mav-hf-7"]', n2(d.gelirV) + ' + ' + n2(d.dsmf) + ' + ' + n2(d.iss) + ' + ' + n2(d.itss));
            set('[data-p="mav-hf-8"]', n2(d.mavCTaxes) + ' − ' + n2(d.mavSalaryTaxes));
            set('[data-p="mav-hf-r"]', n2(d.mavBrut) + ' − ' + n2(d.mavTaxImplied));
        }

        // HYS detail cells
        set('[data-p="hysisci"]', fmt(d.hys), d.hys > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="hysisv"]', fmt(d.hysIsv), d.hysIsv > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="hysisv2"]', fmt(d.hysIsv), d.hysIsv > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="vergibazasi"]', fmt(d.vergiBazasi), d.vergiBazasi > 0 ? 'n n--au' : 'n n--d');

        // Employer costs in detail (update dynamically based on brut)
        set('[data-p="dsmfisv"]', fmt(d.dsmfIsv), d.dsmfIsv > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="itssisv"]', fmt(d.itssIsv), d.itssIsv > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="issizlikisv"]', fmt(d.issIsv), d.issIsv > 0 ? 'n' : 'n n--d');
        set('[data-p="sirketcemi"]', fmt(d.sirketCemi), d.sirketCemi > 0 ? 'n n--p' : 'n n--d');
    }

    /* ── Footer totals ────────────────────────────────────────── */
    function updateFooter() {
        const sel = rows.filter(r => rd(r).checked);
        const t = sel.reduce((a, r) => {
            const d = rd(r);
            // İşçi tərəfi — vergi sütunları maaş + məz.avansi hissəsini birlikdə toplayır
            a.brut        += d.brut;
            a.gelirV      += d.gelirV + d.mavGelirV;
            a.dsmfIsci    += d.dsmf  + d.mavDsmf;
            a.issIsci     += d.iss   + d.mavIss;
            a.itssIsci    += d.itss  + d.mavItss;
            a.hysIsci     += d.hys;
            a.avans       += d.avans;
            a.mavTutulma  += d.mavTutulma;
            a.tutulma     += d.tutulma;
            a.net         += d.net;
            // Şirkət tərəfi
            a.dsmfIsv  += d.dsmfIsv;
            a.issIsv   += d.issIsv;
            a.itssIsv  += d.itssIsv;
            a.hysIsv   += d.hysIsv;
            a.sirketEx += d.sirketCemi;
            a.sirket   += d.brut + d.sirketCemi;
            return a;
        }, {
            brut: 0, gelirV: 0, dsmfIsci: 0, issIsci: 0, itssIsci: 0, hysIsci: 0, avans: 0, mavTutulma: 0, tutulma: 0, net: 0,
            dsmfIsv: 0, issIsv: 0, itssIsv: 0, hysIsv: 0, sirketEx: 0, sirket: 0
        });

        const s = (id, v) => { const el = document.getElementById(id); if (el) el.textContent = v; };
        s('mthFootSayi', sel.length);
        // İşçi tərəfi
        s('mthFootBrut', fmt(t.brut));
        s('mthFootGelirV', fmt(t.gelirV));
        s('mthFootDsmfIsci', fmt(t.dsmfIsci));
        s('mthFootIssizlikIsci', fmt(t.issIsci));
        s('mthFootItssIsci', fmt(t.itssIsci));
        s('mthFootHysIsci', fmt(t.hysIsci));
        s('mthFootAvans', fmt(t.avans));
        s('mthFootMavTutulma', fmt(t.mavTutulma));
        s('mthFootTutulma', fmt(t.tutulma + t.hysIsci));
        s('mthFootNet', fmt(t.net));
        s('mthFootNet2', fmt(t.net));
        // Şirkət tərəfi
        s('mthFootDsmfIsv', fmt(t.dsmfIsv));
        s('mthFootIssizlikIsv', fmt(t.issIsv));
        s('mthFootItssIsv', fmt(t.itssIsv));
        s('mthFootHysIsv', fmt(t.hysIsv));
        s('mthFootSirketEx', fmt(t.sirketEx));
        s('mthFootSirket', fmt(t.sirket));

        if (selChip) {
            selChip.textContent = sel.length + ' işçi seçilib';
            selChip.classList.toggle('on', sel.length > 0);
        }
        if (btnCalc) btnCalc.disabled = sel.length === 0;

        // chkAll state
        const elig = rows.filter(r => !r.classList.contains('done'));
        const allOn = elig.length > 0 && elig.every(r => r.querySelector('.mth-checkbox')?.checked);
        const anyOn = elig.some(r => r.querySelector('.mth-checkbox')?.checked);
        if (chkAll) {
            chkAll.checked = allOn;
            chkAll.indeterminate = !allOn && anyOn;
        }
    }

    /* ── Select all ───────────────────────────────────────────── */
    chkAll?.addEventListener('change', function () {
        rows.forEach(r => {
            if (r.classList.contains('done')) return;
            const c = r.querySelector('.mth-checkbox');
            if (c) c.checked = this.checked;
            r.classList.toggle('selected', this.checked);
        });
        updateFooter();
    });

    /* ── Row checkboxes + inputs ──────────────────────────────── */
    rows.forEach(row => {
        const chk = row.querySelector('.mth-checkbox');
        chk?.addEventListener('change', function () {
            row.classList.toggle('selected', this.checked);
            updateFooter();
        });

        const detailRowForInput = getDetailRow(row);
        const fgInpsForEvent = detailRowForInput
            ? Array.from(detailRowForInput.querySelectorAll('.mth-inp--fg'))
            : [];
        const allBInpsForEvent = Array.from(row.querySelectorAll('.mth-inp--b, .mth-inp--ov'));
        [...allBInpsForEvent,
         row.querySelector('.mth-inp--c'),
         ...fgInpsForEvent].forEach(inp => {
            inp?.addEventListener('input', () => { updateRow(row); updateFooter(); });
        });

        // Expand/collapse toggle
        const expandBtn = row.querySelector('.mth-expand-btn');
        expandBtn?.addEventListener('click', () => {
            const detailRow = getDetailRow(row);
            if (!detailRow) return;
            const isOpen = detailRow.classList.contains('open');
            if (isOpen) {
                detailRow.classList.remove('open');
                expandBtn.classList.remove('open');
                row.classList.remove('expanded');
            } else {
                detailRow.classList.add('open');
                expandBtn.classList.add('open');
                row.classList.add('expanded');
            }
        });

        updateRow(row);
    });

    updateFooter();

    /* ── Excel export — mühasibin yoxlaması üçün ──────────────────
       Bütün işçilərin real-time hesablanmış məlumatlarını HTML cədvəl
       şəklində Blob kimi download edir (.xls). Excel HTML import-u
       formatlama ilə birlikdə dəstəkləyir, əlavə kitabxana lazım deyil. */
    const btnExcel = document.getElementById('mthBtnExcel');
    btnExcel?.addEventListener('click', () => {
        const dovr = document.querySelector('.mth-period-bar')?.querySelector('select')?.parentElement?.textContent?.trim() || '';
        const ay = (document.querySelector('[data-period-ay]')?.dataset?.periodAy) || '';
        const il = (document.querySelector('[data-period-il]')?.dataset?.periodIl) || '';

        // Mühasib cədvəli ilə eyni sütunlar (server ExcelIxrac ilə eyni struktur)
        const headers = [
            '№', 'S.A.A.', 'Vəzifəsi',
            'Müqavilə üzrə aylıq əmək haqqı',
            'Hesablanmış aylıq əmək haqqı',
            '18.02.2016-cı il tarixli IH-07 saylı əmrlə əlavə təminat',
            'İstifadə edilməmiş əmək məzuniyyəti günlərinə görə kompessasiya ödənişi',
            'Orta əmək haqqı saxlanılan günlər üçün hesablanmış orta əmək haqqı',
            'Məzuniyyət haqqı', 'Mükafat', 'Bayram hədiyyəsi',
            'Müsabiqə qalibinə verilən hədiyyə', 'Xəstəlik vərəqəsi',
            'Yalnız mdss haqqı hesablanan digər gəlirlər',
            'VM-nin 98.2.1-ci maddəsinə əsasən vergiyə cəlb olunan gəlirlər',
            'VM-nin 98.2.3-cü maddəsinə əsasən vergiyə cəlb olunan gəlirlər',
            'Əlavə əmək haqqı',
            'HYS müqavilələri üzrə 3 il tamam olmamış qayıdan məbləğlər',
            'Cəmi hesablanmış aylıq ödənişlər',
            'Ödənilmiş həyatın yığım sığortası haqqları',
            'İşgötürən tərəfindən ödənilən həyatın yığım sığortası haqqları',
            'Tutulmuş m.d.s.s. haqları (10%)',
            'Tutulmuş işsizlikdən sığorta haqqları (0.5%)',
            'Gəlir vergisi', 'Icbari Tibbi Sığorta (2 %)', 'Avans',
            'Güvənli Sığorta üzrə çıxılmalar', 'Tutulmuşdur', 'Ödənilməlidir',
            'İşəgötürən tərəfindən ödənilən MDSS haqqı',
            'İşəgötürən tərəfindən ödənilən İTS haqqı',
            'Cari hesablar'
        ];
        const COLS = headers.length; // 32

        const num = v => (v && v > 0) ? Number(v).toFixed(2).replace('.', ',') : '0,00';
        const esc = s => String(s ?? '').replace(/[&<>"']/g, c =>
            ({ '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;' })[c]);

        const tot = new Array(COLS).fill(0);
        const visibleRows = rows.filter(r => !r.classList.contains('mth-hidden'));

        let bodyRows = visibleRows.map((row, i) => {
            const d = rd(row);
            const ad   = row.querySelector('.mth-name-primary')?.textContent?.trim() || '';
            const vez  = row.dataset.vezife || '—';
            const iban = row.dataset.iban || '';

            // IH-07 / VM 98.2.1 / konfiqurasiyalı gəlirlər — detal sətrindən ayrıca oxunur
            const dr = getDetailRow(row);
            const ih07 = dr ? (parseFloat(dr.querySelector('input[name$=".IH07Meblegi"]')?.value || 0) || 0) : 0;
            const vm98 = dr ? (parseFloat(dr.querySelector('input[name$=".VM9821Meblegi"]')?.value || 0) || 0) : 0;
            const cfg  = dr ? Array.from(dr.querySelectorAll('.mth-inp--fg[data-cfg]'))
                                  .reduce((s, inp) => s + (parseFloat(inp.value || 0) || 0), 0) : 0;

            const gross = d.brut + d.mavBrut;   // cəmi hesablanmış (məzuniyyət avansı brütü daxil)
            const v = new Array(COLS).fill(0);
            v[3]  = d.esas;                                                 // Müqavilə üzrə
            v[4]  = d.esas - d.mezKesinti - d.xesKesinti - d.qayibKesinti;  // Hesablanmış (işlənmiş)
            v[5]  = ih07;                                                   // IH-07
            v[8]  = d.mezOdenis + d.mavBrut;                               // Məzuniyyət haqqı (+ avans brütü)
            v[9]  = d.bonus;                                                // Mükafat
            v[12] = d.xesSirketOdenis;                                      // Xəstəlik vərəqəsi
            v[13] = cfg;                                                    // Yalnız mdss / digər gəlirlər
            v[14] = vm98;                                                   // VM 98.2.1
            v[16] = d.overtime;                                             // Əlavə əmək haqqı
            v[18] = gross;                                                  // Cəmi hesablanmış
            v[19] = d.hys;                                                  // Ödənilmiş HYS (işçi)
            v[20] = d.hysIsv;                                               // İşgötürən HYS
            v[21] = d.dsmf   + d.mavDsmf;                                   // MDSS (işçi)
            v[22] = d.iss    + d.mavIss;                                    // İşsizlik (işçi)
            v[23] = d.gelirV + d.mavGelirV;                                 // Gəlir vergisi
            v[24] = d.itss   + d.mavItss;                                   // İTSS (işçi)
            v[25] = d.avans  + d.mavNet;                                    // Avans (+ məz. avansı net)
            v[27] = gross - d.net;                                          // Tutulmuşdur = brüt − net
            v[28] = d.net;                                                  // Ödənilməlidir (net)
            v[29] = d.dsmfIsv;                                              // İşəgötürən MDSS
            v[30] = d.itssIsv;                                              // İşəgötürən İTS

            for (let c = 3; c <= 30; c++) tot[c] += v[c];

            const m = c => `<td style="text-align:right">${num(v[c])}</td>`;
            return `<tr>
                <td style="text-align:center">${i + 1}</td>
                <td>${esc(ad)}</td>
                <td>${esc(vez)}</td>
                ${m(3)}${m(4)}${m(5)}${m(6)}${m(7)}${m(8)}${m(9)}${m(10)}${m(11)}${m(12)}${m(13)}${m(14)}${m(15)}${m(16)}${m(17)}
                <td style="text-align:right;font-weight:bold;background:#e8f5ee">${num(v[18])}</td>
                ${m(19)}${m(20)}${m(21)}${m(22)}${m(23)}${m(24)}${m(25)}${m(26)}
                <td style="text-align:right;color:#c83838">${num(v[27])}</td>
                <td style="text-align:right;font-weight:bold;background:#fff7e0">${num(v[28])}</td>
                ${m(29)}${m(30)}
                <td>${esc(iban)}</td>
            </tr>`;
        }).join('');

        const tm = c => `<td style="text-align:right">${num(tot[c])}</td>`;
        const totalRow = `<tr style="background:#f1f5f9;font-weight:bold">
            <td style="text-align:center">—</td>
            <td>CƏMİ — ${visibleRows.length} işçi</td>
            <td></td>
            ${tm(3)}${tm(4)}${tm(5)}${tm(6)}${tm(7)}${tm(8)}${tm(9)}${tm(10)}${tm(11)}${tm(12)}${tm(13)}${tm(14)}${tm(15)}${tm(16)}${tm(17)}
            <td style="text-align:right;background:#e8f5ee">${num(tot[18])}</td>
            ${tm(19)}${tm(20)}${tm(21)}${tm(22)}${tm(23)}${tm(24)}${tm(25)}${tm(26)}
            <td style="text-align:right;color:#c83838">${num(tot[27])}</td>
            <td style="text-align:right;background:#fff7e0">${num(tot[28])}</td>
            ${tm(29)}${tm(30)}
            <td></td>
        </tr>`;

        const headerRow = '<tr style="background:#1a2332;color:#fff;font-weight:bold">' +
            headers.map(h => `<th style="padding:8px;border:1px solid #ccc">${esc(h)}</th>`).join('') + '</tr>';

        const dovrText = document.querySelector('.mth-period-current')?.textContent?.trim() ||
                         document.querySelector('h1')?.textContent?.trim() || 'Toplu Maaş';
        const indi = new Date();
        const tarixStr = indi.toLocaleDateString('az-AZ');

        const html = `<!DOCTYPE html>
<html xmlns:x="urn:schemas-microsoft-com:office:excel">
<head>
<meta charset="utf-8" />
<title>Toplu Maaş Hesablaması</title>
<!--[if gte mso 9]><xml>
<x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet>
<x:Name>Toplu Maas</x:Name>
<x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions>
</x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook>
</xml><![endif]-->
</head>
<body>
<h2 style="font-family:Arial">Toplu Maaş Hesablaması — ${esc(dovrText)}</h2>
<p style="font-family:Arial;color:#6b7280;font-size:12px">İxrac tarixi: ${tarixStr} · ${visibleRows.length} işçi</p>
<table style="border-collapse:collapse;font-family:Arial;font-size:11px">
<thead>${headerRow}</thead>
<tbody>${bodyRows}${totalRow}</tbody>
</table>
</body></html>`;

        // UTF-8 BOM ilə Blob — Excel düzgün açsın
        const bom = '﻿';
        const blob = new Blob([bom + html], { type: 'application/vnd.ms-excel;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        const fileName = `Toplu_Maas_${dovrText.replace(/\s+/g, '_').replace(/[^\w]/g, '')}_${indi.getFullYear()}${String(indi.getMonth()+1).padStart(2,'0')}${String(indi.getDate()).padStart(2,'0')}.xls`;
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        setTimeout(() => { document.body.removeChild(a); URL.revokeObjectURL(url); }, 100);
    });

    /* ── Open confirm modal ───────────────────────────────────── */
    btnCalc?.addEventListener('click', () => {
        const sel = rows.filter(r => rd(r).checked);
        if (!sel.length) return;
        const t = sel.reduce((a, r) => { const d = rd(r); a.brut += d.brut; a.net += d.net; a.bonus += d.bonus; return a; }, { brut: 0, net: 0, bonus: 0 });
        const s = (id, v) => { const el = document.getElementById(id); if (el) el.textContent = v; };
        s('mthMIsci', sel.length + ' işçi');
        s('mthMBrut', fmt(t.brut));
        s('mthMNet', fmt(t.net));
        s('mthMBonus', fmt(t.bonus));
        overlay?.classList.add('open');
    });

    /* ── Close modal ──────────────────────────────────────────── */
    btnNo?.addEventListener('click', () => overlay?.classList.remove('open'));
    overlay?.addEventListener('click', e => { if (e.target === overlay) overlay.classList.remove('open'); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape') overlay?.classList.remove('open'); });

    /* ── Submit ───────────────────────────────────────────────── */
    btnOk?.addEventListener('click', () => {
        overlay?.classList.remove('open');
        // disable unselected inputs so they don't pollute form
        rows.forEach(r => {
            const d = rd(r);
            if (!d.checked) r.querySelectorAll('input[type=number]').forEach(i => i.disabled = true);
        });
        if (btnOk) { btnOk.textContent = 'Göndərilir...'; btnOk.disabled = true; }
        mainForm?.submit();
    });

    /* ── Positive-only inputs ─────────────────────────────────── */
    document.querySelectorAll('.mth-inp').forEach(inp => {
        inp.addEventListener('blur', function () {
            const v = parseFloat(this.value);
            if (isNaN(v) || v < 0) this.value = '';
        });
    });

    /* ── Client-side search (ad + departament) ───────────────── */
    function normalizeAz(s) {
        if (!s) return '';
        return s.toString().toLowerCase()
            .replace(/ə/g, 'e').replace(/ö/g, 'o').replace(/ü/g, 'u')
            .replace(/ı/g, 'i').replace(/ş/g, 's').replace(/ç/g, 'c')
            .replace(/ğ/g, 'g');
    }

    const searchBox = document.getElementById('mthSearchBox');
    const searchClear = document.getElementById('mthSearchClear');
    const searchWrap = searchBox?.closest('.mth-search');
    const isciCountEl = document.getElementById('mthIsciCount');
    const totalCount = rows.length;

    function applySearch() {
        const raw = (searchBox?.value || '').trim();
        const needle = normalizeAz(raw);
        searchWrap?.classList.toggle('has-value', raw.length > 0);
        let visible = 0;
        rows.forEach(row => {
            const name = row.querySelector('.mth-name-primary')?.textContent || '';
            const dept = row.querySelector('.mth-name-dept')?.textContent || '';
            const hay = normalizeAz(name + ' ' + dept);
            const match = needle.length === 0 || hay.indexOf(needle) >= 0;
            row.classList.toggle('mth-hidden', !match);
            const detail = getDetailRow(row);
            if (detail) detail.classList.toggle('mth-hidden', !match);
            if (match) visible++;
        });
        if (isciCountEl) {
            isciCountEl.textContent = (needle.length > 0 && visible !== totalCount)
                ? `${visible} / ${totalCount} işçi`
                : `${totalCount} işçi`;
        }
    }

    if (searchBox) {
        searchBox.addEventListener('input', applySearch);
        searchBox.addEventListener('keydown', e => {
            if (e.key === 'Escape') { searchBox.value = ''; applySearch(); searchBox.blur(); }
        });
    }
    if (searchClear) {
        searchClear.addEventListener('click', () => {
            if (searchBox) { searchBox.value = ''; applySearch(); searchBox.focus(); }
        });
    }

})();

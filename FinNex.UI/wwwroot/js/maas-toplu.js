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
        const chk = row.querySelector('.mth-checkbox');
        const bInp = row.querySelector('.mth-inp--b');
        const cInp = row.querySelector('.mth-inp--c');
        const done = row.classList.contains('done');

        const bonus = parseFloat(bInp?.value || 0) || 0;
        const cerime = parseFloat(cInp?.value || 0) || 0;

        // İşəgötürən HYS payı (əvvəlcə hesablanır — brüt-ə daxildir)
        const hysIsv  = Math.round(hys * (HYS_ISV_FAIZ / 100) * 100) / 100;

        // GROSS = əsas maaş − məzuniyyət kəsintisi + məzuniyyət ödənişi
        //        + xəstəlik şirkət ödənişi − xəstəlik kəsintisi
        //        + bonus − cərimə + işəgötürən HYS payı
        // (FerdiHesablaAsync ilə eyni düstur — preview həqiqi nəticə ilə üst-üstə düşür)
        const esasBrut = Math.max(
            esas - mezKesinti + mezOdenis
                 - xesKesinti + xesSirketOdenis
                 + bonus - cerime,
            0);
        const brut = esasBrut + hysIsv;

        // Vergi+DSMF bazası = əsas brüt − işçi HYS (işəgötürən payı daxil deyil)
        const vergiDsmfBazasi = Math.max(0, esasBrut - hys);
        // İTSS/İşsizlik = əsas brüt (işəgötürən payı daxil deyil, HYS çıxılmır)
        const itssBazasi = esasBrut;

        // Standart güzəşt — GROSS (maaş + işəgötürən HYS) ≤ 2500 olmalıdır
        const standartGuzest = brut > 0 && brut <= FIRST_BRACKET_MAX ? VERGI_GUZESTI : 0;
        const vergilenecek = Math.max(0, vergiDsmfBazasi - standartGuzest - isciGuzest);

        // İşçidən tutulanlar — GəlirV və DSMF: vergiDsmfBazası ilə; İTSS və İşsizlik: itssBazası ilə
        const gelirV  = hesablaTutulma(vergilenecek,    1, 0);
        const dsmf    = hesablaTutulma(vergiDsmfBazasi,  2, 0);
        const iss     = hesablaTutulma(itssBazasi,       3, FLAT.issizlik);
        const itss    = hesablaTutulma(itssBazasi,       4, 0);
        // Tutulma = vergilər + HYS + avans
        const tutulma = gelirV + dsmf + iss + itss + hys + hysIsv + avans;
        const net     = Math.max(brut - tutulma, 0);

        // İşəgötürən xərcləri — DSMF: vergiDsmfBazası; İTSS/İşsizlik: əsas brüt
        const dsmfIsv = hesablaTutulma(vergiDsmfBazasi, 7, 0);
        const itssIsv = hesablaTutulma(itssBazasi,      9, 0);
        const issIsv  = hesablaTutulma(itssBazasi,      8, FLAT.issizlikIsv);
        const sirketCemi = dsmfIsv + issIsv + itssIsv + hysIsv;

        return {
            esas, bonus, cerime, brut, vergilenecek,
            vergiDsmfBazasi, itssBazasi,
            standartGuzest, isciGuzest, isciGuzestAd,
            hys, hysIsv, avans,
            mezGun, mezOdenis, mezKesinti,
            xesSirketGun, xesDsmfGun, xesSirketOdenis, xesDsmfOdenis, xesKesinti,
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

        // Avans
        set('[data-p="avans"]', fmt(d.avans), d.avans > 0 ? 'n n--r' : 'n n--d');

        // HYS detail cells
        set('[data-p="hysisci"]', fmt(d.hys), d.hys > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="hysisv"]', fmt(d.hysIsv), d.hysIsv > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="hysisv2"]', fmt(d.hysIsv), d.hysIsv > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="vergibazasi"]', fmt(d.vergiDsmfBazasi), d.vergiDsmfBazasi > 0 ? 'n n--au' : 'n n--d');

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
            // İşçi tərəfi
            a.brut     += d.brut;
            a.gelirV   += d.gelirV;
            a.dsmfIsci += d.dsmf;
            a.issIsci  += d.iss;
            a.itssIsci += d.itss;
            a.hysIsci  += d.hys;
            a.avans    += d.avans;
            a.tutulma  += d.tutulma;
            a.net      += d.net;
            // Şirkət tərəfi
            a.dsmfIsv  += d.dsmfIsv;
            a.issIsv   += d.issIsv;
            a.itssIsv  += d.itssIsv;
            a.hysIsv   += d.hysIsv;
            a.sirketEx += d.sirketCemi;
            a.sirket   += d.brut + d.sirketCemi;
            return a;
        }, {
            brut: 0, gelirV: 0, dsmfIsci: 0, issIsci: 0, itssIsci: 0, hysIsci: 0, avans: 0, tutulma: 0, net: 0,
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
        s('mthFootTutulma', fmt(t.tutulma));
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

        [row.querySelector('.mth-inp--b'), row.querySelector('.mth-inp--c')].forEach(inp => {
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

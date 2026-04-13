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
    try {
        const rawEl = document.getElementById('mthVergiConfig');
        if (rawEl) {
            const cfg = JSON.parse(rawEl.textContent);
            PILLELER = cfg.pilleler || {};
            VERGI_GUZESTI = parseFloat(cfg.vergiGuzesti) || 200;
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
        const chk = row.querySelector('.mth-checkbox');
        const bInp = row.querySelector('.mth-inp--b');
        const cInp = row.querySelector('.mth-inp--c');
        const done = row.classList.contains('done');

        const bonus = parseFloat(bInp?.value || 0) || 0;
        const cerime = parseFloat(cInp?.value || 0) || 0;
        const brut = Math.max(esas + bonus - cerime, 0);

        // Vergilənəcək məbləğ: brut - vergi güzəşti
        const vergilenecek = Math.max(0, brut - VERGI_GUZESTI);

        // İşçidən tutulanlar (pilləli + flat)
        const gelirV  = hesablaTutulma(vergilenecek, 1, 0);       // 1 = GelirVergisi
        const dsmf    = hesablaTutulma(brut,         2, 0);       // 2 = DsmfIsci
        const iss     = hesablaTutulma(brut,         3, FLAT.issizlik); // flat
        const itss    = hesablaTutulma(brut,         4, 0);       // 4 = ItssIsci
        const tutulma = gelirV + dsmf + iss + itss;
        const net     = Math.max(brut - tutulma, 0);

        // İşəgötürən xərcləri (pilləli + flat)
        const dsmfIsv = hesablaTutulma(brut, 7, 0);               // 7 = DsmfIsegoturen
        const itssIsv = hesablaTutulma(brut, 9, 0);               // 9 = ItssIsegoturen
        const issIsv  = hesablaTutulma(brut, 8, FLAT.issizlikIsv);// flat
        const sirketCemi = dsmfIsv + issIsv + itssIsv;

        return {
            esas, bonus, cerime, brut, vergilenecek,
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

        // Detail row cells
        set('[data-p="gelirv"]', fmt(d.gelirV), d.gelirV > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="dsmf"]', fmt(d.dsmf), d.dsmf > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="issizlik"]', fmt(d.iss), d.iss > 0 ? 'n' : 'n n--d');
        set('[data-p="itss"]', fmt(d.itss), d.itss > 0 ? 'n' : 'n n--d');
        set('[data-p="tutulma"]', fmt(d.tutulma), d.tutulma > 0 ? 'n n--r' : 'n n--d');

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
            a.brut += d.brut;
            a.tutulma += d.tutulma;
            a.net += d.net;
            a.sirketEx += d.sirketCemi;          // yalnız işəgötürən əlavə xərclər
            a.sirket   += d.brut + d.sirketCemi; // brut + işəgötürən əlavələri (ümumi)
            return a;
        }, { brut: 0, tutulma: 0, net: 0, sirketEx: 0, sirket: 0 });

        const s = (id, v) => { const el = document.getElementById(id); if (el) el.textContent = v; };
        s('mthFootSayi', sel.length);
        s('mthFootBrut', fmt(t.brut));
        s('mthFootTutulma', fmt(t.tutulma));
        s('mthFootNet', fmt(t.net));
        s('mthFootNet2', fmt(t.net));
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

})();

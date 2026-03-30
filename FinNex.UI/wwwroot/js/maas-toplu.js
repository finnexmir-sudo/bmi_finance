/**
 * FinNex — Toplu Maaş Hesablaması
 * maas-toplu.js
 */
(function () {
    'use strict';

    const RATES = { gelirV: 0.14, dsmfIsci: 0.03, issizlik: 0.005, itss: 0.02 };

    // checkbox elementini birbaşa tap, label üzərindən deyil
    const chkAll = document.getElementById('mthChkAll');
    // label-ın özündə click event blokladığı halda birbaşa checkbox-a event ver
    if (chkAll) chkAll.style.pointerEvents = 'auto';
    const selChip = document.getElementById('mthSelChip');
    const btnCalc = document.getElementById('mthBtnCalc');
    const overlay = document.getElementById('mthOverlay');
    const btnOk = document.getElementById('mthBtnOk');
    const btnNo = document.getElementById('mthBtnNo');
    const mainForm = document.getElementById('mthForm');

    const rows = Array.from(document.querySelectorAll('tr[data-isci]'));

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
        const gelirV = brut * RATES.gelirV;
        const dsmf = brut * RATES.dsmfIsci;
        const iss = brut * RATES.issizlik;
        const itss = brut * RATES.itss;
        const tutulma = gelirV + dsmf + iss + itss;
        const net = Math.max(brut - tutulma, 0);

        return {
            esas, bonus, cerime, brut, gelirV, dsmf, iss, itss, tutulma, net,
            checked: !!chk?.checked && !done, done
        };
    }

    /* ── Format ───────────────────────────────────────────────── */
    const fmt = v => v > 0 ? v.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₼' : '—';

    /* ── Update preview cells for one row ────────────────────── */
    function updateRow(row) {
        const d = rd(row);
        const set = (sel, val, cls) => {
            const el = row.querySelector(sel);
            if (!el) return;
            el.textContent = val;
            if (cls) el.className = cls;
        };
        set('[data-p="brut"]', fmt(d.brut), d.brut > 0 ? 'n' : 'n n--d');
        set('[data-p="gelirv"]', fmt(d.gelirV), d.gelirV > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="dsmf"]', fmt(d.dsmf), d.dsmf > 0 ? 'n n--p' : 'n n--d');
        set('[data-p="issizlik"]', fmt(d.iss), d.iss > 0 ? 'n' : 'n n--d');
        set('[data-p="itss"]', fmt(d.itss), d.itss > 0 ? 'n' : 'n n--d');
        set('[data-p="tutulma"]', fmt(d.tutulma), d.tutulma > 0 ? 'n n--r' : 'n n--d');
        set('[data-p="net"]', fmt(d.net), d.net > 0 ? 'n n--au' : 'n n--d');
    }

    /* ── Footer totals ────────────────────────────────────────── */
    function updateFooter() {
        const sel = rows.filter(r => rd(r).checked);
        const t = sel.reduce((a, r) => {
            const d = rd(r);
            a.brut += d.brut;
            a.tutulma += d.tutulma;
            a.net += d.net;
            return a;
        }, { brut: 0, tutulma: 0, net: 0 });

        const s = (id, v) => { const el = document.getElementById(id); if (el) el.textContent = v; };
        s('mthFootSayi', sel.length);
        s('mthFootBrut', fmt(t.brut));
        s('mthFootTutulma', fmt(t.tutulma));
        s('mthFootNet', fmt(t.net));

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
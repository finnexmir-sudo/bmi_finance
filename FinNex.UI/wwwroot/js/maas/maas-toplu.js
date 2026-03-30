/**
 * FinNex — Toplu Maaş Hesablaması
 * maas-toplu.js
 */
(function () {
    'use strict';

    /* ── Vergi dərəcələri (UI göstərmək üçün, real hesab backend-dədir) ── */
    const RATES = {
        gelirVergisi: 0.14,
        dsmfIsci: 0.03,
        issizlikIsci: 0.005,
        dsmfIssverenElave: 0.22,
        itss: 0.02
    };

    /* ── DOM referansları ──────────────────────────────────────────────── */
    const chkAll = document.getElementById('mthCheckAll');
    const selInfo = document.getElementById('mthSelInfo');
    const btnHesabla = document.getElementById('mthBtnHesabla');
    const overlay = document.getElementById('mthOverlay');
    const btnConfirm = document.getElementById('mthBtnConfirm');
    const btnCancel = document.getElementById('mthBtnCancel');
    const modalIsci = document.getElementById('mthModalIsciSayi');
    const modalBrut = document.getElementById('mthModalBrut');
    const modalNet = document.getElementById('mthModalNet');
    const modalBonus = document.getElementById('mthModalBonus');
    const footSelSayi = document.getElementById('mthFootSelSayi');
    const footBrut = document.getElementById('mthFootBrut');
    const footTutulma = document.getElementById('mthFootTutulma');
    const footNet = document.getElementById('mthFootNet');
    const mainForm = document.getElementById('mthForm');

    /* ── Bütün sıralar ────────────────────────────────────────────────── */
    const rows = Array.from(document.querySelectorAll('.mth-row[data-isci-id]'));

    /* ── Köməkçi: bir sıranın məlumatlarını oxu ───────────────────────── */
    function rowData(row) {
        const isciId = row.dataset.isciId;
        const esasMaas = parseFloat(row.dataset.esasMaas || 0);
        const checkbox = row.querySelector('.mth-checkbox');
        const bonusInp = row.querySelector('.mth-input--bonus');
        const cerimeInp = row.querySelector('.mth-input--cerime');
        const alreadyDone = row.classList.contains('already-done');

        const bonus = parseFloat(bonusInp?.value || 0) || 0;
        const cerime = parseFloat(cerimeInp?.value || 0) || 0;

        const brut = esasMaas + bonus - cerime;
        const gelirVergisi = Math.max(brut * RATES.gelirVergisi, 0);
        const dsmfIsci = Math.max(brut * RATES.dsmfIsci, 0);
        const issizlikIsci = Math.max(brut * RATES.issizlikIsci, 0);
        const itss = Math.max(brut * RATES.itss, 0);
        const tutulmaTotal = gelirVergisi + dsmfIsci + issizlikIsci + itss;
        const net = Math.max(brut - tutulmaTotal, 0);

        return {
            isciId, esasMaas, bonus, cerime,
            brut, gelirVergisi, dsmfIsci, issizlikIsci, itss,
            tutulmaTotal, net,
            checked: checkbox?.checked && !alreadyDone,
            alreadyDone
        };
    }

    /* ── Preview qutusu yenilə (real-time) ───────────────────────────── */
    function updatePreview(row) {
        const d = rowData(row);
        const box = row.querySelector('.mth-preview-box');
        if (!box) return;

        const fmt = v => v > 0 ? v.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '—';

        box.querySelector('[data-prev="brut"]') && (box.querySelector('[data-prev="brut"]').textContent = fmt(d.brut));
        box.querySelector('[data-prev="tutulma"]') && (box.querySelector('[data-prev="tutulma"]').textContent = fmt(d.tutulmaTotal));
        box.querySelector('[data-prev="net"]') && (box.querySelector('[data-prev="net"]').textContent = fmt(d.net));

        const netEl = box.querySelector('[data-prev="net"]');
        if (netEl) {
            netEl.className = 'mth-prev-val ' + (d.net > 0 ? 'mth-prev-val--green' : 'mth-prev-val--dim');
        }

        /* Sıra totals pillər */
        const pillBrut = row.querySelector('[data-pill="brut"]');
        const pillTut = row.querySelector('[data-pill="tutulma"]');
        const pillNet = row.querySelector('[data-pill="net"]');
        if (pillBrut) pillBrut.textContent = d.brut > 0 ? fmt(d.brut) : '—';
        if (pillTut) pillTut.textContent = d.tutulmaTotal > 0 ? fmt(d.tutulmaTotal) : '—';
        if (pillNet) pillNet.textContent = d.net > 0 ? fmt(d.net) : '—';
    }

    /* ── Footer cəmi yenilə ───────────────────────────────────────────── */
    function updateFooter() {
        const selected = rows.filter(r => {
            const chk = r.querySelector('.mth-checkbox');
            return chk?.checked && !r.classList.contains('already-done');
        });

        const totals = selected.reduce((acc, row) => {
            const d = rowData(row);
            acc.brut += d.brut;
            acc.tutulma += d.tutulmaTotal;
            acc.net += d.net;
            return acc;
        }, { brut: 0, tutulma: 0, net: 0 });

        const fmt = v => v > 0
            ? v.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₼'
            : '— ₼';

        if (footSelSayi) footSelSayi.textContent = selected.length;
        if (footBrut) footBrut.textContent = fmt(totals.brut);
        if (footTutulma) footTutulma.textContent = fmt(totals.tutulma);
        if (footNet) footNet.textContent = fmt(totals.net);

        /* Selection info chip */
        if (selInfo) {
            if (selected.length > 0) {
                selInfo.textContent = selected.length + ' işçi seçilib';
                selInfo.classList.add('visible');
            } else {
                selInfo.classList.remove('visible');
            }
        }

        /* Hesabla düyməsi */
        if (btnHesabla) {
            btnHesabla.disabled = selected.length === 0;
        }

        /* CheckAll indeterminate */
        const eligibleRows = rows.filter(r => !r.classList.contains('already-done'));
        const allChecked = eligibleRows.every(r => r.querySelector('.mth-checkbox')?.checked);
        const noneChecked = eligibleRows.every(r => !r.querySelector('.mth-checkbox')?.checked);

        if (chkAll) {
            chkAll.checked = allChecked && eligibleRows.length > 0;
            chkAll.indeterminate = !allChecked && !noneChecked;
        }
    }

    /* ── "Hamısını seç" checkbox ──────────────────────────────────────── */
    if (chkAll) {
        chkAll.addEventListener('change', function () {
            rows.forEach(row => {
                if (row.classList.contains('already-done')) return;
                const chk = row.querySelector('.mth-checkbox');
                if (chk) chk.checked = this.checked;
                row.classList.toggle('selected', this.checked);
            });
            updateFooter();
        });
    }

    /* ── Fərdi checkbox-lar ───────────────────────────────────────────── */
    rows.forEach(row => {
        const chk = row.querySelector('.mth-checkbox');
        if (chk) {
            chk.addEventListener('change', function () {
                row.classList.toggle('selected', this.checked);
                updateFooter();
            });
        }

        /* Bonus / Cərimə input-ları real-time preview ──────────────── */
        const bonusInp = row.querySelector('.mth-input--bonus');
        const cerimeInp = row.querySelector('.mth-input--cerime');

        [bonusInp, cerimeInp].forEach(inp => {
            if (!inp) return;
            inp.addEventListener('input', () => updatePreview(row));
            inp.addEventListener('change', () => updatePreview(row));
        });

        /* İlkin preview ────────────────────────────────────────────── */
        updatePreview(row);
    });

    /* ── Hesabla düyməsi — modal aç ──────────────────────────────────── */
    if (btnHesabla) {
        btnHesabla.addEventListener('click', function () {
            const selected = rows.filter(r => {
                const chk = r.querySelector('.mth-checkbox');
                return chk?.checked && !r.classList.contains('already-done');
            });

            if (selected.length === 0) return;

            const totals = selected.reduce((acc, row) => {
                const d = rowData(row);
                acc.brut += d.brut;
                acc.net += d.net;
                acc.bonus += d.bonus;
                return acc;
            }, { brut: 0, net: 0, bonus: 0 });

            const fmt = v => v.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₼';

            if (modalIsci) modalIsci.textContent = selected.length + ' işçi';
            if (modalBrut) modalBrut.textContent = fmt(totals.brut);
            if (modalNet) modalNet.textContent = fmt(totals.net);
            if (modalBonus) modalBonus.textContent = fmt(totals.bonus);

            if (overlay) overlay.classList.add('open');
        });
    }

    /* ── Modal bağla ──────────────────────────────────────────────────── */
    if (btnCancel) {
        btnCancel.addEventListener('click', () => overlay?.classList.remove('open'));
    }

    if (overlay) {
        overlay.addEventListener('click', function (e) {
            if (e.target === this) this.classList.remove('open');
        });
    }

    /* Escape ilə bağla */
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') overlay?.classList.remove('open');
    });

    /* ── Modal Təsdiqlə — formu göndər ───────────────────────────────── */
    if (btnConfirm) {
        btnConfirm.addEventListener('click', function () {
            overlay?.classList.remove('open');
            if (mainForm) {
                /* Seçilmiş olmayan sırların input-larını disabled et
                   ki form-da göndərilməsin (backend tərəf filter edir, 
                   amma UX üçün əlavədir) */
                rows.forEach(row => {
                    const chk = row.querySelector('.mth-checkbox');
                    if (!chk?.checked || row.classList.contains('already-done')) {
                        row.querySelectorAll('input[type="number"]').forEach(inp => {
                            inp.disabled = true;
                        });
                    }
                });

                /* Loading state */
                btnConfirm.textContent = 'Göndərilir...';
                btnConfirm.disabled = true;
                mainForm.submit();
            }
        });
    }

    /* ── İlkin footer cəmini hesabla ─────────────────────────────────── */
    updateFooter();

    /* ── Rəqəm input — yalnız müsbət dəyər ───────────────────────────── */
    document.querySelectorAll('.mth-input').forEach(inp => {
        inp.addEventListener('blur', function () {
            const v = parseFloat(this.value);
            if (isNaN(v) || v < 0) this.value = '';
        });
    });

})();
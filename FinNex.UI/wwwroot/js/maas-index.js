/**
 * FinNex — Əmək Haqqı Siyahısı
 * maas-index.js
 */
(function () {
    'use strict';

    /* ── Status dəyişmə modal ─────────────────────────────────── */
    const overlay = document.getElementById('miOverlay');
    const modalTitle = document.getElementById('miModalTitle');
    const modalSub = document.getElementById('miModalSub');
    const modalIcon = document.getElementById('miModalIcon');
    const hiddenId = document.getElementById('miStatusId');
    const hiddenStat = document.getElementById('miStatusVal');
    const hiddenIl = document.getElementById('miStatusIl');
    const hiddenAy = document.getElementById('miStatusAy');
    const btnCancel = document.getElementById('miModalCancel');
    const statusForm = document.getElementById('miStatusForm');

    const statusMeta = {
        'Tesdiqlendi': {
            title: 'Maaşı təsdiqlə',
            sub: 'Bu maaş "Təsdiqləndi" statusuna keçirilsin?',
            icon: 'green',
            iconSvg: `<svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                <path d="M4 10l4 4 8-8" stroke="#1e9b6b" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>`
        },
        'Odenildi': {
            title: 'Maaş ödənildi',
            sub: 'Bu maaş "Ödənildi" olaraq işarələnsin? Bank köçürməsi aparıldıqda istifadə edin.',
            icon: 'green',
            iconSvg: `<svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                <rect x="2" y="5" width="16" height="12" rx="2" stroke="#1e9b6b" stroke-width="1.5"/>
                <path d="M2 9h16" stroke="#1e9b6b" stroke-width="1.5"/>
                <circle cx="6.5" cy="13" r="1.5" fill="#1e9b6b"/>
              </svg>`
        },
        'LeğvEdildi': {
            title: 'Maaşı ləğv et',
            sub: 'Bu maaş ləğv edilsin? Layihə statusundakı maaşlar silinə bilər.',
            icon: 'orange',
            iconSvg: `<svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                <circle cx="10" cy="10" r="8" stroke="#d85a30" stroke-width="1.5"/>
                <path d="M7 7l6 6M13 7l-6 6" stroke="#d85a30" stroke-width="1.5" stroke-linecap="round"/>
              </svg>`
        }
    };

    /* Trigger any status-change button */
    document.querySelectorAll('[data-status-trigger]').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = this.dataset.maasId;
            const stat = this.dataset.yeniStatus;
            const il = this.dataset.il;
            const ay = this.dataset.ay;
            const meta = statusMeta[stat] || {
                title: 'Status dəyişikliyi',
                sub: 'Əmin misiniz?',
                icon: 'green',
                iconSvg: ''
            };

            if (hiddenId) hiddenId.value = id;
            if (hiddenStat) hiddenStat.value = stat;
            if (hiddenIl) hiddenIl.value = il;
            if (hiddenAy) hiddenAy.value = ay;

            if (modalTitle) modalTitle.textContent = meta.title;
            if (modalSub) modalSub.textContent = meta.sub;
            if (modalIcon) {
                modalIcon.innerHTML = meta.iconSvg;
                modalIcon.className = `mi-modal-icon mi-modal-icon--${meta.icon}`;
            }

            overlay?.classList.add('open');
        });
    });

    if (btnCancel) btnCancel.addEventListener('click', () => overlay?.classList.remove('open'));
    overlay?.addEventListener('click', e => { if (e.target === overlay) overlay.classList.remove('open'); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape') overlay?.classList.remove('open'); });

    /* ── Delete confirm ───────────────────────────────────────── */
    document.querySelectorAll('[data-delete-trigger]').forEach(btn => {
        btn.addEventListener('click', function (e) {
            if (!confirm(`"${this.dataset.isciAd}" üçün maaşı silmək istədiyinizə əminsiniz?\nBu əməliyyat geri alına bilməz.`)) {
                e.preventDefault();
            }
        });
    });

    /* ── Filter auto-submit ───────────────────────────────────── */
    document.querySelectorAll('.mi-auto-filter').forEach(el => {
        el.addEventListener('change', function () {
            const form = this.closest('form') || document.getElementById('miFilterForm');
            form?.submit();
        });
    });

    /* ── Row hover — show action buttons ─────────────────────── */
    document.querySelectorAll('.mi-tr').forEach(row => {
        const actions = row.querySelector('.mi-actions');
        if (!actions) return;
        row.addEventListener('mouseenter', () => actions.style.opacity = '1');
        row.addEventListener('mouseleave', () => actions.style.opacity = '');
    });

    /* ── Client-side quick search ────────────────────────────── */
    const quickSearch = document.getElementById('miQuickSearch');
    const clearBtn = document.getElementById('miClearSearch');

    function normalizeText(s) {
        if (!s) return '';
        return s.toString().toLowerCase()
            .replaceAll('ə', 'e').replaceAll('ı', 'i').replaceAll('ö', 'o')
            .replaceAll('ü', 'u').replaceAll('ğ', 'g').replaceAll('ş', 's')
            .replaceAll('ç', 'c');
    }

    function filterRows() {
        const q = normalizeText(quickSearch?.value || '');
        const rows = document.querySelectorAll('.mi-tr');
        let visibleCount = 0;

        rows.forEach(row => {
            if (!q) {
                row.style.display = '';
                visibleCount++;
                return;
            }
            // Axtarış üçün bütün sətrin mətnini götür
            const text = normalizeText(row.textContent || '');
            if (text.includes(q)) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        // Show/hide clear button
        if (clearBtn) clearBtn.style.display = q ? 'flex' : 'none';

        // Show "no results" message if nothing matches
        const table = document.querySelector('.mi-table-wrap');
        let emptyMsg = document.getElementById('miSearchEmpty');
        if (q && visibleCount === 0 && table) {
            if (!emptyMsg) {
                emptyMsg = document.createElement('div');
                emptyMsg.id = 'miSearchEmpty';
                emptyMsg.className = 'mi-empty';
                emptyMsg.innerHTML = '<div style="padding:30px;text-align:center;color:#8a93a8;">🔍 "<span id="miSearchQuery"></span>" üçün nəticə tapılmadı</div>';
                table.appendChild(emptyMsg);
            }
            const qSpan = document.getElementById('miSearchQuery');
            if (qSpan) qSpan.textContent = quickSearch.value;
            emptyMsg.style.display = '';
        } else if (emptyMsg) {
            emptyMsg.style.display = 'none';
        }
    }

    if (quickSearch) {
        quickSearch.addEventListener('input', filterRows);
        quickSearch.addEventListener('keydown', e => {
            if (e.key === 'Escape') { quickSearch.value = ''; filterRows(); }
        });
    }
    if (clearBtn) {
        clearBtn.addEventListener('click', () => {
            if (quickSearch) { quickSearch.value = ''; filterRows(); quickSearch.focus(); }
        });
    }

})();
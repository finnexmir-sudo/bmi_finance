/**
 * mezuniyyet.js
 * FinNex HR – Leave Management Module Scripts
 */

(function () {
    'use strict';

    /* ──────────────────────────────────────────
       TABLE SEARCH & FILTER
    ────────────────────────────────────────── */
    function initTableFilter() {
        const searchInput = document.getElementById('mez-search');
        const statusFilter = document.getElementById('mez-filter-status');
        const typeFilter = document.getElementById('mez-filter-type');
        const tableBody = document.getElementById('mez-table-body');
        const emptyState = document.getElementById('mez-empty-state');
        const countEl = document.getElementById('mez-row-count');

        if (!searchInput || !tableBody) return;

        function filterRows() {
            const searchVal = searchInput.value.toLowerCase().trim();
            const statusVal = statusFilter ? statusFilter.value : '';
            const typeVal = typeFilter ? typeFilter.value : '';

            const rows = tableBody.querySelectorAll('tr[data-row]');
            let visible = 0;

            rows.forEach(function (row) {
                const text = row.textContent.toLowerCase();
                const status = row.dataset.status || '';
                const type = row.dataset.type || '';

                const matchSearch = !searchVal || text.includes(searchVal);
                const matchStatus = !statusVal || status === statusVal;
                const matchType = !typeVal || type === typeVal;

                const show = matchSearch && matchStatus && matchType;
                row.style.display = show ? '' : 'none';
                if (show) visible++;
            });

            if (emptyState) emptyState.style.display = visible === 0 ? '' : 'none';
            if (countEl) countEl.textContent = visible + ' nəticə';
        }

        searchInput.addEventListener('input', filterRows);
        if (statusFilter) statusFilter.addEventListener('change', filterRows);
        if (typeFilter) typeFilter.addEventListener('change', filterRows);
    }

    /* ──────────────────────────────────────────
       DELETE MODAL
    ────────────────────────────────────────── */
    function initDeleteModal() {
        const modal = document.getElementById('deleteModal');
        const nameEl = document.getElementById('deleteEmployeeName');
        const deleteForm = document.getElementById('deleteForm');

        if (!modal) return;

        modal.addEventListener('show.bs.modal', function (e) {
            const btn = e.relatedTarget;
            const id = btn.dataset.id;
            const name = btn.dataset.name;

            if (nameEl) nameEl.textContent = name || '';
            if (deleteForm) deleteForm.action = deleteForm.dataset.baseUrl + '/' + id;
        });
    }

    /* ──────────────────────────────────────────
       DATE VALIDATION
    ────────────────────────────────────────── */
    function initDateValidation() {
        const startDate = document.getElementById('BaslamaTarixi');
        const endDate = document.getElementById('BitmeTarixi');
        const dayCount = document.getElementById('mez-day-count');

        if (!startDate || !endDate) return;

        function calcDays() {
            const s = new Date(startDate.value);
            const e = new Date(endDate.value);

            if (!startDate.value || !endDate.value) return;

            // Clear error
            endDate.classList.remove('is-invalid');
            const errEl = document.getElementById('date-range-error');
            if (errEl) errEl.style.display = 'none';

            if (e < s) {
                endDate.classList.add('is-invalid');
                if (errEl) {
                    errEl.textContent = 'Bitmə tarixi başlama tarixindən əvvəl ola bilməz';
                    errEl.style.display = 'block';
                }
                if (dayCount) dayCount.textContent = '—';
                return;
            }

            const diffMs = e - s;
            const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24)) + 1;
            if (dayCount) dayCount.textContent = diffDays + ' gün';
        }

        startDate.addEventListener('change', calcDays);
        endDate.addEventListener('change', calcDays);
        calcDays(); // initial
    }

    /* ──────────────────────────────────────────
       FORM VALIDATION BOOTSTRAP STYLE
    ────────────────────────────────────────── */
    function initFormValidation() {
        const forms = document.querySelectorAll('.mez-validated-form');
        forms.forEach(function (form) {
            form.addEventListener('submit', function (e) {
                if (!form.checkValidity()) {
                    e.preventDefault();
                    e.stopPropagation();
                }
                form.classList.add('was-validated');
            });
        });
    }

    /* ──────────────────────────────────────────
       AVATAR INITIALS GENERATOR
    ────────────────────────────────────────── */
    function initAvatars() {
        document.querySelectorAll('.employee-avatar[data-name]').forEach(function (el) {
            const name = el.dataset.name || '';
            const parts = name.trim().split(' ');
            const initials = parts.length >= 2
                ? parts[0][0] + parts[parts.length - 1][0]
                : (parts[0] || '?')[0];
            el.textContent = initials.toUpperCase();
        });
    }

    /* ──────────────────────────────────────────
       TABLE SORT
    ────────────────────────────────────────── */
    function initTableSort() {
        document.querySelectorAll('.mez-table thead th[data-sort]').forEach(function (th) {
            th.addEventListener('click', function () {
                const col = th.dataset.sort;
                const asc = th.dataset.dir !== 'asc';
                th.dataset.dir = asc ? 'asc' : 'desc';

                // Reset others
                document.querySelectorAll('.mez-table thead th[data-sort]').forEach(function (t) {
                    if (t !== th) delete t.dataset.dir;
                });

                const tbody = document.getElementById('mez-table-body');
                if (!tbody) return;

                const rows = Array.from(tbody.querySelectorAll('tr[data-row]'));
                rows.sort(function (a, b) {
                    const aVal = (a.dataset[col] || '').toLowerCase();
                    const bVal = (b.dataset[col] || '').toLowerCase();
                    return asc ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
                });

                rows.forEach(function (r) { tbody.appendChild(r); });
            });
        });
    }

    /* ──────────────────────────────────────────
       TOOLTIPS
    ────────────────────────────────────────── */
    function initTooltips() {
        var tooltipEls = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipEls.map(function (el) {
            return new bootstrap.Tooltip(el, { trigger: 'hover' });
        });
    }

    /* ──────────────────────────────────────────
       INIT
    ────────────────────────────────────────── */
    document.addEventListener('DOMContentLoaded', function () {
        initTableFilter();
        initDeleteModal();
        initDateValidation();
        initFormValidation();
        initAvatars();
        initTableSort();
        if (typeof bootstrap !== 'undefined') initTooltips();
    });

})();

'use strict';

// ── INDEX: Search + Filter ──
(function initIndexFilter() {
    const searchInput = document.getElementById('mezSearch');
    const statusFilter = document.getElementById('mezStatusFilter');
    const typeFilter = document.getElementById('mezTypeFilter');
    const tableBody = document.getElementById('mezTableBody');
    const noResults = document.getElementById('noResults');

    if (!searchInput || !tableBody) return;

    function filterRows() {
        const q = searchInput.value.trim().toLowerCase();
        const status = statusFilter ? statusFilter.value : '';
        const type = typeFilter ? typeFilter.value : '';

        let visible = 0;
        const rows = tableBody.querySelectorAll('tr[data-row]');

        rows.forEach(row => {
            const name = (row.dataset.name || '').toLowerCase();
            const rowStatus = row.dataset.status || '';
            const rowType = row.dataset.type || '';

            const matchQ = !q || name.includes(q);
            const matchStatus = !status || rowStatus === status;
            const matchType = !type || rowType === type;

            if (matchQ && matchStatus && matchType) {
                row.style.display = '';
                visible++;
            } else {
                row.style.display = 'none';
            }
        });

        if (noResults) {
            noResults.style.display = visible === 0 ? 'block' : 'none';
        }
    }

    searchInput.addEventListener('input', filterRows);
    if (statusFilter) statusFilter.addEventListener('change', filterRows);
    if (typeFilter) typeFilter.addEventListener('change', filterRows);
})();

// ── DELETE MODAL ──
(function initDeleteModal() {
    const deleteModal = document.getElementById('deleteModal');
    if (!deleteModal) return;

    deleteModal.addEventListener('show.bs.modal', function (event) {
        const trigger = event.relatedTarget;
        if (!trigger) return;

        const id = trigger.dataset.id;
        const name = trigger.dataset.name;

        const nameEl = deleteModal.querySelector('#deleteTargetName');
        const formEl = deleteModal.querySelector('#deleteForm');

        if (nameEl) nameEl.textContent = name || '—';
        if (formEl && id) {
            const action = formEl.dataset.baseAction;
            formEl.action = action.replace('__ID__', id);
        }
    });
})();

// ── CREATE / EDIT: Client-side Validation ──
(function initFormValidation() {
    const forms = document.querySelectorAll('.mez-validate-form');

    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            let valid = true;

            // Required fields
            form.querySelectorAll('[data-required]').forEach(field => {
                if (!field.value || field.value.trim() === '') {
                    setInvalid(field, 'Bu sahə məcburidir.');
                    valid = false;
                } else {
                    setValid(field);
                }
            });

            // Date range validation
            const startDate = form.querySelector('[name="BaslamaTarixi"]');
            const endDate = form.querySelector('[name="BitmeTarixi"]');

            if (startDate && endDate && startDate.value && endDate.value) {
                if (new Date(endDate.value) < new Date(startDate.value)) {
                    setInvalid(endDate, 'Bitmə tarixi başlama tarixindən əvvəl ola bilməz.');
                    valid = false;
                } else {
                    setValid(endDate);
                }
            }

            if (!valid) {
                e.preventDefault();
                const firstInvalid = form.querySelector('.mez-input.is-invalid, .mez-select.is-invalid');
                if (firstInvalid) {
                    firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    firstInvalid.focus();
                }
            }
        });

        // Live validation on blur
        form.querySelectorAll('.mez-input, .mez-select, .mez-textarea').forEach(field => {
            field.addEventListener('blur', function () {
                if (this.dataset.required && !this.value.trim()) {
                    setInvalid(this, 'Bu sahə məcburidir.');
                } else {
                    setValid(this);
                }
            });

            field.addEventListener('input', function () {
                if (this.classList.contains('is-invalid') && this.value.trim()) {
                    setValid(this);
                }
            });
        });
    });

    function setInvalid(field, msg) {
        field.classList.add('is-invalid');
        field.classList.remove('is-valid');
        let fb = field.parentElement.querySelector('.mez-invalid-feedback');
        if (!fb) {
            fb = field.closest('.mez-form-group')?.querySelector('.mez-invalid-feedback');
        }
        if (fb) {
            fb.textContent = msg;
            fb.style.display = 'block';
        }
    }

    function setValid(field) {
        field.classList.remove('is-invalid');
        const fb = field.closest('.mez-form-group')?.querySelector('.mez-invalid-feedback');
        if (fb) fb.style.display = 'none';
    }
})();

// ── DATE RANGE: Auto-calc duration ──
(function initDurationCalc() {
    const startInput = document.querySelector('[name="BaslamaTarixi"]');
    const endInput = document.querySelector('[name="BitmeTarixi"]');
    const durationDisplay = document.getElementById('durationDisplay');

    if (!startInput || !endInput || !durationDisplay) return;

    function updateDuration() {
        if (!startInput.value || !endInput.value) {
            durationDisplay.style.display = 'none';
            return;
        }

        const start = new Date(startInput.value);
        const end = new Date(endInput.value);
        const diffTime = end - start;

        if (diffTime < 0) {
            durationDisplay.style.display = 'none';
            return;
        }

        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
        durationDisplay.textContent = diffDays + ' gün';
        durationDisplay.style.display = 'inline-flex';
    }

    startInput.addEventListener('change', updateDuration);
    endInput.addEventListener('change', updateDuration);
    updateDuration();
})();

// ── TOOLTIP INIT ──
(function initTooltips() {
    const tooltipEls = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    if (typeof bootstrap !== 'undefined') {
        tooltipEls.forEach(el => new bootstrap.Tooltip(el, { trigger: 'hover' }));
    }
})();

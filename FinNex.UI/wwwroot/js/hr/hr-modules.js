/**
 * hr-modules.js
 * Shared JavaScript for HR module pages (Davamiyyet, Mezuniyyet, Maas)
 * Handles: status pickers, search debounce, table row click, tooltips
 */

(function () {
    'use strict';

    /* ── Status / Nov radio pickers ─────────────────────────── */
    function initRadioPickers() {
        document.querySelectorAll('.mod-status-picker, .mod-nov-picker').forEach(picker => {
            const radios = picker.querySelectorAll('input[type="radio"]');
            radios.forEach(radio => {
                // Set initial state
                if (radio.checked) {
                    radio.closest('label').classList.add('checked');
                }
                radio.addEventListener('change', () => {
                    picker.querySelectorAll('label').forEach(l => l.classList.remove('checked'));
                    radio.closest('label').classList.add('checked');
                });
            });
        });
    }

    /* ── Search debounce ─────────────────────────────────────── */
    function initSearchDebounce() {
        const searchInputs = document.querySelectorAll('input[name="SearchTerm"]');
        searchInputs.forEach(input => {
            let timer;
            input.addEventListener('input', () => {
                clearTimeout(timer);
                timer = setTimeout(() => {
                    input.closest('form').submit();
                }, 500);
            });
        });
    }

    /* ── Table row click → detail ────────────────────────────── */
    function initTableRowClick() {
        document.querySelectorAll('.mod-table tbody tr').forEach(row => {
            const detailLink = row.querySelector('.mod-btn-view');
            if (!detailLink) return;

            row.style.cursor = 'pointer';
            row.addEventListener('click', (e) => {
                // Ignore clicks on action buttons
                if (e.target.closest('.mod-action-btns')) return;
                detailLink.click();
            });
        });
    }

    /* ── Bootstrap tooltips ──────────────────────────────────── */
    function initTooltips() {
        if (typeof bootstrap === 'undefined' || !bootstrap.Tooltip) return;
        document.querySelectorAll('[title]').forEach(el => {
            new bootstrap.Tooltip(el, { trigger: 'hover', placement: 'top' });
        });
    }

    /* ── Alert auto-dismiss ──────────────────────────────────── */
    function initAlertDismiss() {
        document.querySelectorAll('.mod-alert-success, .mod-alert-info').forEach(alert => {
            setTimeout(() => {
                alert.style.opacity = '0';
                alert.style.transition = 'opacity .4s';
                setTimeout(() => alert.remove(), 400);
            }, 4000);
        });
    }

    /* ── Form dirty check ────────────────────────────────────── */
    function initDirtyCheck() {
        const forms = document.querySelectorAll('#editForm, #createForm');
        forms.forEach(form => {
            let dirty = false;
            form.querySelectorAll('input, select, textarea').forEach(el => {
                el.addEventListener('change', () => { dirty = true; });
            });
            form.addEventListener('submit', () => { dirty = false; });
            window.addEventListener('beforeunload', e => {
                if (dirty) {
                    e.preventDefault();
                    e.returnValue = 'Saxlanmamış dəyişikliklər var. Çıxmaq istəyirsiniz?';
                }
            });
        });
    }

    /* ── Animate stat cards ──────────────────────────────────── */
    function animateStats() {
        document.querySelectorAll('.mod-stat-val').forEach(el => {
            const text = el.textContent.trim();
            const num = parseFloat(text.replace(/[^\d.]/g, ''));
            if (isNaN(num) || num === 0) return;

            const suffix = text.replace(/[\d.,]/g, '').trim();
            let current = 0;
            const step = num / 30;
            const timer = setInterval(() => {
                current += step;
                if (current >= num) {
                    current = num;
                    clearInterval(timer);
                }
                el.textContent = (Number.isInteger(num)
                    ? Math.floor(current)
                    : current.toFixed(2)) + (suffix ? ' ' + suffix : '');
            }, 20);
        });
    }

    /* ── Init ────────────────────────────────────────────────── */
    document.addEventListener('DOMContentLoaded', () => {
        initRadioPickers();
        initSearchDebounce();
        initTableRowClick();
        initTooltips();
        initAlertDismiss();
        initDirtyCheck();

        // Small delay for stat animation
        setTimeout(animateStats, 200);
    });

})();

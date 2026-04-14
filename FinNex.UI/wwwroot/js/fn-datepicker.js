/**
 * FnDatePicker — Monday-first custom date picker
 * Vanilla JS, CSP-safe, no dependencies.
 *
 * <input type="date"> -i tekst halına gətirir, öz custom picker-ini göstərir.
 * input.value formatı yyyy-MM-dd qalır (form binding pozulmur).
 *
 * İstifadə:
 *   <input type="date" name="tarix" />   ← avtomatik
 *   <input type="date" data-no-fn-dp />  ← native qalsın
 */
(function () {
    'use strict';

    const MONTHS_AZ = [
        'Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'İyun',
        'İyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'
    ];
    // Monday-first week headers
    const DAYS_AZ = ['B.e', 'Ç.a', 'Ç', 'C.a', 'C', 'Ş', 'B'];

    function parseISO(v) {
        if (!v) return null;
        const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(v);
        if (!m) return null;
        const d = new Date(parseInt(m[1]), parseInt(m[2]) - 1, parseInt(m[3]));
        return isNaN(d) ? null : d;
    }

    function toISO(d) {
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        return `${y}-${m}-${dd}`;
    }

    function sameDay(a, b) {
        return a && b &&
            a.getFullYear() === b.getFullYear() &&
            a.getMonth() === b.getMonth() &&
            a.getDate() === b.getDate();
    }

    class FnDatePicker {
        constructor(input) {
            if (!input || input.dataset.fnDpInit === '1') return;
            input.dataset.fnDpInit = '1';

            this.input = input;
            this.popup = null;
            this.isOpen = false;

            // Native date input-u text-ə çevir (value yenə yyyy-MM-dd qalır)
            if (input.type === 'date') {
                input.type = 'text';
                input.setAttribute('readonly', 'readonly');
                input.autocomplete = 'off';
                input.placeholder = 'yyyy-mm-dd';
                input.classList.add('fn-dp-input');
            }

            this.input.addEventListener('click', (e) => {
                e.stopPropagation();
                this.open();
            });
            this.input.addEventListener('focus', () => this.open());
        }

        open() {
            if (this.isOpen) return;
            this.isOpen = true;
            this.viewMonth = parseISO(this.input.value) || new Date();
            this._render();
            setTimeout(() => document.addEventListener('click', this._onOutsideClick), 0);
        }

        close = () => {
            if (!this.isOpen) return;
            this.isOpen = false;
            if (this.popup) {
                this.popup.remove();
                this.popup = null;
            }
            document.removeEventListener('click', this._onOutsideClick);
            this.input.blur();
        }

        _onOutsideClick = (e) => {
            if (!this.popup) return;
            if (this.popup.contains(e.target) || e.target === this.input) return;
            this.close();
        }

        _render() {
            // Build the popup shell once, then reposition. Month navigation
            // only updates the title + grid — the popup is never destroyed,
            // so its position never jumps mid-interaction.
            if (this.popup) this.popup.remove();

            this.popup = document.createElement('div');
            this.popup.className = 'fn-dp-popup';
            this.popup.addEventListener('click', (e) => e.stopPropagation());
            // Prevent focus-stealing scroll inside the modal when clicking nav/cells
            this.popup.addEventListener('mousedown', (e) => e.preventDefault());

            // Header
            const head = document.createElement('div');
            head.className = 'fn-dp-head';
            const prev = this._makeNav('‹', () => { this.viewMonth.setMonth(this.viewMonth.getMonth() - 1); this._renderMonth(); });
            const next = this._makeNav('›', () => { this.viewMonth.setMonth(this.viewMonth.getMonth() + 1); this._renderMonth(); });
            this.titleEl = document.createElement('div');
            this.titleEl.className = 'fn-dp-title';
            head.appendChild(prev);
            head.appendChild(this.titleEl);
            head.appendChild(next);
            this.popup.appendChild(head);

            // Days of week header
            const daysHead = document.createElement('div');
            daysHead.className = 'fn-dp-days-head';
            DAYS_AZ.forEach((d, i) => {
                const el = document.createElement('div');
                el.className = 'fn-dp-day-name' + (i >= 5 ? ' fn-dp-weekend' : '');
                el.textContent = d;
                daysHead.appendChild(el);
            });
            this.popup.appendChild(daysHead);

            // Grid (contents filled by _renderMonth)
            this.gridEl = document.createElement('div');
            this.gridEl.className = 'fn-dp-grid';
            this.popup.appendChild(this.gridEl);

            // Footer
            const foot = document.createElement('div');
            foot.className = 'fn-dp-foot';
            const clearBtn = document.createElement('button');
            clearBtn.type = 'button';
            clearBtn.className = 'fn-dp-clear-btn';
            clearBtn.textContent = 'Təmizlə';
            clearBtn.onclick = () => {
                this.input.value = '';
                this.input.dispatchEvent(new Event('change', { bubbles: true }));
                this.close();
            };
            const todayBtn = document.createElement('button');
            todayBtn.type = 'button';
            todayBtn.className = 'fn-dp-today-btn';
            todayBtn.textContent = 'Bu gün';
            todayBtn.onclick = () => {
                this.input.value = toISO(new Date());
                this.input.dispatchEvent(new Event('change', { bubbles: true }));
                this.close();
            };
            foot.appendChild(clearBtn);
            foot.appendChild(todayBtn);
            this.popup.appendChild(foot);

            // Fill month contents
            this._renderMonth();

            // Position below input (once)
            const rect = this.input.getBoundingClientRect();
            this.popup.style.position = 'fixed';
            this.popup.style.top = (rect.bottom + 4) + 'px';
            this.popup.style.left = rect.left + 'px';
            this.popup.style.zIndex = '10000';

            document.body.appendChild(this.popup);

            // Ensure it's in viewport (once)
            const popupRect = this.popup.getBoundingClientRect();
            if (popupRect.right > window.innerWidth) {
                this.popup.style.left = (window.innerWidth - popupRect.width - 8) + 'px';
            }
            if (popupRect.bottom > window.innerHeight) {
                this.popup.style.top = (rect.top - popupRect.height - 4) + 'px';
            }
        }

        _renderMonth() {
            if (!this.titleEl || !this.gridEl) return;

            this.titleEl.textContent = `${MONTHS_AZ[this.viewMonth.getMonth()]} ${this.viewMonth.getFullYear()}`;
            this.gridEl.innerHTML = '';

            const firstDay = new Date(this.viewMonth.getFullYear(), this.viewMonth.getMonth(), 1);
            // Monday-first: Mon=0, Sun=6
            let startOffset = firstDay.getDay() - 1;
            if (startOffset < 0) startOffset = 6;

            const daysInMonth = new Date(this.viewMonth.getFullYear(), this.viewMonth.getMonth() + 1, 0).getDate();
            const prevMonthLastDay = new Date(this.viewMonth.getFullYear(), this.viewMonth.getMonth(), 0).getDate();

            const today = new Date();
            today.setHours(0, 0, 0, 0);
            const selected = parseISO(this.input.value);

            // Previous month filler
            for (let i = startOffset; i > 0; i--) {
                const cell = document.createElement('div');
                cell.className = 'fn-dp-cell fn-dp-cell--other';
                cell.textContent = prevMonthLastDay - i + 1;
                this.gridEl.appendChild(cell);
            }

            // Current month days
            for (let d = 1; d <= daysInMonth; d++) {
                const dateObj = new Date(this.viewMonth.getFullYear(), this.viewMonth.getMonth(), d);
                const cell = document.createElement('div');
                cell.className = 'fn-dp-cell';
                cell.textContent = d;

                const dow = dateObj.getDay();
                if (dow === 0 || dow === 6) cell.classList.add('fn-dp-weekend');
                if (sameDay(dateObj, today)) cell.classList.add('fn-dp-today');
                if (sameDay(dateObj, selected)) cell.classList.add('fn-dp-selected');

                cell.addEventListener('click', () => {
                    this.input.value = toISO(dateObj);
                    this.input.dispatchEvent(new Event('change', { bubbles: true }));
                    this.input.dispatchEvent(new Event('input', { bubbles: true }));
                    this.close();
                });
                this.gridEl.appendChild(cell);
            }

            // Next month filler (to complete 6 rows if needed)
            const totalCells = startOffset + daysInMonth;
            const rows = Math.ceil(totalCells / 7);
            const remaining = (rows * 7) - totalCells;
            for (let i = 1; i <= remaining; i++) {
                const cell = document.createElement('div');
                cell.className = 'fn-dp-cell fn-dp-cell--other';
                cell.textContent = i;
                this.gridEl.appendChild(cell);
            }
        }

        _makeNav(label, onclick) {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'fn-dp-nav';
            btn.innerHTML = label;
            btn.onclick = onclick;
            return btn;
        }
    }

    function initAll(root) {
        // OPT-IN: yalnız data-fn-datepicker atributu olan inputlar
        // Bu, mövcud səhifələrin native picker-ini pozmasın deyə
        (root || document).querySelectorAll('input[data-fn-datepicker]').forEach(input => {
            new FnDatePicker(input);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => initAll());
    } else {
        initAll();
    }

    window.FnDatePicker = FnDatePicker;
    window.fnDatePickerInitAll = initAll;
})();

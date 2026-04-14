// Bayram Gunleri - AJAX CRUD

const bgModal = {
    overlay: null,
    form: null,
    titleEl: null,
    idField: null,
    adField: null,
    baslangicField: null,
    bitisField: null,
    herIlField: null,
    submitBtn: null,
    isEdit: false,

    init() {
        this.overlay = document.getElementById('bgModalOverlay');
        this.form = document.getElementById('bgForm');
        this.titleEl = document.getElementById('bgModalTitle');
        this.idField = document.getElementById('bgId');
        this.adField = document.getElementById('bgAd');
        this.baslangicField = document.getElementById('bgBaslangicTarix');
        this.bitisField = document.getElementById('bgBitisTarix');
        this.herIlField = document.getElementById('bgHerIl');
        this.submitBtn = document.getElementById('bgSubmitBtn');

        this.form.addEventListener('submit', (e) => {
            e.preventDefault();
            this.submit();
        });

        this.overlay.addEventListener('click', (e) => {
            if (e.target === this.overlay) this.close();
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') this.close();
        });
    },

    open() {
        this.isEdit = false;
        this.titleEl.textContent = 'Yeni Qeyd';
        this.submitBtn.textContent = 'Yadda saxla';
        this.resetForm();
        this.bitisField.closest('.bg-form-half').style.display = '';
        // Default tip = Bayram
        const tip1 = document.getElementById('bgTip1');
        if (tip1) tip1.checked = true;
        this.overlay.classList.add('bg-active');
        setTimeout(() => this.adField.focus(), 100);
    },

    openEdit(id) {
        this.isEdit = true;
        this.titleEl.textContent = 'Qeydi Düzəlt';
        this.submitBtn.textContent = 'Yenilə';
        this.resetForm();
        this.bitisField.closest('.bg-form-half').style.display = 'none';

        fetch(`/HR/BayramGunu/Get?id=${id}`)
            .then(r => r.json())
            .then(data => {
                this.idField.value = data.id;
                this.adField.value = data.ad;
                this.baslangicField.value = data.tarix;
                this.herIlField.checked = data.herIlTeyinOlunur;
                // Set tip radio
                const tipId = data.tip === 2 ? 'bgTip2' : 'bgTip1';
                const radio = document.getElementById(tipId);
                if (radio) radio.checked = true;
                this.overlay.classList.add('bg-active');
                setTimeout(() => this.adField.focus(), 100);
            })
            .catch(() => bgToast('Melumat yuklenilmedi.', false));
    },

    close() {
        this.overlay.classList.remove('bg-active');
        this.resetForm();
    },

    resetForm() {
        this.form.reset();
        this.idField.value = '';
        this.clearErrors();
    },

    clearErrors() {
        document.getElementById('bgAdError').textContent = '';
        document.getElementById('bgTarixError').textContent = '';
        document.getElementById('bgBitisTarixError').textContent = '';
        this.adField.classList.remove('bg-input-error');
        this.baslangicField.classList.remove('bg-input-error');
        this.bitisField.classList.remove('bg-input-error');
    },

    validate() {
        let valid = true;
        this.clearErrors();

        if (!this.adField.value.trim()) {
            document.getElementById('bgAdError').textContent = 'Bayram adi bos ola bilmez.';
            this.adField.classList.add('bg-input-error');
            valid = false;
        }

        if (!this.baslangicField.value) {
            document.getElementById('bgTarixError').textContent = 'Baslangic tarix secin.';
            this.baslangicField.classList.add('bg-input-error');
            valid = false;
        }

        if (this.bitisField.value && this.baslangicField.value && this.bitisField.value < this.baslangicField.value) {
            document.getElementById('bgBitisTarixError').textContent = 'Bitis tarixi baslangicdan evvel ola bilmez.';
            this.bitisField.classList.add('bg-input-error');
            valid = false;
        }

        return valid;
    },

    submit() {
        if (!this.validate()) return;

        const url = this.isEdit ? '/HR/BayramGunu/Edit' : '/HR/BayramGunu/Create';
        const formData = new FormData(this.form);

        // Checkbox handling: if not checked, don't send true
        if (!this.herIlField.checked) {
            formData.set('herIlTeyinOlunur', 'false');
        }

        this.submitBtn.disabled = true;
        this.submitBtn.textContent = 'Gozleyin...';

        fetch(url, {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    bgToast(data.message, true);
                    this.close();
                    refreshTable();
                    // Calendar-ı da yenilə
                    if (typeof bgLoadYear === 'function') {
                        bgLoadYear(bgCurrentYear);
                    }
                } else {
                    bgToast(data.message, false);
                }
            })
            .catch(() => bgToast('Xeta bas verdi.', false))
            .finally(() => {
                this.submitBtn.disabled = false;
                this.submitBtn.textContent = this.isEdit ? 'Yenile' : 'Yadda saxla';
            });
    }
};

function bgDelete(id) {
    if (!confirm('Bu bayrami silmek isteyirsiniz?')) return;

    const formData = new FormData();
    formData.append('id', id);

    fetch('/HR/BayramGunu/Delete', {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': getAntiForgeryToken()
        }
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                bgToast(data.message, true);
                refreshTable();
            } else {
                bgToast(data.message, false);
            }
        })
        .catch(() => bgToast('Silme zamani xeta bas verdi.', false));
}

function refreshTable() {
    fetch('/HR/BayramGunu/List')
        .then(r => r.json())
        .then(data => {
            const tbody = document.getElementById('bgTableBody');
            const records = data.records;

            document.getElementById('statCemi').textContent = data.stats.cemi;
            document.getElementById('statGelecek').textContent = data.stats.gelecek;

            if (records.length === 0) {
                tbody.innerHTML = '';
                // Show empty state
                const card = document.querySelector('.bg-table-card');
                let emptyEl = card.querySelector('.bg-empty');
                if (!emptyEl) {
                    emptyEl = document.createElement('div');
                    emptyEl.className = 'bg-empty';
                    emptyEl.innerHTML = '<i class="bi bi-calendar-x"></i><p>Hec bir bayram tapilmadi</p>';
                    card.appendChild(emptyEl);
                }
                return;
            }

            // Remove empty state if exists
            const emptyEl = document.querySelector('.bg-table-card .bg-empty');
            if (emptyEl) emptyEl.remove();

            tbody.innerHTML = records.map(r => {
                const tipBadge = r.tip === 2
                    ? '<span class="bg-badge bg-badge--work"><i class="bi bi-briefcase-fill"></i> İş günü</span>'
                    : '<span class="bg-badge bg-badge--holiday"><i class="bi bi-calendar-heart"></i> Bayram</span>';
                const herIlBadge = r.herIlTeyinOlunur
                    ? '<span class="bg-badge bg-badge--yes"><i class="bi bi-check-circle-fill"></i> Bəli</span>'
                    : '<span class="bg-badge bg-badge--no"><i class="bi bi-x-circle"></i> Xeyr</span>';
                return `
                <tr data-id="${r.id}">
                    <td>${r.index}</td>
                    <td>${escapeHtml(r.ad)}</td>
                    <td>${r.tarix}</td>
                    <td>${tipBadge}</td>
                    <td>${herIlBadge}</td>
                    <td>
                        <button class="bg-btn-icon bg-btn-icon--edit" data-edit-id="${r.id}" title="Duzelis et">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button class="bg-btn-icon bg-btn-icon--delete" data-delete-id="${r.id}" title="Sil">
                            <i class="bi bi-trash3"></i>
                        </button>
                    </td>
                </tr>`;
            }).join('');
        })
        .catch(() => bgToast('Cedvel yenilenilmedi.', false));
}

function getAntiForgeryToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

function escapeHtml(str) {
    if (!str) return '';
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

function bgToast(message, success) {
    const toast = document.getElementById('bgToast');
    const msgEl = document.getElementById('bgToastMsg');
    msgEl.textContent = message;
    toast.className = 'bg-toast bg-toast-show ' + (success ? 'bg-toast-success' : 'bg-toast-error');
    setTimeout(() => {
        toast.classList.remove('bg-toast-show');
    }, 3000);
}

// ═══════════════════════════════════════════════════════════
// YEAR CALENDAR
// ═══════════════════════════════════════════════════════════

const MONTHS_AZ = ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'İyun',
    'İyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'];
const WEEK_HEADERS_AZ = ['B.e', 'Ç.a', 'Ç', 'C.a', 'C', 'Ş', 'B'];

let bgCurrentYear = new Date().getFullYear();
let bgYearData = null;

function bgLoadYear(year) {
    bgCurrentYear = year;
    document.getElementById('bgCalendarYear').textContent = year;
    const grid = document.getElementById('bgCalendarGrid');
    grid.innerHTML = '<div class="bg-calendar-loading">Yüklənir...</div>';

    fetch(`/HR/BayramGunu/Year?year=${year}`)
        .then(r => r.json())
        .then(data => {
            if (!data.success) {
                grid.innerHTML = '<div class="bg-calendar-loading">Xəta baş verdi.</div>';
                return;
            }
            bgYearData = data;
            bgRenderYear();
        })
        .catch(() => {
            grid.innerHTML = '<div class="bg-calendar-loading">Xəta baş verdi.</div>';
        });
}

function bgRenderYear() {
    const grid = document.getElementById('bgCalendarGrid');
    grid.innerHTML = '';

    // Records lookup by date
    const recordsByDate = {};
    bgYearData.records.forEach(r => { recordsByDate[r.tarix] = r; });

    const today = bgYearData.today; // yyyy-MM-dd

    for (let m = 0; m < 12; m++) {
        const monthEl = document.createElement('div');
        monthEl.className = 'bg-month';

        // Title
        const title = document.createElement('div');
        title.className = 'bg-month-title';
        title.textContent = MONTHS_AZ[m];
        monthEl.appendChild(title);

        // Header
        const header = document.createElement('div');
        header.className = 'bg-month-header';
        WEEK_HEADERS_AZ.forEach((wd, i) => {
            const el = document.createElement('div');
            el.textContent = wd;
            if (i >= 5) el.classList.add('bg-weekend-hdr');
            header.appendChild(el);
        });
        monthEl.appendChild(header);

        // Days grid
        const days = document.createElement('div');
        days.className = 'bg-month-days';

        const firstDay = new Date(bgCurrentYear, m, 1);
        let startOffset = firstDay.getDay() - 1; // Monday-first
        if (startOffset < 0) startOffset = 6;

        const daysInMonth = new Date(bgCurrentYear, m + 1, 0).getDate();

        // Previous month filler
        for (let i = 0; i < startOffset; i++) {
            const cell = document.createElement('div');
            cell.className = 'bg-day bg-day--other';
            days.appendChild(cell);
        }

        // Current month days
        for (let d = 1; d <= daysInMonth; d++) {
            const cell = document.createElement('div');
            cell.className = 'bg-day';
            cell.textContent = d;

            const dateStr = `${bgCurrentYear}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
            cell.dataset.date = dateStr;

            const dateObj = new Date(bgCurrentYear, m, d);
            const dow = dateObj.getDay();
            const isWeekend = (dow === 0 || dow === 6);

            const record = recordsByDate[dateStr];
            if (record) {
                if (record.tip === 2) {
                    cell.classList.add('bg-day--work-override');
                    cell.title = `${record.ad} (İş günü)`;
                } else {
                    cell.classList.add('bg-day--holiday');
                    cell.title = `${record.ad} (Bayram)`;
                }
                cell.dataset.recordId = record.id;
                cell.dataset.tip = record.tip;
            } else if (isWeekend) {
                cell.classList.add('bg-day--weekend');
                cell.title = 'Şənbə/Bazar — kliklə iş günü et';
            } else {
                cell.title = 'İş günü — kliklə bayram et';
            }

            // Past date?
            if (dateStr < today) {
                cell.classList.add('bg-day--past');
            } else if (dateStr === today) {
                cell.classList.add('bg-day--today');
            }

            days.appendChild(cell);
        }

        monthEl.appendChild(days);
        grid.appendChild(monthEl);
    }
}

function bgToggleDay(cell) {
    const dateStr = cell.dataset.date;
    if (!dateStr) return;
    if (cell.classList.contains('bg-day--past')) return;
    if (cell.dataset.bgBusy === '1') return;

    // Current state → desired tip
    let tip;
    if (cell.classList.contains('bg-day--holiday')) {
        // Remove (toggle off) — server sees existing record and deletes
        tip = 1;
    } else if (cell.classList.contains('bg-day--work-override')) {
        tip = 2;
    } else if (cell.classList.contains('bg-day--weekend')) {
        tip = 2; // weekend → work override
    } else {
        tip = 1; // work day → holiday
    }

    const formData = new FormData();
    formData.append('tarix', dateStr);
    formData.append('tip', tip);

    cell.dataset.bgBusy = '1';
    cell.style.opacity = '0.5';
    fetch('/HR/BayramGunu/Toggle', {
        method: 'POST',
        body: formData,
        headers: { 'RequestVerificationToken': getAntiForgeryToken() }
    })
        .then(r => r.json())
        .then(data => {
            cell.style.opacity = '';
            delete cell.dataset.bgBusy;
            if (!data.success) {
                bgToast(data.message || 'Xəta baş verdi', false);
                return;
            }
            bgToast(data.message, true);

            // Update ONLY the clicked cell in-place (no full reload → scroll stays)
            bgUpdateCellState(cell, data);

            // Keep local cache in sync for subsequent clicks
            if (bgYearData && Array.isArray(bgYearData.records)) {
                if (data.action === 'removed') {
                    bgYearData.records = bgYearData.records.filter(r => r.tarix !== dateStr);
                } else if (data.action === 'added') {
                    bgYearData.records.push({
                        id: data.id,
                        tarix: dateStr,
                        ad: data.ad,
                        tip: data.tip
                    });
                }
            }

            // Refresh bottom table list without touching calendar scroll
            refreshTable();
        })
        .catch(() => {
            cell.style.opacity = '';
            delete cell.dataset.bgBusy;
            bgToast('Şəbəkə xətası', false);
        });
}

function bgUpdateCellState(cell, data) {
    const dateStr = cell.dataset.date;
    // Clear state classes
    cell.classList.remove('bg-day--holiday', 'bg-day--work-override', 'bg-day--weekend');
    delete cell.dataset.recordId;
    delete cell.dataset.tip;

    if (data.action === 'added') {
        if (data.tip === 2) {
            cell.classList.add('bg-day--work-override');
            cell.title = `${data.ad} (İş günü)`;
        } else {
            cell.classList.add('bg-day--holiday');
            cell.title = `${data.ad} (Bayram)`;
        }
        cell.dataset.recordId = data.id;
        cell.dataset.tip = data.tip;
    } else {
        // Removed — fall back to default weekday/weekend state
        const parts = dateStr.split('-');
        const dateObj = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
        const dow = dateObj.getDay();
        if (dow === 0 || dow === 6) {
            cell.classList.add('bg-day--weekend');
            cell.title = 'Şənbə/Bazar — kliklə iş günü et';
        } else {
            cell.title = 'İş günü — kliklə bayram et';
        }
    }
}

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', () => {
    bgModal.init();

    // Event delegation for buttons (CSP-safe, no inline onclick)
    document.getElementById('bgOpenCreateBtn').addEventListener('click', () => bgModal.open());
    document.getElementById('bgCloseModalBtn').addEventListener('click', () => bgModal.close());
    document.getElementById('bgCancelBtn').addEventListener('click', () => bgModal.close());

    // Year navigation
    const prev = document.getElementById('bgYearPrev');
    const next = document.getElementById('bgYearNext');
    const today = document.getElementById('bgYearToday');
    if (prev) prev.addEventListener('click', () => bgLoadYear(bgCurrentYear - 1));
    if (next) next.addEventListener('click', () => bgLoadYear(bgCurrentYear + 1));
    if (today) today.addEventListener('click', () => bgLoadYear(new Date().getFullYear()));

    // Calendar click delegation
    const grid = document.getElementById('bgCalendarGrid');
    if (grid) {
        grid.addEventListener('click', (e) => {
            const cell = e.target.closest('.bg-day');
            if (!cell) return;
            if (cell.classList.contains('bg-day--other')) return;
            bgToggleDay(cell);
        });
    }

    // Initial year load
    bgLoadYear(new Date().getFullYear());

    // Table button delegation
    document.addEventListener('click', (e) => {
        const editBtn = e.target.closest('[data-edit-id]');
        if (editBtn) {
            bgModal.openEdit(parseInt(editBtn.getAttribute('data-edit-id')));
            return;
        }
        const deleteBtn = e.target.closest('[data-delete-id]');
        if (deleteBtn) {
            bgDelete(parseInt(deleteBtn.getAttribute('data-delete-id')));
            return;
        }
    });
});

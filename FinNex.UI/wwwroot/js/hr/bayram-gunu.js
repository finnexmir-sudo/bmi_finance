// Bayram Gunleri - AJAX CRUD

const bgModal = {
    overlay: null,
    form: null,
    titleEl: null,
    idField: null,
    adField: null,
    tarixField: null,
    herIlField: null,
    submitBtn: null,
    isEdit: false,

    init() {
        this.overlay = document.getElementById('bgModalOverlay');
        this.form = document.getElementById('bgForm');
        this.titleEl = document.getElementById('bgModalTitle');
        this.idField = document.getElementById('bgId');
        this.adField = document.getElementById('bgAd');
        this.tarixField = document.getElementById('bgTarix');
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
        this.titleEl.textContent = 'Yeni Bayram';
        this.submitBtn.textContent = 'Yadda saxla';
        this.resetForm();
        this.overlay.classList.add('bg-active');
        setTimeout(() => this.adField.focus(), 100);
    },

    openEdit(id) {
        this.isEdit = true;
        this.titleEl.textContent = 'Bayram Duzelisi';
        this.submitBtn.textContent = 'Yenile';
        this.resetForm();

        fetch(`/HR/BayramGunu/Get?id=${id}`)
            .then(r => r.json())
            .then(data => {
                this.idField.value = data.id;
                this.adField.value = data.ad;
                this.tarixField.value = data.tarix;
                this.herIlField.checked = data.herIlTeyinOlunur;
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
        this.adField.classList.remove('bg-input-error');
        this.tarixField.classList.remove('bg-input-error');
    },

    validate() {
        let valid = true;
        this.clearErrors();

        if (!this.adField.value.trim()) {
            document.getElementById('bgAdError').textContent = 'Bayram adi bos ola bilmez.';
            this.adField.classList.add('bg-input-error');
            valid = false;
        }

        if (!this.tarixField.value) {
            document.getElementById('bgTarixError').textContent = 'Tarix secin.';
            this.tarixField.classList.add('bg-input-error');
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

            tbody.innerHTML = records.map(r => `
                <tr data-id="${r.id}">
                    <td>${r.index}</td>
                    <td>${escapeHtml(r.ad)}</td>
                    <td>${r.tarix}</td>
                    <td>
                        ${r.herIlTeyinOlunur
                    ? '<span class="bg-badge bg-badge--yes"><i class="bi bi-check-circle-fill"></i> Beli</span>'
                    : '<span class="bg-badge bg-badge--no"><i class="bi bi-x-circle"></i> Xeyr</span>'}
                    </td>
                    <td>
                        <button class="bg-btn-icon bg-btn-icon--edit" onclick="bgModal.openEdit(${r.id})" title="Duzelis et">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button class="bg-btn-icon bg-btn-icon--delete" onclick="bgDelete(${r.id})" title="Sil">
                            <i class="bi bi-trash3"></i>
                        </button>
                    </td>
                </tr>
            `).join('');
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

// Initialize on DOM ready
document.addEventListener('DOMContentLoaded', () => {
    bgModal.init();
});

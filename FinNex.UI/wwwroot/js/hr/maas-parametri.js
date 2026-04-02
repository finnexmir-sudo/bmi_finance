// ── Maas Parametri CRUD (Vergi Parametrləri) ──

const MaasParametri = (() => {
    const overlay = () => document.getElementById('mpModalOverlay');
    const form    = () => document.getElementById('mpForm');
    const title   = () => document.getElementById('mpModalTitle');
    const token   = () => document.querySelector('input[name="__RequestVerificationToken"]').value;

    // Enum nov -> tip mapping (auto-set Tip based on Nov)
    const novTipMap = {
        1: 1, // GelirVergisiFaizi -> Faiz
        2: 1, // DsmfFaizi -> Faiz
        3: 1, // IssizlikSigortasiFaizi -> Faiz
        4: 1, // IcbariTibbiSigortaFaizi -> Faiz
        5: 2, // MinimumEmekHaqqi -> Mebleg
        6: 2, // VergiGuzestiMeblegi -> Mebleg
        7: 1, // DsmfIsegoturenFaizi -> Faiz
        8: 1  // IssizlikIsegoturenFaizi -> Faiz
    };

    function openCreate() {
        form().reset();
        document.getElementById('mpId').value = '';
        document.getElementById('mpAktivdirWrap').style.display = 'none';
        title().textContent = 'Yeni Parametr';
        updateSuffix();
        overlay().classList.add('mp-show');
    }

    function openEdit(id) {
        fetch(`/HR/MaasParametri/GetById/${id}`)
            .then(r => r.json())
            .then(data => {
                if (!data.success) { toast(data.message, 'error'); return; }
                const d = data.data;
                document.getElementById('mpId').value = d.Id;
                document.getElementById('mpNov').value = d.Nov;
                document.getElementById('mpTip').value = d.Tip;
                document.getElementById('mpDeyer').value = d.Deyer;
                document.getElementById('mpAciqlama').value = d.Aciqlama || '';
                document.getElementById('mpBaslamaTarixi').value = d.BaslamaTarixi;
                document.getElementById('mpBitmeTarixi').value = d.BitmeTarixi || '';
                document.getElementById('mpAktivdir').checked = d.Aktivdir;
                document.getElementById('mpAktivdirWrap').style.display = 'flex';
                title().textContent = 'Parametri Redaktə Et';
                updateSuffix();
                overlay().classList.add('mp-show');
            })
            .catch(() => toast('Məlumat yüklənərkən xəta baş verdi.', 'error'));
    }

    function closeModal() {
        overlay().classList.remove('mp-show');
    }

    function save() {
        if (!validate()) return;

        const id = document.getElementById('mpId').value;
        const isEdit = id !== '';
        const url = isEdit ? `/HR/MaasParametri/Edit/${id}` : '/HR/MaasParametri/Create';

        const formData = new FormData();
        formData.append('__RequestVerificationToken', token());
        formData.append('nov', document.getElementById('mpNov').value);
        formData.append('tip', document.getElementById('mpTip').value);
        formData.append('deyer', document.getElementById('mpDeyer').value);
        formData.append('aciqlama', document.getElementById('mpAciqlama').value);
        formData.append('baslamaTarixi', document.getElementById('mpBaslamaTarixi').value);
        formData.append('bitmeTarixi', document.getElementById('mpBitmeTarixi').value);

        if (isEdit) {
            formData.append('aktivdir', document.getElementById('mpAktivdir').checked);
        }

        const btn = document.getElementById('mpSaveBtn');
        btn.disabled = true;

        fetch(url, { method: 'POST', body: formData })
            .then(r => r.json())
            .then(data => {
                btn.disabled = false;
                if (data.success) {
                    toast(data.message, 'success');
                    closeModal();
                    setTimeout(() => location.reload(), 600);
                } else {
                    toast(data.message, 'error');
                }
            })
            .catch(() => {
                btn.disabled = false;
                toast('Xəta baş verdi.', 'error');
            });
    }

    function remove(id) {
        if (!confirm('Bu parametri silmək istədiyinizə əminsiniz?')) return;

        const formData = new FormData();
        formData.append('__RequestVerificationToken', token());

        fetch(`/HR/MaasParametri/Delete/${id}`, { method: 'POST', body: formData })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    toast(data.message, 'success');
                    setTimeout(() => location.reload(), 600);
                } else {
                    toast(data.message, 'error');
                }
            })
            .catch(() => toast('Silmə zamanı xəta baş verdi.', 'error'));
    }

    function validate() {
        let valid = true;
        const fields = ['mpNov', 'mpDeyer', 'mpBaslamaTarixi'];
        fields.forEach(fid => {
            const el = document.getElementById(fid);
            if (!el.value || el.value === '' || el.value === '0') {
                el.classList.add('mp-invalid');
                valid = false;
            } else {
                el.classList.remove('mp-invalid');
            }
        });

        const deyer = parseFloat(document.getElementById('mpDeyer').value);
        if (isNaN(deyer) || deyer <= 0) {
            document.getElementById('mpDeyer').classList.add('mp-invalid');
            valid = false;
        }

        if (!valid) toast('Zəhmət olmasa bütün sahələri doldurun.', 'error');
        return valid;
    }

    function updateSuffix() {
        const tipVal = parseInt(document.getElementById('mpTip').value);
        const suffixEl = document.getElementById('mpDeyerSuffix');
        suffixEl.textContent = tipVal === 2 ? '\u20BC' : '%';
    }

    function onNovChange() {
        const novVal = parseInt(document.getElementById('mpNov').value);
        if (novTipMap[novVal]) {
            document.getElementById('mpTip').value = novTipMap[novVal];
            updateSuffix();
        }
    }

    function toast(msg, type) {
        const existing = document.querySelectorAll('.mp-toast');
        existing.forEach(e => e.remove());

        const el = document.createElement('div');
        el.className = `mp-toast mp-toast--${type}`;
        el.textContent = msg;
        document.body.appendChild(el);
        setTimeout(() => el.remove(), 3000);
    }

    // Init
    function init() {
        // Close on overlay click
        overlay().addEventListener('click', e => {
            if (e.target === overlay()) closeModal();
        });

        // Close on Escape
        document.addEventListener('keydown', e => {
            if (e.key === 'Escape') closeModal();
        });

        // Nov change -> auto set Tip
        document.getElementById('mpNov').addEventListener('change', onNovChange);

        // Tip change -> update suffix
        document.getElementById('mpTip').addEventListener('change', updateSuffix);
    }

    document.addEventListener('DOMContentLoaded', init);

    return { openCreate, openEdit, closeModal, save, remove };
})();

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
        overlay().classList.add('mp-active');
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
                overlay().classList.add('mp-active');
            })
            .catch(() => toast('Məlumat yüklənərkən xəta baş verdi.', 'error'));
    }

    function closeModal() {
        overlay().classList.remove('mp-active');
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

    // ───────────────────────────────────────────────────────
    // PİLLƏLİ VERGİ DƏRƏCƏLƏRİ CRUD
    // ───────────────────────────────────────────────────────
    const pilleOverlay = () => document.getElementById('mpPilleModalOverlay');
    const pilleForm    = () => document.getElementById('mpPilleForm');
    const pilleTitle   = () => document.getElementById('mpPilleModalTitle');

    function pilleOpenCreate() {
        pilleForm().reset();
        document.getElementById('mpPilleId').value = '';
        document.getElementById('mpPilleSabit').value = '0';
        document.getElementById('mpPilleSira').value = '1';
        document.getElementById('mpPilleAktivdirWrap').style.display = 'none';
        pilleTitle().textContent = 'Yeni Pillə';
        pilleOverlay().classList.add('mp-active');
    }

    function pilleOpenEdit(id) {
        fetch(`/HR/MaasParametri/PilleGetById/${id}`)
            .then(r => r.json())
            .then(data => {
                if (!data.success) { toast(data.message, 'error'); return; }
                const d = data.data;
                document.getElementById('mpPilleId').value = d.Id;
                document.getElementById('mpPilleNov').value = d.Nov;
                document.getElementById('mpPilleSira').value = d.Sira;
                document.getElementById('mpPilleAsagi').value = d.AsagiHedd;
                document.getElementById('mpPilleYuxari').value = d.YuxariHedd || '';
                document.getElementById('mpPilleFaiz').value = d.Faiz;
                document.getElementById('mpPilleSabit').value = d.SabitMebleg;
                document.getElementById('mpPilleAciqlama').value = d.Aciqlama || '';
                document.getElementById('mpPilleBaslama').value = d.BaslamaTarixi;
                document.getElementById('mpPilleBitme').value = d.BitmeTarixi || '';
                document.getElementById('mpPilleAktivdir').checked = d.Aktivdir;
                document.getElementById('mpPilleAktivdirWrap').style.display = 'flex';
                pilleTitle().textContent = 'Pilləni Redaktə Et';
                pilleOverlay().classList.add('mp-active');
            })
            .catch(() => toast('Məlumat yüklənərkən xəta baş verdi.', 'error'));
    }

    function pilleCloseModal() {
        pilleOverlay().classList.remove('mp-active');
    }

    function pilleSave() {
        const asagi = parseFloat(document.getElementById('mpPilleAsagi').value);
        const yuxariStr = document.getElementById('mpPilleYuxari').value;
        const yuxari = yuxariStr === '' ? null : parseFloat(yuxariStr);
        const faiz = parseFloat(document.getElementById('mpPilleFaiz').value);
        const sabit = parseFloat(document.getElementById('mpPilleSabit').value) || 0;
        const sira = parseInt(document.getElementById('mpPilleSira').value);
        const baslama = document.getElementById('mpPilleBaslama').value;

        if (isNaN(asagi) || asagi < 0) { toast('Aşağı hədd düzgün deyil.', 'error'); return; }
        if (yuxari !== null && yuxari <= asagi) { toast('Yuxarı hədd aşağı həddən böyük olmalıdır.', 'error'); return; }
        if (isNaN(faiz) || faiz < 0) { toast('Faiz düzgün deyil.', 'error'); return; }
        if (isNaN(sira) || sira < 1) { toast('Sıra 1-dən kiçik ola bilməz.', 'error'); return; }
        if (!baslama) { toast('Başlama tarixi mütləqdir.', 'error'); return; }

        const id = document.getElementById('mpPilleId').value;
        const isEdit = id !== '';
        const url = isEdit ? `/HR/MaasParametri/PilleEdit/${id}` : '/HR/MaasParametri/PilleCreate';

        const formData = new FormData();
        formData.append('__RequestVerificationToken', token());
        formData.append('nov', document.getElementById('mpPilleNov').value);
        formData.append('sira', sira);
        formData.append('asagiHedd', asagi);
        if (yuxari !== null) formData.append('yuxariHedd', yuxari);
        formData.append('faiz', faiz);
        formData.append('sabitMebleg', sabit);
        formData.append('aciqlama', document.getElementById('mpPilleAciqlama').value);
        formData.append('baslamaTarixi', baslama);
        const bitme = document.getElementById('mpPilleBitme').value;
        if (bitme) formData.append('bitmeTarixi', bitme);
        if (isEdit) {
            formData.append('aktivdir', document.getElementById('mpPilleAktivdir').checked);
        }

        const btn = document.getElementById('mpPilleSaveBtn');
        btn.disabled = true;

        fetch(url, { method: 'POST', body: formData })
            .then(r => r.json())
            .then(data => {
                btn.disabled = false;
                if (data.success) {
                    toast(data.message, 'success');
                    pilleCloseModal();
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

    function pilleRemove(id) {
        if (!confirm('Bu pilləni silmək istədiyinizə əminsiniz?')) return;

        const formData = new FormData();
        formData.append('__RequestVerificationToken', token());

        fetch(`/HR/MaasParametri/PilleDelete/${id}`, { method: 'POST', body: formData })
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

    document.addEventListener('DOMContentLoaded', () => {
        init();

        // Event delegation (CSP-safe)
        document.getElementById('mpOpenCreate').addEventListener('click', openCreate);
        document.getElementById('mpCloseBtn').addEventListener('click', closeModal);
        document.getElementById('mpCancelBtn').addEventListener('click', closeModal);
        document.getElementById('mpSaveBtn').addEventListener('click', save);

        // Pillə buttons (may not exist on all pages)
        const pilleCreateBtn = document.getElementById('mpOpenPilleCreate');
        if (pilleCreateBtn) pilleCreateBtn.addEventListener('click', pilleOpenCreate);
        const pilleCloseBtn = document.getElementById('mpPilleCloseBtn');
        if (pilleCloseBtn) pilleCloseBtn.addEventListener('click', pilleCloseModal);
        const pilleCancelBtn = document.getElementById('mpPilleCancelBtn');
        if (pilleCancelBtn) pilleCancelBtn.addEventListener('click', pilleCloseModal);
        const pilleSaveBtn = document.getElementById('mpPilleSaveBtn');
        if (pilleSaveBtn) pilleSaveBtn.addEventListener('click', pilleSave);

        // Pillə modal close on overlay/Escape
        const pOverlay = pilleOverlay();
        if (pOverlay) {
            pOverlay.addEventListener('click', e => { if (e.target === pOverlay) pilleCloseModal(); });
        }
        document.addEventListener('keydown', e => { if (e.key === 'Escape') pilleCloseModal(); });

        document.addEventListener('click', (e) => {
            const editBtn = e.target.closest('[data-edit-id]');
            if (editBtn) { openEdit(parseInt(editBtn.getAttribute('data-edit-id'))); return; }
            const deleteBtn = e.target.closest('[data-delete-id]');
            if (deleteBtn) { remove(parseInt(deleteBtn.getAttribute('data-delete-id'))); return; }

            // Pillə edit/delete
            const editPille = e.target.closest('[data-edit-pille-id]');
            if (editPille) { pilleOpenEdit(parseInt(editPille.getAttribute('data-edit-pille-id'))); return; }
            const deletePille = e.target.closest('[data-delete-pille-id]');
            if (deletePille) { pilleRemove(parseInt(deletePille.getAttribute('data-delete-pille-id'))); return; }
        });
    });

    return { openCreate, openEdit, closeModal, save, remove, pilleOpenCreate, pilleOpenEdit, pilleSave, pilleRemove };
})();

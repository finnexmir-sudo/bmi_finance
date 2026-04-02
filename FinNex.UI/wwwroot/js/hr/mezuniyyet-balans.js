// ── Mezuniyyet Balans JS ──────────────────────────────────

(function () {
    'use strict';

    // ── Year selector ────────────────────────────────────
    const yearSelect = document.getElementById('mbYearSelect');
    if (yearSelect) {
        yearSelect.addEventListener('change', function () {
            const il = this.value;
            window.location.href = '/HR/MezuniyyetBalans?il=' + il;
        });
    }

    // ── Toast ────────────────────────────────────────────
    function showToast(message, type) {
        let toast = document.getElementById('mbToast');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'mbToast';
            toast.className = 'mb-toast';
            document.body.appendChild(toast);
        }
        toast.textContent = message;
        toast.className = 'mb-toast mb-toast--' + type;
        // trigger reflow
        void toast.offsetWidth;
        toast.classList.add('mb-toast--visible');

        setTimeout(function () {
            toast.classList.remove('mb-toast--visible');
        }, 3000);
    }

    // ── Progress bar colors ──────────────────────────────
    function getColorClass(qaliq, toplam) {
        if (toplam === 0) return 'green';
        var pct = (qaliq / toplam) * 100;
        if (pct > 50) return 'green';
        if (pct >= 20) return 'yellow';
        return 'red';
    }

    function updateProgressBars() {
        document.querySelectorAll('.mb-progress__bar').forEach(function (bar) {
            var qaliq = parseInt(bar.dataset.qaliq) || 0;
            var toplam = parseInt(bar.dataset.toplam) || 0;
            var pct = toplam > 0 ? Math.round((qaliq / toplam) * 100) : 0;
            bar.style.width = pct + '%';

            bar.classList.remove('mb-progress__bar--green', 'mb-progress__bar--yellow', 'mb-progress__bar--red');
            bar.classList.add('mb-progress__bar--' + getColorClass(qaliq, toplam));
        });

        document.querySelectorAll('.mb-qaliq').forEach(function (el) {
            var qaliq = parseInt(el.dataset.qaliq) || 0;
            var toplam = parseInt(el.dataset.toplam) || 0;
            el.classList.remove('mb-qaliq--green', 'mb-qaliq--yellow', 'mb-qaliq--red');
            el.classList.add('mb-qaliq--' + getColorClass(qaliq, toplam));
        });
    }

    updateProgressBars();

    // ── Modal ────────────────────────────────────────────
    const overlay = document.getElementById('mbModalOverlay');
    const modal = overlay ? overlay.querySelector('.mb-modal') : null;

    // Store current edit data
    let editData = [];

    function openModal(isciId, isciAd, balanslar) {
        if (!overlay) return;

        document.getElementById('mbModalIsciAd').textContent = isciAd;
        editData = balanslar;

        const tbody = document.getElementById('mbEditTableBody');
        tbody.innerHTML = '';

        const novAdlari = { 1: 'Illik', 2: 'Xestelik', 3: 'Ezamiyyet' };
        const novLabels = { 1: 'Illik', 2: 'Xestelik', 3: 'Ezamiyyet' };

        balanslar.forEach(function (b) {
            const tr = document.createElement('tr');
            tr.innerHTML =
                '<td>' + (novLabels[b.nov] || b.nov) + '</td>' +
                '<td><input type="number" class="mb-edit-input" min="0" value="' + b.toplamGun + '" data-id="' + b.id + '" data-original="' + b.toplamGun + '"></td>' +
                '<td>' + b.istifade + '</td>' +
                '<td>' + b.qaliq + '</td>';
            tbody.appendChild(tr);
        });

        overlay.classList.add('mb-modal-overlay--active');
    }

    function closeModal() {
        if (overlay) {
            overlay.classList.remove('mb-modal-overlay--active');
        }
    }

    // Edit button click delegation
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.mb-btn-edit');
        if (!btn) return;

        const isciId = btn.dataset.isciId;
        const isciAd = btn.dataset.isciAd;
        let balanslar = [];
        try {
            balanslar = JSON.parse(btn.dataset.balanslar);
        } catch (err) { }

        openModal(isciId, isciAd, balanslar);
    });

    // Close modal
    if (overlay) {
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) closeModal();
        });
    }

    const closeBtn = document.getElementById('mbModalClose');
    if (closeBtn) closeBtn.addEventListener('click', closeModal);

    const cancelBtn = document.getElementById('mbModalCancel');
    if (cancelBtn) cancelBtn.addEventListener('click', closeModal);

    // Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeModal();
    });

    // ── Save Changes ─────────────────────────────────────
    const saveBtn = document.getElementById('mbModalSave');
    if (saveBtn) {
        saveBtn.addEventListener('click', async function () {
            const inputs = document.querySelectorAll('#mbEditTableBody .mb-edit-input');
            const updates = [];

            inputs.forEach(function (input) {
                const newVal = parseInt(input.value);
                const original = parseInt(input.dataset.original);
                if (newVal !== original) {
                    updates.push({ id: parseInt(input.dataset.id), toplamGun: newVal });
                }
            });

            if (updates.length === 0) {
                closeModal();
                return;
            }

            saveBtn.disabled = true;
            saveBtn.textContent = 'Saxlanilir...';

            let allOk = true;
            for (const u of updates) {
                try {
                    const resp = await fetch('/HR/MezuniyyetBalans/Update', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: 'id=' + u.id + '&toplamGun=' + u.toplamGun
                    });
                    const result = await resp.json();
                    if (!result.success) {
                        showToast(result.message, 'error');
                        allOk = false;
                        break;
                    }
                } catch (err) {
                    showToast('Server xetasi bas verdi.', 'error');
                    allOk = false;
                    break;
                }
            }

            saveBtn.disabled = false;
            saveBtn.textContent = 'Yadda saxla';

            if (allOk) {
                showToast('Balanslar yenilendi.', 'success');
                closeModal();
                setTimeout(function () { location.reload(); }, 800);
            }
        });
    }

    // ── Create New Year Balances ─────────────────────────
    const createBtn = document.getElementById('mbCreateYearBtn');
    if (createBtn) {
        createBtn.addEventListener('click', function () {
            const il = yearSelect ? yearSelect.value : new Date().getFullYear();
            const ok = confirm(il + '-ci il ucun butun aktiv iscilere yeni balans yaradilsin?');
            if (!ok) return;

            createBtn.disabled = true;
            createBtn.textContent = 'Yaradilir...';

            fetch('/HR/MezuniyyetBalans/YeniIlBalansYarat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'il=' + il
            })
                .then(function (r) { return r.json(); })
                .then(function (result) {
                    if (result.success) {
                        showToast(result.message, 'success');
                        setTimeout(function () { location.reload(); }, 800);
                    } else {
                        showToast(result.message, 'error');
                    }
                })
                .catch(function () {
                    showToast('Server xetasi bas verdi.', 'error');
                })
                .finally(function () {
                    createBtn.disabled = false;
                    createBtn.innerHTML = '<i class="bi bi-plus-circle"></i> Yeni il balansi yarat';
                });
        });
    }

})();

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

    // Store current edit context
    let currentIsciId = null;
    let currentIl = null;

    const novDefaults = [
        { nov: 1, ad: 'İllik məzuniyyət', defaultGun: 21 },
        { nov: 2, ad: 'Xəstəlik məzuniyyəti', defaultGun: 10 },
        { nov: 3, ad: 'Ezamiyyət', defaultGun: 30 }
    ];

    function openModal(isciId, isciAd, il, balanslar) {
        if (!overlay) return;

        currentIsciId = isciId;
        currentIl = il;
        document.getElementById('mbModalIsciAd').textContent = isciAd;

        const tbody = document.getElementById('mbEditTableBody');
        tbody.innerHTML = '';

        novDefaults.forEach(function (nd) {
            var existing = balanslar.find(function (b) { return b.nov === nd.nov; });
            var toplam = existing ? existing.toplamGun : nd.defaultGun;
            var istifade = existing ? existing.istifade : 0;
            var qaliq = toplam - istifade;

            var tr = document.createElement('tr');
            tr.innerHTML =
                '<td><strong>' + nd.ad + '</strong></td>' +
                '<td><input type="number" class="mb-edit-input" min="0" value="' + toplam + '" data-nov="' + nd.nov + '"></td>' +
                '<td>' + istifade + '</td>' +
                '<td class="mb-edit-qaliq">' + qaliq + '</td>';
            tbody.appendChild(tr);

            // Real-time qalıq hesablama
            var input = tr.querySelector('input');
            var qaliqTd = tr.querySelector('.mb-edit-qaliq');
            input.addEventListener('input', function () {
                var newToplam = parseInt(this.value) || 0;
                qaliqTd.textContent = newToplam - istifade;
            });
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
        const il = btn.dataset.il || yearSelect.value;
        let balanslar = [];
        try {
            balanslar = JSON.parse(btn.dataset.balanslar);
        } catch (err) { }

        openModal(isciId, isciAd, il, balanslar);
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
            var inputs = document.querySelectorAll('#mbEditTableBody .mb-edit-input');
            var illikGun = 0, xestelikGun = 0, ezamiyyetGun = 0;

            inputs.forEach(function (input) {
                var nov = parseInt(input.dataset.nov);
                var val = parseInt(input.value) || 0;
                if (nov === 1) illikGun = val;
                if (nov === 2) xestelikGun = val;
                if (nov === 3) ezamiyyetGun = val;
            });

            saveBtn.disabled = true;
            saveBtn.textContent = 'Saxlanılır...';

            try {
                var body = 'isciId=' + currentIsciId +
                    '&il=' + currentIl +
                    '&illikGun=' + illikGun +
                    '&xestelikGun=' + xestelikGun +
                    '&ezamiyyetGun=' + ezamiyyetGun;

                var resp = await fetch('/HR/MezuniyyetBalans/CreateOrUpdate', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: body
                });
                var result = await resp.json();

                if (result.success) {
                    showToast(result.message, 'success');
                    closeModal();
                    setTimeout(function () { location.reload(); }, 800);
                } else {
                    showToast(result.message, 'error');
                }
            } catch (err) {
                showToast('Server xətası baş verdi.', 'error');
            }

            saveBtn.disabled = false;
            saveBtn.textContent = 'Yadda saxla';
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

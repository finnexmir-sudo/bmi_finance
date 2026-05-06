// Xerc Kateqoriyalari JS

(function () {
    'use strict';

    var modal      = document.getElementById('katModal');
    var form       = document.getElementById('katForm');
    var title      = document.getElementById('katModalTitle');
    var idInput    = document.getElementById('katId');
    var adInput    = document.getElementById('katAd');
    var ikonInput  = document.getElementById('katIkon');
    var aktivRow   = document.getElementById('aktivRow');
    var aktivCheck = document.getElementById('katAktivdir');

    // Seçilmiş ikonu grid-də işarələ
    function selectIkon(val) {
        ikonInput.value = val || '';
        document.querySelectorAll('.xk-ikon-item').forEach(function (btn) {
            btn.classList.toggle('xk-ikon-item--selected', btn.dataset.ikon === (val || ''));
        });
    }

    function openModal(isEdit, data) {
        form.action = isEdit
            ? '/HR/XercKateqoriya/Yenile'
            : '/HR/XercKateqoriya/Yarat';

        title.textContent  = isEdit ? 'Kateqoriyanı redaktə et' : 'Yeni kateqoriya';
        idInput.value      = data && data.id   ? data.id   : 0;
        adInput.value      = data && data.ad   ? data.ad   : '';
        aktivCheck.checked = !data || data.aktiv !== 'false';
        aktivRow.style.display = isEdit ? 'block' : 'none';
        selectIkon(data && data.ikon ? data.ikon : '');
        modal.style.display = 'flex';
        setTimeout(function () { adInput.focus(); }, 50);
    }

    function closeModal() {
        modal.style.display = 'none';
    }

    // İkon grid — klik ilə seç
    document.querySelectorAll('.xk-ikon-item').forEach(function (btn) {
        btn.addEventListener('click', function () {
            selectIkon(this.dataset.ikon);
        });
    });

    // Yeni düyməsi
    document.getElementById('btnYeniKat').addEventListener('click', function () {
        openModal(false, null);
    });

    // Redaktə düymələri
    document.querySelectorAll('.xk-edit-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openModal(true, {
                id   : this.dataset.id,
                ad   : this.dataset.ad,
                ikon : this.dataset.ikon,
                aktiv: this.dataset.aktiv
            });
        });
    });

    // Modal bağla
    document.getElementById('katModalClose').addEventListener('click', closeModal);
    document.getElementById('katModalCancel').addEventListener('click', closeModal);
    modal.addEventListener('click', function (e) {
        if (e.target === this) closeModal();
    });
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && modal.style.display !== 'none') closeModal();
    });

})();

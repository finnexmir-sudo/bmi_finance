// ── Xərc Kateqoriyaları JS ───────────────────────────────

(function () {
    'use strict';

    var modal   = document.getElementById('katModal');
    var form    = document.getElementById('katForm');
    var title   = document.getElementById('katModalTitle');
    var idInput = document.getElementById('katId');
    var adInput = document.getElementById('katAd');
    var ikonInput  = document.getElementById('katIkon');
    var ikonPreview = document.getElementById('ikonPreview');
    var aktivRow   = document.getElementById('aktivRow');
    var aktivCheck = document.getElementById('katAktivdir');

    function openModal(isEdit, data) {
        form.action = isEdit
            ? '/HR/XercKateqoriya/Yenile'
            : '/HR/XercKateqoriya/Yarat';

        title.textContent = isEdit ? 'Kateqoriyanı redaktə et' : 'Yeni kateqoriya';
        idInput.value  = data?.id   ?? 0;
        adInput.value  = data?.ad   ?? '';
        ikonInput.value = data?.ikon ?? '';
        aktivCheck.checked = data?.aktiv !== 'false';
        aktivRow.style.display = isEdit ? 'block' : 'none';
        updatePreview();
        modal.style.display = 'flex';
        adInput.focus();
    }

    function closeModal() {
        modal.style.display = 'none';
    }

    function updatePreview() {
        var val = ikonInput.value.trim();
        ikonPreview.className = 'bi xk-ikon-preview ' + (val || 'bi-tag');
    }

    // Yeni düyməsi
    document.getElementById('btnYeniKat').addEventListener('click', function () {
        openModal(false, null);
    });

    // Redaktə düymələri
    document.querySelectorAll('.xk-edit-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openModal(true, {
                id:   this.dataset.id,
                ad:   this.dataset.ad,
                ikon: this.dataset.ikon,
                aktiv: this.dataset.aktiv
            });
        });
    });

    // İkon önizləmə
    ikonInput.addEventListener('input', updatePreview);

    // Modal bağla
    document.getElementById('katModalClose').addEventListener('click', closeModal);
    document.getElementById('katModalCancel').addEventListener('click', closeModal);
    modal.addEventListener('click', function (e) {
        if (e.target === this) closeModal();
    });

    // Klaviatura
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && modal.style.display !== 'none') closeModal();
    });

})();

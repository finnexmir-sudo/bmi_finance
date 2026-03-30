// wwwroot/js/hr/departament-index.js

document.querySelectorAll('[data-action="dept-edit"]').forEach(function (btn) {
    btn.addEventListener('click', function () {
        document.getElementById('editId').value = btn.dataset.id;
        document.getElementById('editAd').value = btn.dataset.ad;
        document.getElementById('editAciqlama').value = btn.dataset.aciqlama || '';
        document.getElementById('editModal').style.display = 'flex';
    });
});

document.querySelectorAll('[data-action="dept-delete"]').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var id = btn.dataset.id;
        var ad = btn.dataset.ad;
        var sayi = parseInt(btn.dataset.sayi) || 0;

        document.getElementById('deleteId').value = id;

        var blocked = document.getElementById('deptWarnBlocked');
        var confirm = document.getElementById('deptWarnConfirm');
        var delBtn = document.getElementById('deleteConfirmBtn');

        if (sayi > 0) {
            document.getElementById('deptWarnBlockedText').textContent =
                '"' + ad + '" departamentini silmək mümkün deyil. ' +
                'Bu departamentə bağlı ' + sayi + ' aktiv işçi var.';
            blocked.style.display = 'flex';
            confirm.style.display = 'none';
            delBtn.style.display = 'none';
        } else {
            document.getElementById('deptWarnConfirmText').textContent =
                '"' + ad + '" departamenti silinəcək. Bu əməliyyat geri qaytarıla bilməz.';
            confirm.style.display = 'flex';
            blocked.style.display = 'none';
            delBtn.style.display = 'inline-flex';
        }

        document.getElementById('deleteModal').style.display = 'flex';
    });
});

document.querySelectorAll('[data-close-modal]').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var modal = document.getElementById(btn.dataset.closeModal);
        if (modal) modal.style.display = 'none';
    });
});

document.querySelectorAll('.str-modal-overlay').forEach(function (overlay) {
    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) overlay.style.display = 'none';
    });
});
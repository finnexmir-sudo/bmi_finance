// wwwroot/js/hr/vezife-index.js

document.querySelectorAll('[data-action="vezife-edit"]').forEach(function (btn) {
    btn.addEventListener('click', function () {
        document.getElementById('vezifeEditId').value = btn.dataset.id;
        document.getElementById('vezifeEditAd').value = btn.dataset.ad;
        document.getElementById('vezifeEditAktiv').checked = btn.dataset.active === 'true';
        var yonlukInp = document.getElementById('vezifeEditYonluk');
        if (yonlukInp) yonlukInp.value = btn.dataset.yonluk || '';

        var deptSel = document.getElementById('vezifeEditDept');
        if (deptSel && btn.dataset.deptid) {
            deptSel.value = btn.dataset.deptid;
        }

        var mezSel = document.getElementById('vezifeEditMezGun');
        if (mezSel) {
            mezSel.value = btn.dataset.mezgun === '30' ? '30' : '21';
        }

        document.getElementById('vezifeEditModal').style.display = 'flex';
    });
});

document.querySelectorAll('[data-action="vezife-delete"]').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var id = btn.dataset.id;
        var ad = btn.dataset.ad;
        var sayi = parseInt(btn.dataset.sayi) || 0;

        document.getElementById('vezifeDeleteId').value = id;

        var blocked = document.getElementById('vezWarnBlocked');
        var confirm = document.getElementById('vezWarnConfirm');
        var delBtn = document.getElementById('vezifeDeleteBtn');

        if (sayi > 0) {
            document.getElementById('vezWarnBlockedText').textContent =
                '"' + ad + '" vəzifəsini silmək mümkün deyil. ' +
                'Bu vəzifəyə bağlı ' + sayi + ' işçi var.';
            blocked.style.display = 'flex';
            confirm.style.display = 'none';
            delBtn.style.display = 'none';
        } else {
            document.getElementById('vezWarnConfirmText').textContent =
                '"' + ad + '" vəzifəsi silinəcək. Bu əməliyyat geri qaytarıla bilməz.';
            confirm.style.display = 'flex';
            blocked.style.display = 'none';
            delBtn.style.display = 'inline-flex';
        }

        document.getElementById('vezifeDeleteModal').style.display = 'flex';
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
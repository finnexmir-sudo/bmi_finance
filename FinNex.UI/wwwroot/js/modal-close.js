// wwwroot/js/modal-close.js

document.querySelectorAll('[data-close-modal]').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var modal = document.getElementById(btn.dataset.closeModal);
        if (modal) modal.classList.add('str-modal-hidden');
    });
});

document.querySelectorAll('.str-modal-overlay').forEach(function (overlay) {
    overlay.addEventListener('click', function (e) {
        if (e.target === overlay) overlay.classList.add('str-modal-hidden');
    });
});
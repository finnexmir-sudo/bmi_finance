// struktur-modal.js — Modal açma/bağlama
(function () {
    // Açma: data-open-modal="modalId" attribute-u olan düymələr
    document.addEventListener('click', function (e) {
        var opener = e.target.closest('[data-open-modal]');
        if (opener) {
            var id = opener.getAttribute('data-open-modal');
            var modal = document.getElementById(id);
            if (modal) modal.classList.remove('str-modal-hidden');
        }
    });

    // Bağlama: data-close-modal="modalId" attribute-u olan düymələr
    document.addEventListener('click', function (e) {
        var closer = e.target.closest('[data-close-modal]');
        if (closer) {
            var id = closer.getAttribute('data-close-modal');
            var modal = document.getElementById(id);
            if (modal) modal.classList.add('str-modal-hidden');
        }
    });

    // Overlay-ə klik ilə bağlama
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('str-modal-overlay') && !e.target.classList.contains('str-modal-hidden')) {
            e.target.classList.add('str-modal-hidden');
        }
    });

    // ESC düyməsi ilə bağlama
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var modals = document.querySelectorAll('.str-modal-overlay:not(.str-modal-hidden)');
            modals.forEach(function (m) { m.classList.add('str-modal-hidden'); });
        }
    });
})();

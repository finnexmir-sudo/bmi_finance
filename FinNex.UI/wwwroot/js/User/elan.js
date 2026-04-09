// ── User Elan JS ─────────────────────────────────────────

(function () {
    'use strict';

    // Simple fade-in animation for cards
    var cards = document.querySelectorAll('.elan-card');
    cards.forEach(function (card, i) {
        card.style.opacity = '0';
        card.style.transform = 'translateY(12px)';
        card.style.transition = 'opacity 0.3s ease, transform 0.3s ease';

        setTimeout(function () {
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, i * 80);
    });

})();

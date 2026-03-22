/* =============================================
   FinNex — User Area Scripts
   ============================================= */

(function () {
    'use strict';

    // Auto-hide alerts after 4 seconds
    document.querySelectorAll('.fn-alert').forEach(function (el) {
        setTimeout(function () {
            el.style.transition = 'opacity .4s';
            el.style.opacity = '0';
            setTimeout(function () { el.remove(); }, 400);
        }, 4000);
    });

    // Active nav item highlight (fallback for Razor class binding)
    var currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('.fn-nav-item').forEach(function (link) {
        if (link.getAttribute('href') && currentPath.startsWith(link.getAttribute('href').toLowerCase())) {
            link.classList.add('active');
        }
    });

})();
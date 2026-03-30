/* =============================================
   FinNex — User Area Scripts
   Step-by-step accordion: only one sibling open at a time
   ============================================= */

(function () {
    'use strict';

    // Auto-hide alerts
    document.querySelectorAll('.fn-alert').forEach(function (el) {
        setTimeout(function () {
            el.style.transition = 'opacity .4s';
            el.style.opacity = '0';
            setTimeout(function () { el.remove(); }, 400);
        }, 4000);
    });

    // Active nav item highlight
    var currentPath = window.location.pathname.toLowerCase();
    document.querySelectorAll('.fn-nav-item').forEach(function (link) {
        var href = link.getAttribute('href');
        if (href && href !== '#' && currentPath.startsWith(href.toLowerCase())) {
            link.classList.add('active');
        }
    });

})();

document.addEventListener("DOMContentLoaded", function () {

    // Attach click to every group toggle
    document.querySelectorAll(".fn-nav-group-toggle").forEach(function (toggle) {
        toggle.addEventListener("click", function (e) {
            e.stopPropagation();

            var group = this.closest(".fn-nav-group");
            if (!group) return;

            var isOpen = group.classList.contains("open");

            // Close all sibling groups at the same level
            var parent = group.parentElement;
            parent.querySelectorAll(":scope > .fn-nav-group.open").forEach(function (sibling) {
                if (sibling !== group) {
                    sibling.classList.remove("open");
                }
            });

            // Toggle this group
            group.classList.toggle("open", !isOpen);
        });
    });

});
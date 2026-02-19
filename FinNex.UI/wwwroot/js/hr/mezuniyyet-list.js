/**
 * mezuniyyet-list.js
 * Məzuniyyət Index page specific JavaScript
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', () => {

        /* Highlight rows by status */
        document.querySelectorAll('.mod-table tbody tr').forEach(row => {
            const badge = row.querySelector('.badge-pill');
            if (!badge) return;

            if (badge.classList.contains('badge-warning')) {
                row.style.borderLeft = '3px solid #f59e0b';
            } else if (badge.classList.contains('badge-danger')) {
                row.style.borderLeft = '3px solid #ef4444';
            } else if (badge.classList.contains('badge-success')) {
                row.style.borderLeft = '3px solid #10b981';
            }
        });

    });

})();

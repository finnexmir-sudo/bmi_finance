/* =============================================
   FinNex — Tesdiq (Approval) Panel JS
   ============================================= */

(function () {
    'use strict';

    /* ── Tab switching ─────────────────────── */
    document.querySelectorAll('.tsd-tab').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var target = btn.dataset.tab;

            document.querySelectorAll('.tsd-tab').forEach(function (b) { b.classList.remove('active'); });
            btn.classList.add('active');

            document.querySelectorAll('.tsd-tab-panel').forEach(function (p) {
                p.style.display = p.dataset.panel === target ? '' : 'none';
            });
        });
    });

    /* ── Search / Filter ───────────────────── */
    document.querySelectorAll('.tsd-search-input').forEach(function (input) {
        input.addEventListener('input', function () {
            var query = input.value.trim().toLowerCase();
            var panelId = input.dataset.panel;
            var panel = document.querySelector('[data-panel="' + panelId + '"]');
            if (!panel) return;

            panel.querySelectorAll('.tsd-card').forEach(function (card) {
                var text = card.textContent.toLowerCase();
                card.style.display = text.includes(query) ? '' : 'none';
            });
        });
    });

    /* ── Confirm Modal ─────────────────────── */
    var pendingForm = null;
    var pendingStatus = null;

    var overlay = document.getElementById('tsd-confirm-overlay');
    var modalTitle = document.getElementById('tsd-modal-title');
    var modalSub = document.getElementById('tsd-modal-sub');
    var modalIcon = document.getElementById('tsd-modal-icon');
    var modalConfirmBtn = document.getElementById('tsd-modal-confirm');
    var modalCancelBtn = document.getElementById('tsd-modal-cancel');
    var modalHiddenStatus = document.getElementById('tsd-modal-status');

    function openModal(form, approve) {
        pendingForm = form;
        pendingStatus = approve;

        if (approve) {
            modalIcon && modalIcon.classList.replace('tsd-modal-icon--reject', 'tsd-modal-icon--approve');
            modalTitle && (modalTitle.textContent = 'Müraciəti təsdiqləyin');
            modalSub && (modalSub.textContent = 'Bu müraciəti təsdiqləmək istədiyinizə əminsiniz?');
            modalConfirmBtn && modalConfirmBtn.classList.replace('tsd-btn--reject', 'tsd-btn--approve');
            modalConfirmBtn && (modalConfirmBtn.textContent = 'Bəli, Təsdiqlə');
        } else {
            modalIcon && modalIcon.classList.replace('tsd-modal-icon--approve', 'tsd-modal-icon--reject');
            modalTitle && (modalTitle.textContent = 'Müraciətə imtina edin');
            modalSub && (modalSub.textContent = 'Bu müraciətə imtina etmək istədiyinizə əminsiniz? İmtina səbəbini qeyd edin.');
            modalConfirmBtn && modalConfirmBtn.classList.replace('tsd-btn--approve', 'tsd-btn--reject');
            modalConfirmBtn && (modalConfirmBtn.textContent = 'Bəli, İmtina et');
        }

        if (modalHiddenStatus) modalHiddenStatus.value = approve ? 'true' : 'false';
        overlay && overlay.classList.add('open');
    }

    function closeModal() {
        overlay && overlay.classList.remove('open');
        pendingForm = null;
        pendingStatus = null;
    }

    // Approve buttons
    document.querySelectorAll('[data-tesdiq-approve]').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            var formId = btn.dataset.tesdiqApprove;
            var form = document.getElementById(formId);
            openModal(form, true);
        });
    });

    // Reject buttons
    document.querySelectorAll('[data-tesdiq-reject]').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            var formId = btn.dataset.tesdiqReject;
            var form = document.getElementById(formId);
            openModal(form, false);
        });
    });

    // Confirm
    modalConfirmBtn && modalConfirmBtn.addEventListener('click', function () {
        if (pendingForm) {
            var statusInput = pendingForm.querySelector('input[name="status"]');
            if (statusInput) statusInput.value = pendingStatus ? 'true' : 'false';
            pendingForm.submit();
        }
        closeModal();
    });

    // Cancel
    modalCancelBtn && modalCancelBtn.addEventListener('click', closeModal);
    overlay && overlay.addEventListener('click', function (e) {
        if (e.target === overlay) closeModal();
    });

    // ESC key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeModal();
    });

    /* ── Toast Notifications ───────────────── */
    function showToast(message, type) {
        type = type || 'success';
        var container = document.getElementById('tsd-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'tsd-toast-container';
            container.className = 'tsd-toast-container';
            document.body.appendChild(container);
        }

        var toast = document.createElement('div');
        toast.className = 'tsd-toast' + (type === 'error' ? ' tsd-toast--error' : type === 'warn' ? ' tsd-toast--warn' : '');

        var iconSvg = type === 'error'
            ? '<svg width="16" height="16" viewBox="0 0 16 16" fill="none"><circle cx="8" cy="8" r="6" stroke="#c0392b" stroke-width="1.3"/><path d="M5.5 5.5l5 5M10.5 5.5l-5 5" stroke="#c0392b" stroke-width="1.3" stroke-linecap="round"/></svg>'
            : '<svg width="16" height="16" viewBox="0 0 16 16" fill="none"><circle cx="8" cy="8" r="6" stroke="#1e9b6b" stroke-width="1.3"/><path d="M5.5 8l2 2 3-3" stroke="#1e9b6b" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>';

        toast.innerHTML = iconSvg + '<span>' + message + '</span>';
        container.appendChild(toast);

        setTimeout(function () {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity .3s';
            setTimeout(function () { toast.remove(); }, 300);
        }, 3500);
    }

    // Auto-show toasts from TempData alerts
    document.querySelectorAll('.fn-alert').forEach(function (el) {
        var isError = el.classList.contains('fn-alert--error');
        showToast(el.textContent.trim(), isError ? 'error' : 'success');
    });

    /* ── Animated card entrance ────────────── */
    var cards = document.querySelectorAll('.tsd-card');
    cards.forEach(function (card, i) {
        card.style.animationDelay = (i * 0.04) + 's';
    });

    /* ── Expose showToast globally ─────────── */
    window.tsdShowToast = showToast;
})();

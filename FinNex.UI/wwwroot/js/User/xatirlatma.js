// wwwroot/js/User/xatirlatma.js

(function () {
    'use strict';

    // ── Antiforgery token ──────────────────────────────
    function getToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    // ── URL-ləri view-dan al ───────────────────────────
    const container = document.getElementById('xatirlatmaContainer');
    const oxuUrl = container ? container.dataset.oxuUrl : '';
    const hamisiniOxuUrl = container ? container.dataset.hamisiniOxuUrl : '';

    // ── Accordion toggle ──────────────────────────────
    document.querySelectorAll('.xat-section-head').forEach(function (head) {
        head.addEventListener('click', function () {
            head.closest('.xat-section').classList.toggle('open');
        });
    });

    // ── Mark single as read (event delegation) ────────
    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.xat-btn-read');
        if (!btn) return;

        const id = btn.dataset.id;
        if (!id) return;

        btn.disabled = true;
        btn.style.opacity = '0.6';

        try {
            const formData = new FormData();
            formData.append('id', id);
            formData.append('__RequestVerificationToken', getToken());

            const res = await fetch(oxuUrl, {
                method: 'POST',
                body: formData
            });

            if (res.ok) {
                const card = document.querySelector('.xat-item[data-id="' + id + '"]');
                if (card) {
                    card.classList.remove('xat-item--unread');
                    card.classList.add('xat-item--read');
                    btn.remove();
                }
                updateUnreadUI();
            } else {
                btn.disabled = false;
                btn.style.opacity = '';
                console.error('Oxu uğursuz:', res.status);
            }
        } catch (err) {
            btn.disabled = false;
            btn.style.opacity = '';
            console.error('Oxu xəta:', err);
        }
    });

    // ── Mark all as read ──────────────────────────────
    const markAllBtn = document.getElementById('markAllBtn');
    if (markAllBtn) {
        markAllBtn.addEventListener('click', async function () {
            markAllBtn.disabled = true;
            try {
                const formData = new FormData();
                formData.append('__RequestVerificationToken', getToken());

                const res = await fetch(hamisiniOxuUrl, {
                    method: 'POST',
                    body: formData
                });

                if (res.ok) {
                    document.querySelectorAll('.xat-item--unread').forEach(function (card) {
                        card.classList.remove('xat-item--unread');
                        card.classList.add('xat-item--read');
                        const rb = card.querySelector('.xat-btn-read');
                        if (rb) rb.remove();
                    });
                    updateUnreadUI();
                } else {
                    console.error('HamisiniOxu uğursuz:', res.status);
                }
            } catch (err) {
                console.error('HamisiniOxu xəta:', err);
            } finally {
                markAllBtn.disabled = false;
            }
        });
    }

    // ── UI yenilə ─────────────────────────────────────
    function updateUnreadUI() {
        const remaining = document.querySelectorAll('.xat-item--unread').length;

        const badge = document.getElementById('xatUnreadBadge');
        if (badge) {
            if (remaining > 0) {
                badge.style.display = 'inline-flex';
                // badge içindəki mətn node-u tap və yenilə
                const textNodes = Array.from(badge.childNodes).filter(n => n.nodeType === 3);
                if (textNodes.length) textNodes[textNodes.length - 1].textContent = ' ' + remaining + ' oxunmamış';
            } else {
                badge.style.display = 'none';
            }
        }

        if (markAllBtn) {
            markAllBtn.style.display = remaining > 0 ? 'inline-flex' : 'none';
        }
    }

})();

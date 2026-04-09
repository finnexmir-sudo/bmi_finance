// wwwroot/js/User/Bildiris.js

// ── Token ──────────────────────────────────────────────
function getBildirisToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}

// ── Topbar badge yenilə ────────────────────────────────
async function checkNotifs() {
    try {
        const res = await fetch('/User/Inbox/OxunmamisSayi');
        const data = await res.json();
        const count = data.cemi ?? 0;

        const badge = document.getElementById('notifCount');
        if (badge) {
            badge.textContent = count > 99 ? '99+' : count;
            badge.style.display = count > 0 ? 'inline' : 'none';
        }

        const dot = document.getElementById('notifDot');
        if (dot) dot.style.display = count > 0 ? 'block' : 'none';

        const sidebarBadge = document.getElementById('sidebarNotifCount');
        if (sidebarBadge) {
            sidebarBadge.textContent = count > 0 ? (count > 99 ? '99+' : count) : '';
            sidebarBadge.style.display = count > 0 ? 'inline' : 'none';
        }
    } catch (e) { console.error('checkNotifs:', e); }
}

// ── Xatırlatma badge yenilə ────────────────────────────
async function checkXatirlatma() {
    try {
        const res = await fetch('/User/Xatirlatma/OxunmamisSayi');
        const data = await res.json();
        const say = data.say ?? 0;

        const badge = document.getElementById('xatirlatmaCount');
        if (badge) {
            badge.textContent = say > 99 ? '99+' : say;
            badge.style.display = say > 0 ? 'inline' : 'none';
        }

        const sidebarBadge = document.getElementById('sidebarXatCount');
        if (sidebarBadge) {
            sidebarBadge.textContent = say > 0 ? (say > 99 ? '99+' : say) : '';
            sidebarBadge.style.display = say > 0 ? 'inline' : 'none';
        }
    } catch (e) { console.error('checkXatirlatma:', e); }
}

// ── "Bax →" klik: oxundu et + returnUrl ilə yönləndir ─
document.addEventListener('click', function (e) {
    const link = e.target.closest('a.ibx-item-link[data-bildiris-id]');
    if (!link) return;

    const bildirisId = link.dataset.bildirisId;
    const href = link.getAttribute('href');
    if (!bildirisId || !href) return;

    e.preventDefault();

    // returnUrl = hazırki Inbox səhifəsi
    const returnUrl = window.location.pathname + window.location.search;

    // href-ə returnUrl əlavə et
    const separator = href.includes('?') ? '&' : '?';
    const targetUrl = href + separator + 'returnUrl=' + encodeURIComponent(returnUrl);

    const fd = new FormData();
    fd.append('id', bildirisId);
    fd.append('__RequestVerificationToken', getBildirisToken());

    fetch('/User/Inbox/BildirisOxu', { method: 'POST', body: fd })
        .catch(() => { })
        .finally(() => {
            window.location.href = targetUrl;
        });
});

// ── "Hamısını oxundu işarələ" ──────────────────────────
document.addEventListener('click', async function (e) {
    const btn = e.target.closest('#hamisiniOxuBtn');
    if (!btn) return;

    try {
        const fd = new FormData();
        fd.append('__RequestVerificationToken', getBildirisToken());
        const res = await fetch('/User/Inbox/HamisiniOxu', { method: 'POST', body: fd });

        if (res.ok) {
            document.querySelectorAll('.ibx-item--unread').forEach(function (el) {
                el.classList.remove('ibx-item--unread');
                const dot = el.querySelector('.ibx-unread-dot');
                if (dot) dot.remove();
            });

            const tabDot = document.querySelector('[data-tab="bildirisler"] .ibx-tab-dot');
            if (tabDot) tabDot.remove();

            btn.closest('.ibx-panel-actions')?.remove();

            await checkNotifs();
        }
    } catch (e) { console.error('hamisiniOxu:', e); }
});

// ── Başlanğıcda + hər 30 san ──────────────────────────
checkNotifs();
checkXatirlatma();
setInterval(checkNotifs, 30000);
setInterval(checkXatirlatma, 30000);
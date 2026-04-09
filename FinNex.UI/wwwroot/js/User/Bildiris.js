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

// ══════════════════════════════════════════════════════
// WEB PUSH NOTIFICATION - Service Worker + Subscription
// ══════════════════════════════════════════════════════

(function initPushNotifications() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return;

    navigator.serviceWorker.register('/sw.js')
        .then(function (reg) {
            // VAPID public key-i al
            return fetch('/User/Inbox/VapidPublicKey')
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (!data.key) return;

                    // Artıq abonə olubmu?
                    return reg.pushManager.getSubscription().then(function (sub) {
                        if (sub) return; // artıq abonədir

                        // İcazə sor və abonə ol
                        return reg.pushManager.subscribe({
                            userVisibleOnly: true,
                            applicationServerKey: urlBase64ToUint8Array(data.key)
                        }).then(function (subscription) {
                            // Subscription-u serverə göndər
                            var key = subscription.getKey('p256dh');
                            var auth = subscription.getKey('auth');

                            return fetch('/User/Inbox/PushAboneOl', {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify({
                                    endpoint: subscription.endpoint,
                                    p256dh: btoa(String.fromCharCode.apply(null, new Uint8Array(key))),
                                    auth: btoa(String.fromCharCode.apply(null, new Uint8Array(auth)))
                                })
                            });
                        });
                    });
                });
        })
        .catch(function (err) {
            console.log('Push notification qeydiyyatı uğursuz:', err);
        });

    function urlBase64ToUint8Array(base64String) {
        var padding = '='.repeat((4 - base64String.length % 4) % 4);
        var base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        var rawData = atob(base64);
        var outputArray = new Uint8Array(rawData.length);
        for (var i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }
})();
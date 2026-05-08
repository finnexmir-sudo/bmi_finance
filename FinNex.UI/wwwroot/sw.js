// BMI Finance — Minimal Service Worker (PWA install üçün)
// Keşləmə yoxdur — hər zaman serverdən yenilə (beləliklə dəyişikliklər dərhal görünür)

const CACHE_VERSION = 'bmi-finance-v1';

self.addEventListener('install', (event) => {
    // Dərhal aktivləş
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    // Köhnə keşləri sil
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(keys.map((k) => caches.delete(k)))
        ).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    // Network-first — hər zaman serverdən götür, offline olanda fallback
    event.respondWith(
        fetch(event.request).catch(() =>
            caches.match(event.request).then(r => r ?? new Response('', { status: 503, statusText: 'Offline' }))
        )
    );
});

// FinNex Push Notification Service Worker

self.addEventListener('push', function (event) {
    var data = { bashliq: 'FinNex', metn: 'Yeni bildiriş', url: '/' };

    if (event.data) {
        try {
            data = event.data.json();
        } catch (e) {
            data.metn = event.data.text();
        }
    }

    var options = {
        body: data.metn || data.body || '',
        icon: '/images/Logobank.jpg',
        badge: '/images/Logobank.jpg',
        data: { url: data.url || '/' },
        vibrate: [100, 50, 100],
        tag: 'finnex-' + Date.now()
    };

    event.waitUntil(
        self.registration.showNotification(data.bashliq || data.title || 'FinNex', options)
    );
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();

    var url = event.notification.data && event.notification.data.url
        ? event.notification.data.url
        : '/';

    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
            // Əgər artıq açıq tab varsa, ona fokuslan
            for (var i = 0; i < clientList.length; i++) {
                var client = clientList[i];
                if (client.url.includes(self.location.origin) && 'focus' in client) {
                    client.navigate(url);
                    return client.focus();
                }
            }
            // Yoxdursa yeni tab aç
            return clients.openWindow(url);
        })
    );
});

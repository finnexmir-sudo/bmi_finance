/* =============================================
   FinNex — ADMS Cihaz Test Səhifəsi | JS
   ============================================= */

(function () {
    'use strict';

    const STATUS_MAP = {
        1: '<span class="adms-status-chip chip-isde">İşdə</span>',
        2: '<span class="adms-status-chip chip-gecikme">Gecikmiş</span>',
        3: '<span class="adms-status-chip chip-qayib">Qayib</span>',
        4: '<span class="adms-status-chip chip-icazeli">İcazəli</span>',
    };

    // ── DOM refs ──────────────────────────────
    const pulseRing   = document.getElementById('pulseRing');
    const statusIcon  = document.getElementById('statusIcon');
    const statusLabel = document.getElementById('statusLabel');
    const statusBadge = document.getElementById('statusBadge');
    const badgeText   = document.getElementById('statusBadgeText');
    const lastContact = document.getElementById('lastContact');
    const todayCount  = document.getElementById('todayCount');
    const logTbody    = document.getElementById('logTbody');
    const logCount    = document.getElementById('logCount');
    const lastUpdate  = document.getElementById('lastUpdate');
    const refreshBtn  = document.getElementById('refreshBtn');

    // ── Main loader ───────────────────────────
    function loadData() {
        refreshBtn.classList.add('spinning');

        fetch('/HR/ADMSTest/GetRecentLogs')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                renderStatus(data.isOnline, data.lastContact, data.todayCount);
                renderTable(data.logs);
                lastUpdate.textContent = new Date().toLocaleTimeString('az-AZ');
            })
            .catch(function () {
                renderStatus(false, '—', 0);
                renderTable([]);
            })
            .finally(function () {
                refreshBtn.classList.remove('spinning');
            });
    }

    // ── Status panel ─────────────────────────
    function renderStatus(isOnline, contact, count) {
        if (isOnline) {
            statusIcon.classList.remove('offline');
            pulseRing.classList.remove('offline');
            statusBadge.className = 'adms-badge online';
            statusLabel.textContent = 'Cihaz Aktiv';
            badgeText.textContent = 'Online';
        } else {
            statusIcon.classList.add('offline');
            pulseRing.classList.add('offline');
            statusBadge.className = 'adms-badge offline';
            statusLabel.textContent = 'Əlaqə Yoxdur';
            badgeText.textContent = 'Offline';
        }

        lastContact.textContent = contact || '—';
        todayCount.textContent  = (count ?? 0) + ' qeyd';
    }

    // ── Log table ─────────────────────────────
    function renderTable(logs) {
        if (!logs || logs.length === 0) {
            logTbody.innerHTML =
                '<tr><td colspan="6">' +
                '<div class="adms-empty">' +
                '<svg width="32" height="32" viewBox="0 0 24 24" fill="none">' +
                '<circle cx="12" cy="12" r="9" stroke="#ccc" stroke-width="1.5"/>' +
                '<path d="M12 7v5l3 3" stroke="#ccc" stroke-width="1.5" stroke-linecap="round"/>' +
                '</svg>Hələ heç bir qeyd yoxdur</div></td></tr>';
            logCount.textContent = '0 qeyd';
            return;
        }

        logCount.textContent = logs.length + ' qeyd';

        logTbody.innerHTML = logs.map(function (l) {
            var girisHtml = l.girisVaxti
                ? '<span class="adms-type-badge giris">▲ ' + l.girisVaxti + '</span>'
                : '<span style="color:#ccc">—</span>';

            var cixisHtml = l.cixisVaxti
                ? '<span class="adms-type-badge cixis">▼ ' + l.cixisVaxti + '</span>'
                : '<span style="color:#ccc">—</span>';

            return '<tr>' +
                '<td class="pin-cell">#' + l.isciId + '</td>' +
                '<td>' + (l.isciAd || '—') + '</td>' +
                '<td>' + l.tarix + '</td>' +
                '<td>' + girisHtml + '</td>' +
                '<td>' + cixisHtml + '</td>' +
                '<td>' + (STATUS_MAP[l.status] || '—') + '</td>' +
                '</tr>';
        }).join('');
    }

    // ── Init ──────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        loadData();
        setInterval(loadData, 30000); // hər 30 saniyə
    });

    // Düyməyə klik
    refreshBtn.addEventListener('click', loadData);

    // Global-a aç (lazım olarsa Razor-dan çağırıla bilər)
    window.admsLoadData = loadData;

})();
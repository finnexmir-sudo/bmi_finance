/* =============================================
   FinNex — Telim JS
   ============================================= */

(function () {
    'use strict';

    // Tab switching
    document.querySelectorAll('.tlm-tab').forEach(function (tab) {
        tab.addEventListener('click', function () {
            var target = this.getAttribute('data-tab');

            document.querySelectorAll('.tlm-tab').forEach(function (t) { t.classList.remove('active'); });
            document.querySelectorAll('.tlm-tab-content').forEach(function (c) { c.classList.remove('active'); });

            this.classList.add('active');
            var content = document.getElementById('tab-' + target);
            if (content) content.classList.add('active');
        });
    });

    // Load training data via AJAX
    if (typeof telimDataUrl !== 'undefined') {
        loadTelimData();
    }

    function loadTelimData() {
        fetch(telimDataUrl)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                renderTable(data);
            })
            .catch(function () {
                document.getElementById('tlmTableBody').innerHTML =
                    '<tr><td colspan="10" class="tlm-loading">Məlumat yüklənə bilmədi</td></tr>';
            });
    }

    function renderTable(data) {
        var tbody = document.getElementById('tlmTableBody');
        var countEl = document.getElementById('tlmResultCount');

        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="10" class="tlm-loading">Təlim tapılmadı</td></tr>';
            countEl.textContent = '0';
            return;
        }

        countEl.textContent = data.length;
        var html = '';

        data.forEach(function (item) {
            var badgeClass = statusBadge(item.status);
            var novText = item.daxiliTelimdir ? 'Daxili' : 'Xarici';

            html += '<tr>';
            html += '<td class="left"><strong>' + escHtml(item.ad) + '</strong></td>';
            html += '<td>' + escHtml(item.tesviqci) + '</td>';
            html += '<td>' + escHtml(item.mekan) + '</td>';
            html += '<td>' + escHtml(item.baslamaTarixi) + '</td>';
            html += '<td>' + escHtml(item.bitmeTarixi) + '</td>';
            html += '<td>' + item.muddetSaat + ' saat</td>';
            html += '<td><span class="tlm-badge ' + (item.daxiliTelimdir ? 'tlm-badge--progress' : 'tlm-badge--wait') + '">' + novText + '</span></td>';
            html += '<td><span class="tlm-badge ' + badgeClass + '">' + escHtml(item.statusAd) + '</span></td>';
            html += '<td>' + item.ishtirakciSayi + '</td>';
            html += '<td class="center">';
            html += '<a href="' + telimDetalUrl + '/' + item.id + '" class="tlm-act-btn" title="Detal">';
            html += '<svg width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M1.5 6h9M7 3l3 3-3 3" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>';
            html += '</a>';
            html += '</td>';
            html += '</tr>';
        });

        tbody.innerHTML = html;
    }

    function statusBadge(status) {
        switch (status) {
            case 0: return 'tlm-badge--wait';
            case 1: return 'tlm-badge--progress';
            case 2: return 'tlm-badge--done';
            case 3: return 'tlm-badge--cancelled';
            default: return 'tlm-badge--wait';
        }
    }

    function escHtml(str) {
        if (!str) return '\u2014';
        var d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

})();

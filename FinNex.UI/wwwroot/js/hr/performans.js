/* =============================================
   FinNex — Performans JS
   ============================================= */

(function () {
    'use strict';

    // Auto-filter on select change
    document.querySelectorAll('.prf-auto-filter').forEach(function (el) {
        el.addEventListener('change', function () {
            document.getElementById('perfFilterForm').submit();
        });
    });

    // Load data via AJAX
    if (typeof perfDataUrl !== 'undefined') {
        loadPerformansData();
    }

    function loadPerformansData() {
        fetch(perfDataUrl)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                renderTable(data);
                renderStats(data);
            })
            .catch(function () {
                document.getElementById('prfTableBody').innerHTML =
                    '<tr><td colspan="9" class="prf-loading">Məlumat yüklənə bilmədi</td></tr>';
            });
    }

    function renderTable(data) {
        var tbody = document.getElementById('prfTableBody');
        var countEl = document.getElementById('prfResultCount');

        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="prf-loading">Qiymətləndirmə tapılmadı</td></tr>';
            countEl.textContent = '0';
            return;
        }

        countEl.textContent = data.length;
        var html = '';

        data.forEach(function (item) {
            var badgeClass = statusBadge(item.status);
            var dovrText = item.rubu ? item.dovrTipi + ' ' + item.rubu : item.dovrTipi;

            html += '<tr>';
            html += '<td class="left"><strong>' + escHtml(item.isciAd) + '</strong></td>';
            html += '<td class="left">' + escHtml(item.departament) + '</td>';
            html += '<td>' + escHtml(item.dovrTipi) + '</td>';
            html += '<td>' + item.il + (item.rubu ? ' / R' + item.rubu : '') + '</td>';
            html += '<td><span class="prf-badge ' + badgeClass + '">' + escHtml(item.statusAd) + '</span></td>';
            html += '<td>' + fmtQ(item.isciOrtalamaQiymet) + '</td>';
            html += '<td>' + fmtQ(item.mudirOrtalamaQiymet) + '</td>';
            html += '<td><strong>' + fmtQ(item.yekunQiymet) + '</strong></td>';
            html += '<td class="center">';
            html += '<a href="/HR/Performans/Detal/' + item.id + '" class="prf-act-btn" title="Detal">';
            html += '<svg width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M1.5 6h9M7 3l3 3-3 3" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>';
            html += '</a>';
            html += '</td>';
            html += '</tr>';
        });

        tbody.innerHTML = html;
    }

    function renderStats(data) {
        if (!data) return;

        document.getElementById('statUmumi').textContent = data.length;
        document.getElementById('statGozlemede').textContent =
            data.filter(function (x) { return x.status === 0; }).length;
        document.getElementById('statDavam').textContent =
            data.filter(function (x) { return x.status === 1 || x.status === 2; }).length;
        document.getElementById('statTamamlandi').textContent =
            data.filter(function (x) { return x.status === 3; }).length;

        var tamamlanan = data.filter(function (x) { return x.yekunQiymet > 0; });
        var orta = tamamlanan.length > 0
            ? (tamamlanan.reduce(function (s, x) { return s + x.yekunQiymet; }, 0) / tamamlanan.length).toFixed(2)
            : '0';
        document.getElementById('statOrtaQiymet').textContent = orta;
    }

    function statusBadge(status) {
        switch (status) {
            case 0: return 'prf-badge--wait';
            case 1: return 'prf-badge--progress';
            case 2: return 'prf-badge--review';
            case 3: return 'prf-badge--done';
            default: return 'prf-badge--wait';
        }
    }

    function fmtQ(val) {
        if (!val || val <= 0) return '<span class="prf-q--empty">—</span>';
        var cls = val >= 4 ? 'prf-q--high' : val >= 3 ? 'prf-q--mid' : 'prf-q--low';
        return '<span class="prf-q ' + cls + '">' + val.toFixed(2) + '</span>';
    }

    function escHtml(str) {
        if (!str) return '—';
        var d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

})();

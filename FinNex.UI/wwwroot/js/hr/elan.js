// ── HR Elan JS ───────────────────────────────────────────

(function () {
    'use strict';

    function escHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // ── Load Data ───────────────────────────────────────
    function loadData() {
        var body = document.getElementById('elanBody');
        body.innerHTML = '<tr><td colspan="7" class="elan-empty">Yuklenir...</td></tr>';

        fetch('/HR/Elan/GetData')
            .then(function (r) { return r.json(); })
            .then(function (data) { renderTable(data.elanlar); })
            .catch(function () {
                body.innerHTML = '<tr><td colspan="7" class="elan-empty">Xeta bash verdi</td></tr>';
            });
    }

    // ── Render Table ────────────────────────────────────
    function renderTable(elanlar) {
        var body = document.getElementById('elanBody');

        if (!elanlar || elanlar.length === 0) {
            body.innerHTML = '<tr><td colspan="7" class="elan-empty">Elan tapilmadi</td></tr>';
            return;
        }

        var html = '';
        elanlar.forEach(function (e) {
            html += '<tr>';
            html += '<td><strong>' + escHtml(e.bashliq) + '</strong></td>';
            html += '<td>' + escHtml(e.gonderenAd) + '</td>';
            html += '<td>' + (e.vacibdir
                ? '<span class="elan-badge elan-badge--vacib">Vacib</span>'
                : '<span class="elan-badge elan-badge--normal">Normal</span>') + '</td>';
            html += '<td>' + (e.bitirmeTarixi || 'Muddetsiz') + '</td>';
            html += '<td>' + (e.aktivdir
                ? '<span class="elan-badge elan-badge--aktiv">Aktiv</span>'
                : '<span class="elan-badge elan-badge--deaktiv">Deaktiv</span>') + '</td>';
            html += '<td>' + e.yaradilma + '</td>';
            html += '<td><div class="elan-action-btns">';
            if (e.aktivdir) {
                html += '<button class="fn-btn fn-btn--danger" onclick="silElan(' + e.id + ')">Deaktiv et</button>';
            }
            html += '</div></td>';
            html += '</tr>';
        });

        body.innerHTML = html;
    }

    // ── Sil (Deaktiv) ───────────────────────────────────
    window.silElan = function (id) {
        if (!confirm('Bu elani deaktiv etmek isteyirsiniz?')) return;

        fetch('/HR/Elan/Sil?id=' + id, { method: 'POST' })
            .then(function (r) {
                if (!r.ok) throw new Error();
                return r.json();
            })
            .then(function () { loadData(); })
            .catch(function () { alert('Xeta bash verdi.'); });
    };

    // Init
    loadData();

})();

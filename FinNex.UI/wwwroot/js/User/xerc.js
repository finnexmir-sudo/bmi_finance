// ── User Xerc JS ─────────────────────────────────────────

(function () {
    'use strict';

    function fmt(val) {
        return Number(val).toLocaleString('az-AZ', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function badgeClass(status) {
        switch (status) {
            case 0: return 'xerc-badge--muraciet';
            case 1: return 'xerc-badge--sobe';
            case 2: return 'xerc-badge--hr';
            case 3: return 'xerc-badge--odenildi';
            case 4: return 'xerc-badge--imtina';
            default: return '';
        }
    }

    function loadXercler() {
        var list = document.getElementById('xercList');
        if (!list) return;

        list.innerHTML = '<div class="xerc-loading">Yuklenir...</div>';

        fetch('/User/Xerc/GetMyXercler')
            .then(r => r.json())
            .then(data => {
                renderList(data.xercler);
                renderStats(data.xercler);
            })
            .catch(() => {
                list.innerHTML = '<div class="xerc-empty-state">Xeta bash verdi</div>';
            });
    }

    function renderList(xercler) {
        var list = document.getElementById('xercList');

        if (!xercler || xercler.length === 0) {
            list.innerHTML = '<div class="xerc-empty-state">Hele xerc muracietiniz yoxdur</div>';
            return;
        }

        var html = '';
        xercler.forEach(function (x) {
            html += '<div class="xerc-item">';
            html += '<div class="xerc-item-icon">' + (x.kateqoriyaIkon || '&#128176;') + '</div>';
            html += '<div class="xerc-item-info">';
            html += '<div class="xerc-item-title">' + escHtml(x.kateqoriya) + '</div>';
            html += '<div class="xerc-item-meta">' + escHtml(x.tesvir) + ' &middot; ' + x.xercTarixi + '</div>';
            if (x.status === 4 && x.imtinaSebebi) {
                html += '<div class="xerc-imtina-sebeb">Sebeb: ' + escHtml(x.imtinaSebebi) + '</div>';
            }
            if (x.qebzFaylYolu) {
                html += '<a href="' + x.qebzFaylYolu + '" target="_blank" class="xerc-qebz-link">Qebze bax</a>';
            }
            html += '</div>';
            html += '<div class="xerc-item-right">';
            html += '<div class="xerc-item-amount">' + fmt(x.mebleg) + ' AZN</div>';
            html += '<span class="xerc-badge ' + badgeClass(x.status) + '">' + escHtml(x.statusAd) + '</span>';
            html += '</div>';
            html += '</div>';
        });

        list.innerHTML = html;
    }

    function renderStats(xercler) {
        if (!xercler || xercler.length === 0) return;

        var stats = document.getElementById('xercStats');
        stats.style.display = 'flex';

        document.getElementById('statCemi').textContent = xercler.length;
        document.getElementById('statMuraciet').textContent = xercler.filter(x => x.status <= 2).length;
        document.getElementById('statOdenildi').textContent = xercler.filter(x => x.status === 3).length;
        document.getElementById('statImtina').textContent = xercler.filter(x => x.status === 4).length;
    }

    function escHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // Init
    loadXercler();

})();

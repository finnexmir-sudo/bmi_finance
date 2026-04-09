// ── HR Xerc JS ───────────────────────────────────────────

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

    function escHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    // ── Load Data ───────────────────────────────────────
    function loadData() {
        var status = document.getElementById('filterStatus').value;
        var axtaris = document.getElementById('filterAxtaris').value;
        var body = document.getElementById('xercBody');
        body.innerHTML = '<tr><td colspan="7" class="xerc-empty">Yuklenir...</td></tr>';

        var url = '/HR/Xerc/GetData?status=' + status;
        if (axtaris) url += '&axtaris=' + encodeURIComponent(axtaris);

        fetch(url)
            .then(r => r.json())
            .then(data => renderTable(data.xercler))
            .catch(() => {
                body.innerHTML = '<tr><td colspan="7" class="xerc-empty">Xeta bash verdi</td></tr>';
            });
    }

    // ── Render Table ────────────────────────────────────
    function renderTable(xercler) {
        var body = document.getElementById('xercBody');

        if (!xercler || xercler.length === 0) {
            body.innerHTML = '<tr><td colspan="7" class="xerc-empty">Xerc tapilmadi</td></tr>';
            return;
        }

        var html = '';
        xercler.forEach(function (x) {
            html += '<tr>';
            html += '<td>' + escHtml(x.isciAd) + '</td>';
            html += '<td>' + escHtml(x.kateqoriya) + '</td>';
            html += '<td>' + escHtml(x.tesvir) + '</td>';
            html += '<td class="xerc-amount">' + fmt(x.mebleg) + ' AZN</td>';
            html += '<td>' + x.xercTarixi + '</td>';
            html += '<td><span class="xerc-badge ' + badgeClass(x.status) + '">' + escHtml(x.statusAd) + '</span></td>';

            html += '<td><div class="xerc-action-btns">';

            // Tesdiqle button: for Muraciet or SobeReisiTesdiq
            if (x.status === 0 || x.status === 1) {
                html += '<button class="fn-btn fn-btn--success" onclick="tesdiqle(' + x.id + ')">Tesdiqle</button>';
                html += '<button class="fn-btn fn-btn--danger" onclick="openImtina(' + x.id + ')">Imtina</button>';
            }

            // Ode button: for HrTesdiq
            if (x.status === 2) {
                html += '<button class="fn-btn fn-btn--info" onclick="ode(' + x.id + ')">Ode</button>';
            }

            // Qebz link
            if (x.qebzFaylYolu) {
                html += '<a href="' + x.qebzFaylYolu + '" target="_blank" class="xerc-qebz-link">Qebz</a>';
            }

            html += '</div></td>';
            html += '</tr>';
        });

        body.innerHTML = html;
    }

    // ── Tesdiqle ────────────────────────────────────────
    window.tesdiqle = function (id) {
        if (!confirm('Bu xerci tesdiqlemek isteyirsiniz?')) return;

        fetch('/HR/Xerc/Tesdiqle?id=' + id, { method: 'POST' })
            .then(r => {
                if (!r.ok) throw new Error();
                return r.json();
            })
            .then(() => loadData())
            .catch(() => alert('Xeta bash verdi.'));
    };

    // ── Ode ─────────────────────────────────────────────
    window.ode = function (id) {
        if (!confirm('Bu xerci odenildi kimi isharelenmek isteyirsiniz?')) return;

        fetch('/HR/Xerc/Ode?id=' + id, { method: 'POST' })
            .then(r => {
                if (!r.ok) throw new Error();
                return r.json();
            })
            .then(() => loadData())
            .catch(() => alert('Xeta bash verdi.'));
    };

    // ── Imtina Modal ────────────────────────────────────
    window.openImtina = function (id) {
        document.getElementById('imtinaXercId').value = id;
        document.getElementById('imtinaSebeb').value = '';
        document.getElementById('imtinaModal').style.display = 'flex';
    };

    function closeImtinaModal() {
        document.getElementById('imtinaModal').style.display = 'none';
    }

    function confirmImtina() {
        var id = document.getElementById('imtinaXercId').value;
        var sebeb = document.getElementById('imtinaSebeb').value;

        if (!sebeb.trim()) {
            alert('Imtina sebebi daxil edin.');
            return;
        }

        fetch('/HR/Xerc/Imtina?id=' + id, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ sebeb: sebeb })
        })
            .then(r => {
                if (!r.ok) throw new Error();
                return r.json();
            })
            .then(() => {
                closeImtinaModal();
                loadData();
            })
            .catch(() => alert('Xeta bash verdi.'));
    }

    // ── Excel ───────────────────────────────────────────
    function exportExcel() {
        var status = document.getElementById('filterStatus').value;
        window.location.href = '/HR/Xerc/ExportExcel?status=' + status;
    }

    // ── Events ──────────────────────────────────────────
    document.getElementById('btnFilter').addEventListener('click', loadData);
    document.getElementById('btnExcel').addEventListener('click', exportExcel);
    document.getElementById('btnImtinaClose').addEventListener('click', closeImtinaModal);
    document.getElementById('btnImtinaCancel').addEventListener('click', closeImtinaModal);
    document.getElementById('btnImtinaConfirm').addEventListener('click', confirmImtina);

    document.getElementById('imtinaModal').addEventListener('click', function (e) {
        if (e.target === this) closeImtinaModal();
    });

    document.getElementById('filterAxtaris').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') loadData();
    });

    // Init
    loadData();

})();

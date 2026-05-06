// ── HR Xərc İdarəetməsi JS ───────────────────────────────

(function () {
    'use strict';

    function fmt(val) {
        var n = Number(val);
        var dec = n % 1 === 0 ? 0 : 2;
        return n.toLocaleString('az-AZ', { minimumFractionDigits: dec, maximumFractionDigits: dec });
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

    // ── Məlumatları yüklə ────────────────────────────────
    function loadData() {
        var status = document.getElementById('filterStatus').value;
        var axtaris = document.getElementById('filterAxtaris').value;
        var body = document.getElementById('xercBody');
        body.innerHTML = '<tr><td colspan="7" class="xerc-empty">Yüklənir...</td></tr>';

        var url = '/HR/Xerc/GetData?status=' + status;
        if (axtaris) url += '&axtaris=' + encodeURIComponent(axtaris);

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) { renderTable(data.xercler); })
            .catch(function () {
                body.innerHTML = '<tr><td colspan="7" class="xerc-empty">Xəta baş verdi</td></tr>';
            });
    }

    // ── Cədvəli render et ───────────────────────────────
    function renderTable(xercler) {
        var body = document.getElementById('xercBody');

        if (!xercler || xercler.length === 0) {
            body.innerHTML = '<tr><td colspan="7" class="xerc-empty">Xərc tapılmadı</td></tr>';
            return;
        }

        var html = '';
        xercler.forEach(function (x) {
            html += '<tr>';

            // İşçi / Şöbə sütunu
            if (x.manualGiris) {
                var dept = x.departamentAd ? escHtml(x.departamentAd) : '—';
                html += '<td><span class="xerc-badge xerc-badge--manual">Manual</span> ' + dept + '</td>';
            } else {
                html += '<td>' + escHtml(x.isciAd || '—') + '</td>';
            }

            html += '<td>' + escHtml(x.kateqoriya) + '</td>';
            html += '<td class="xerc-tesvir">' + escHtml(x.tesvir) + '</td>';
            html += '<td class="xerc-amount">' + fmt(x.mebleg) + ' ₼</td>';
            html += '<td>' + x.xercTarixi + '</td>';
            html += '<td><span class="xerc-badge ' + badgeClass(x.status) + '">' + escHtml(x.statusAd) + '</span></td>';

            html += '<td><div class="xerc-action-btns">';

            if (x.status === 0 || x.status === 1) {
                html += '<button class="fn-btn fn-btn--success" onclick="xercTesdiqle(' + x.id + ')">Təsdiqlə</button>';
                html += '<button class="fn-btn fn-btn--danger" onclick="xercOpenImtina(' + x.id + ')">İmtina</button>';
            }

            // Ödə düyməsini yalnız canOde = true olduqda göstər (Muhasib/Admin)
            if (x.status === 2 && x.canOde) {
                html += '<button class="fn-btn fn-btn--info" onclick="xercOde(' + x.id + ')">Ödə</button>';
            }

            if (x.qebzFaylYolu) {
                html += '<a href="' + escHtml(x.qebzFaylYolu) + '" target="_blank" class="xerc-qebz-link">' +
                        '<i class="bi bi-paperclip"></i> Sənəd</a>';
            }

            html += '</div></td>';
            html += '</tr>';
        });

        body.innerHTML = html;
    }

    // ── Təsdiqlə ────────────────────────────────────────
    window.xercTesdiqle = function (id) {
        if (!confirm('Bu xərci təsdiqləmək istəyirsiniz?')) return;
        fetch('/HR/Xerc/Tesdiqle?id=' + id, { method: 'POST' })
            .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
            .then(function () { loadData(); })
            .catch(function () { alert('Xəta baş verdi.'); });
    };

    // ── Ödə ─────────────────────────────────────────────
    window.xercOde = function (id) {
        if (!confirm('Bu xərci ödənildi kimi işarələmək istəyirsiniz?')) return;
        fetch('/HR/Xerc/Ode?id=' + id, { method: 'POST' })
            .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
            .then(function () { loadData(); })
            .catch(function () { alert('Xəta baş verdi.'); });
    };

    // ── İmtina Modal ────────────────────────────────────
    window.xercOpenImtina = function (id) {
        document.getElementById('imtinaXercId').value = id;
        document.getElementById('imtinaSebeb').value = '';
        document.getElementById('imtinaModal').style.display = 'flex';
    };

    function closeImtinaModal() {
        document.getElementById('imtinaModal').style.display = 'none';
    }

    function confirmImtina() {
        var id = document.getElementById('imtinaXercId').value;
        var sebeb = document.getElementById('imtinaSebeb').value.trim();
        if (!sebeb) { alert('İmtina səbəbi daxil edin.'); return; }

        var csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        fetch('/HR/Xerc/Imtina?id=' + id, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': csrfToken
            },
            body: JSON.stringify({ sebeb: sebeb })
        })
            .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
            .then(function () { closeImtinaModal(); loadData(); })
            .catch(function () { alert('Xəta baş verdi.'); });
    }

    // ── Excel ────────────────────────────────────────────
    function exportExcel() {
        var status = document.getElementById('filterStatus').value;
        window.location.href = '/HR/Xerc/ExportExcel?status=' + status;
    }

    // ── Hadisələr ────────────────────────────────────────
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

    loadData();

})();

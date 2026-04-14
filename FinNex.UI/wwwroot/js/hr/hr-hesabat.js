// ── HR Hesabat JS ────────────────────────────────────────

(function () {
    'use strict';

    // ── Tab switching ─────────────────────────────────────
    const tabs = document.querySelectorAll('.rp-tab');
    const panels = document.querySelectorAll('.rp-panel');

    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            tabs.forEach(t => t.classList.remove('rp-tab--active'));
            panels.forEach(p => p.classList.remove('rp-panel--active'));

            tab.classList.add('rp-tab--active');
            const target = document.getElementById('panel-' + tab.dataset.tab);
            if (target) target.classList.add('rp-panel--active');
        });
    });

    // ── Helpers ───────────────────────────────────────────
    function formatMoney(val) {
        return Number(val).toLocaleString('az-AZ', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function showLoading(container) {
        container.innerHTML =
            '<div class="rp-loading"><i class="bi bi-arrow-repeat"></i>Yüklənir...</div>';
    }

    function showError(container, msg) {
        container.innerHTML =
            '<div class="rp-no-data"><i class="bi bi-exclamation-triangle"></i> ' + (msg || 'Xəta baş verdi') + '</div>';
    }

    function statusBadge(status) {
        const map = {
            'Layihe': 'layihe',
            'Tesdiqlendi': 'tesdiqlendi',
            'Odenildi': 'odenildi',
            'LegvEdildi': 'legv'
        };
        const cls = map[status] || 'layihe';
        const labels = {
            'Layihe': 'Layihə',
            'Tesdiqlendi': 'Təsdiqləndi',
            'Odenildi': 'Ödənildi',
            'LegvEdildi': 'Ləğv edildi'
        };
        return '<span class="rp-badge rp-badge--' + cls + '">' + (labels[status] || status) + '</span>';
    }

    // ── PDF export via window.print ──────────────────────
    function printTable(wrapId, title) {
        var wrap = document.getElementById(wrapId);
        if (!wrap) return;
        var tableHtml = wrap.innerHTML;
        var printWin = window.open('', '_blank', 'width=900,height=700');
        printWin.document.write('<!DOCTYPE html><html><head><title>' + title + '</title>');
        printWin.document.write('<style>');
        printWin.document.write('body{font-family:"Plus Jakarta Sans",sans-serif;padding:20px;color:#334155;}');
        printWin.document.write('h2{font-size:18px;margin-bottom:16px;color:#1a1d21;}');
        printWin.document.write('table{width:100%;border-collapse:collapse;font-size:12px;}');
        printWin.document.write('th{padding:8px 10px;background:#1e2a3b;color:#fff;text-align:left;font-weight:600;}');
        printWin.document.write('th.rp-th-right{text-align:right;}th.rp-th-center{text-align:center;}');
        printWin.document.write('td{padding:7px 10px;border-bottom:1px solid #e5e7eb;}');
        printWin.document.write('td.rp-td-right{text-align:right;}td.rp-td-center{text-align:center;}');
        printWin.document.write('.rp-dept-header td{background:#eef2ff;font-weight:700;color:#4338ca;}');
        printWin.document.write('.rp-dept-total td{background:#f5f3ff;font-weight:600;color:#5b21b6;}');
        printWin.document.write('.rp-grand-total td{background:#f0f0ff;font-weight:700;color:#1e1b4b;border-top:2px solid #667eea;}');
        printWin.document.write('.rp-badge{padding:2px 8px;border-radius:10px;font-size:10px;}');
        printWin.document.write('.rp-balans-val{display:inline-block;min-width:24px;text-align:center;}');
        printWin.document.write('.rp-balans-sep{color:#cbd5e1;margin:0 2px;}');
        printWin.document.write('.rp-balans-qaliq--low{color:#dc2626;font-weight:700;}');
        printWin.document.write('.rp-balans-qaliq--ok{color:#16a34a;font-weight:700;}');
        printWin.document.write('</style></head><body>');
        printWin.document.write('<h2>' + title + '</h2>');
        printWin.document.write(tableHtml);
        printWin.document.write('</body></html>');
        printWin.document.close();
        printWin.focus();
        setTimeout(function () { printWin.print(); }, 300);
    }

    // ── Maaş Hesabatı ────────────────────────────────────
    document.getElementById('btnMaasGoster').addEventListener('click', loadMaasData);

    document.getElementById('btnMaasExcel').addEventListener('click', function () {
        var il = document.getElementById('maasIl').value;
        var ay = document.getElementById('maasAy').value;
        window.location.href = '/HR/Hesabat/ExportMaasExcel?il=' + il + '&ay=' + ay;
    });

    document.getElementById('btnMaasPdf').addEventListener('click', function () {
        printTable('maasTableWrap', 'Maaş Hesabatı');
    });

    function loadMaasData() {
        const il = document.getElementById('maasIl').value;
        const ay = document.getElementById('maasAy').value;
        const wrap = document.getElementById('maasTableWrap');

        showLoading(wrap);

        fetch('/HR/Hesabat/GetMaasData?il=' + il + '&ay=' + ay)
            .then(r => r.json())
            .then(data => renderMaasTable(wrap, data))
            .catch(() => showError(wrap));
    }

    function renderMaasTable(wrap, data) {
        if (!data.departamentlar || data.departamentlar.length === 0) {
            wrap.innerHTML = '<div class="rp-no-data">Bu dövr üçün maaş məlumatı tapılmadı.</div>';
            return;
        }

        let html = '<table class="rp-table">';
        html += '<thead><tr>';
        html += '<th>İşçi</th>';
        html += '<th class="rp-th-right">Gross (AZN)</th>';
        html += '<th class="rp-th-right">Net (AZN)</th>';
        html += '<th class="rp-th-center">Status</th>';
        html += '</tr></thead><tbody>';

        data.departamentlar.forEach(dept => {
            // Department header
            html += '<tr class="rp-dept-header">';
            html += '<td colspan="4"><i class="bi bi-building"></i>' + dept.departament + '</td>';
            html += '</tr>';

            // Employee rows
            dept.isciler.forEach(isci => {
                html += '<tr>';
                html += '<td>' + isci.isciAdSoyad + '</td>';
                html += '<td class="rp-td-right">' + formatMoney(isci.brutMebleg) + '</td>';
                html += '<td class="rp-td-right">' + formatMoney(isci.netMebleg) + '</td>';
                html += '<td class="rp-td-center">' + statusBadge(isci.status) + '</td>';
                html += '</tr>';
            });

            // Department total
            html += '<tr class="rp-dept-total">';
            html += '<td>Cəmi: ' + dept.isciler.length + ' işçi</td>';
            html += '<td class="rp-td-right">' + formatMoney(dept.cemibrut) + '</td>';
            html += '<td class="rp-td-right">' + formatMoney(dept.ceminet) + '</td>';
            html += '<td></td>';
            html += '</tr>';
        });

        // Grand total
        html += '<tr class="rp-grand-total">';
        html += '<td>Ümumi</td>';
        html += '<td class="rp-td-right">' + formatMoney(data.umumibrut) + '</td>';
        html += '<td class="rp-td-right">' + formatMoney(data.umuminet) + '</td>';
        html += '<td></td>';
        html += '</tr>';

        html += '</tbody></table>';
        wrap.innerHTML = html;
    }

    // ── Davamiyyət Hesabatı ──────────────────────────────
    document.getElementById('btnDavamiyyetGoster').addEventListener('click', loadDavamiyyetData);

    document.getElementById('btnDavamiyyetExcel').addEventListener('click', function () {
        var il = document.getElementById('davamiyyetIl').value;
        var ay = document.getElementById('davamiyyetAy').value;
        window.location.href = '/HR/Hesabat/ExportDavamiyyetExcel?il=' + il + '&ay=' + ay;
    });

    document.getElementById('btnDavamiyyetPdf').addEventListener('click', function () {
        printTable('davamiyyetTableWrap', 'Davamiyyət Hesabatı');
    });

    function loadDavamiyyetData() {
        const il = document.getElementById('davamiyyetIl').value;
        const ay = document.getElementById('davamiyyetAy').value;
        const wrap = document.getElementById('davamiyyetTableWrap');

        showLoading(wrap);

        fetch('/HR/Hesabat/GetDavamiyyetData?il=' + il + '&ay=' + ay)
            .then(r => r.json())
            .then(data => renderDavamiyyetTable(wrap, data))
            .catch(() => showError(wrap));
    }

    function renderDavamiyyetTable(wrap, data) {
        if (!data.departamentlar || data.departamentlar.length === 0) {
            wrap.innerHTML = '<div class="rp-no-data">Bu dövr üçün davamiyyət məlumatı tapılmadı.</div>';
            return;
        }

        let html = '<table class="rp-table">';
        html += '<thead><tr>';
        html += '<th>İşçi</th>';
        html += '<th class="rp-th-center">İşdə</th>';
        html += '<th class="rp-th-center">Gecikm.</th>';
        html += '<th class="rp-th-center">Qayıb</th>';
        html += '<th class="rp-th-center">İcazəli</th>';
        html += '<th class="rp-th-center">Cəmi gün</th>';
        html += '</tr></thead><tbody>';

        let umumiIsde = 0, umumiGecikme = 0, umumiQayib = 0, umumiIcazeli = 0, umumiGun = 0;

        data.departamentlar.forEach(dept => {
            // Department header
            html += '<tr class="rp-dept-header">';
            html += '<td colspan="6"><i class="bi bi-building"></i>' + dept.departament + '</td>';
            html += '</tr>';

            // Employee rows
            dept.isciler.forEach(isci => {
                html += '<tr>';
                html += '<td>' + isci.isciAdSoyad + '</td>';
                html += '<td class="rp-td-center">' + isci.isde + '</td>';
                html += '<td class="rp-td-center">' + isci.gecikme + '</td>';
                html += '<td class="rp-td-center">' + isci.qayib + '</td>';
                html += '<td class="rp-td-center">' + isci.icazeli + '</td>';
                html += '<td class="rp-td-center">' + isci.cemiGun + '</td>';
                html += '</tr>';
            });

            // Department total
            html += '<tr class="rp-dept-total">';
            html += '<td>Cəmi: ' + dept.isciler.length + ' işçi</td>';
            html += '<td class="rp-td-center">' + dept.cemiIsde + '</td>';
            html += '<td class="rp-td-center">' + dept.cemiGecikme + '</td>';
            html += '<td class="rp-td-center">' + dept.cemiQayib + '</td>';
            html += '<td class="rp-td-center">' + dept.cemiIcazeli + '</td>';
            html += '<td class="rp-td-center">' + dept.cemiGun + '</td>';
            html += '</tr>';

            umumiIsde += dept.cemiIsde;
            umumiGecikme += dept.cemiGecikme;
            umumiQayib += dept.cemiQayib;
            umumiIcazeli += dept.cemiIcazeli;
            umumiGun += dept.cemiGun;
        });

        // Grand total
        html += '<tr class="rp-grand-total">';
        html += '<td>Ümumi</td>';
        html += '<td class="rp-td-center">' + umumiIsde + '</td>';
        html += '<td class="rp-td-center">' + umumiGecikme + '</td>';
        html += '<td class="rp-td-center">' + umumiQayib + '</td>';
        html += '<td class="rp-td-center">' + umumiIcazeli + '</td>';
        html += '<td class="rp-td-center">' + umumiGun + '</td>';
        html += '</tr>';

        html += '</tbody></table>';
        wrap.innerHTML = html;
    }

    // ── Balans Hesabatı ──────────────────────────────────
    document.getElementById('btnBalansGoster').addEventListener('click', loadBalansData);

    document.getElementById('btnBalansExcel').addEventListener('click', function () {
        var il = document.getElementById('balansIl').value;
        window.location.href = '/HR/Hesabat/ExportBalansExcel?il=' + il;
    });

    document.getElementById('btnBalansPdf').addEventListener('click', function () {
        printTable('balansTableWrap', 'Balans Hesabatı');
    });

    function loadBalansData() {
        const il = document.getElementById('balansIl').value;
        const wrap = document.getElementById('balansTableWrap');

        showLoading(wrap);

        fetch('/HR/Hesabat/GetBalansData?il=' + il)
            .then(r => r.json())
            .then(data => renderBalansTable(wrap, data))
            .catch(() => showError(wrap));
    }

    function renderBalansTable(wrap, data) {
        if (!data.departamentlar || data.departamentlar.length === 0) {
            wrap.innerHTML = '<div class="rp-no-data">Bu il üçün balans məlumatı tapılmadı.</div>';
            return;
        }

        let html = '<table class="rp-table">';
        html += '<thead><tr>';
        html += '<th>İşçi</th>';
        html += '<th class="rp-th-center">İllik (T/İ/Q)</th>';
        html += '<th class="rp-th-center">Xəstəlik</th>';
        html += '<th class="rp-th-center">Ezamiyyət</th>';
        html += '</tr></thead><tbody>';

        data.departamentlar.forEach(dept => {
            // Department header
            html += '<tr class="rp-dept-header">';
            html += '<td colspan="4"><i class="bi bi-building"></i>' + dept.departament + '</td>';
            html += '</tr>';

            // Employee rows
            dept.isciler.forEach(isci => {
                html += '<tr>';
                html += '<td>' + isci.isciAdSoyad + '</td>';
                html += '<td class="rp-td-center rp-balans-group">' + balansCell(isci.illikToplam, isci.illikIstifade, isci.illikQaliq) + '</td>';
                html += '<td class="rp-td-center rp-balans-group">' + limitsizBalansCell(isci.xestelikIstifade) + '</td>';
                html += '<td class="rp-td-center rp-balans-group">' + limitsizBalansCell(isci.ezamiyyetIstifade) + '</td>';
                html += '</tr>';
            });
        });

        html += '</tbody></table>';
        wrap.innerHTML = html;
    }

    function limitsizBalansCell(istifade) {
        return '<span class="rp-balans-val">' + istifade + ' gün istifadə</span>';
    }

    function balansCell(toplam, istifade, qaliq) {
        const qaliqCls = qaliq <= 3 ? 'rp-balans-qaliq--low' : 'rp-balans-qaliq--ok';
        return '<span class="rp-balans-val">' + toplam + '</span>' +
            '<span class="rp-balans-sep">/</span>' +
            '<span class="rp-balans-val">' + istifade + '</span>' +
            '<span class="rp-balans-sep">/</span>' +
            '<span class="rp-balans-val rp-balans-qaliq ' + qaliqCls + '">' + qaliq + '</span>';
    }

    // ── Vergi Hesabatı ──────────────────────────────────
    document.getElementById('btnVergiGoster').addEventListener('click', loadVergiData);

    document.getElementById('btnVergiExcel').addEventListener('click', function () {
        var il = document.getElementById('vergiIl').value;
        window.location.href = '/HR/Hesabat/ExportVergiExcel?il=' + il;
    });

    document.getElementById('btnVergiPdf').addEventListener('click', function () {
        printTable('vergiTableWrap', 'Vergi Hesabatı');
    });

    function loadVergiData() {
        const il = document.getElementById('vergiIl').value;
        const wrap = document.getElementById('vergiTableWrap');

        showLoading(wrap);

        fetch('/HR/Hesabat/GetVergiData?il=' + il)
            .then(r => r.json())
            .then(data => renderVergiTable(wrap, data))
            .catch(() => showError(wrap));
    }

    function renderVergiTable(wrap, data) {
        if (!data.departamentlar || data.departamentlar.length === 0) {
            wrap.innerHTML = '<div class="rp-no-data">Bu il üçün vergi məlumatı tapılmadı.</div>';
            return;
        }

        let html = '<table class="rp-table">';
        html += '<thead><tr>';
        html += '<th>İşçi</th>';
        html += '<th class="rp-th-right">Gross (AZN)</th>';
        html += '<th class="rp-th-right">Gəlir Vergisi</th>';
        html += '<th class="rp-th-right">DSMF</th>';
        html += '<th class="rp-th-right">İşsizlik</th>';
        html += '<th class="rp-th-right">Cəmi Tutulma</th>';
        html += '</tr></thead><tbody>';

        data.departamentlar.forEach(dept => {
            // Department header
            html += '<tr class="rp-dept-header">';
            html += '<td colspan="6"><i class="bi bi-building"></i>' + dept.departament + '</td>';
            html += '</tr>';

            // Employee rows
            dept.isciler.forEach(isci => {
                html += '<tr>';
                html += '<td>' + isci.isciAdSoyad + '</td>';
                html += '<td class="rp-td-right">' + formatMoney(isci.brutMebleg) + '</td>';
                html += '<td class="rp-td-right">' + formatMoney(isci.gelirVergisi) + '</td>';
                html += '<td class="rp-td-right">' + formatMoney(isci.dsmf) + '</td>';
                html += '<td class="rp-td-right">' + formatMoney(isci.issizlik) + '</td>';
                html += '<td class="rp-td-right">' + formatMoney(isci.cemiTutulma) + '</td>';
                html += '</tr>';
            });

            // Department total
            html += '<tr class="rp-dept-total">';
            html += '<td>Cəmi: ' + dept.isciler.length + ' işçi</td>';
            html += '<td class="rp-td-right">' + formatMoney(dept.cemiBrut) + '</td>';
            html += '<td class="rp-td-right">' + formatMoney(dept.cemiGelirVergisi) + '</td>';
            html += '<td class="rp-td-right">' + formatMoney(dept.cemiDsmf) + '</td>';
            html += '<td class="rp-td-right">' + formatMoney(dept.cemiIssizlik) + '</td>';
            html += '<td class="rp-td-right">' + formatMoney(dept.cemiTutulma) + '</td>';
            html += '</tr>';
        });

        // Grand total
        html += '<tr class="rp-grand-total">';
        html += '<td>Ümumi</td>';
        html += '<td class="rp-td-right">' + formatMoney(data.umumiBrut) + '</td>';
        html += '<td class="rp-td-right">' + formatMoney(data.umumiGelirVergisi) + '</td>';
        html += '<td class="rp-td-right">' + formatMoney(data.umumiDsmf) + '</td>';
        html += '<td class="rp-td-right">' + formatMoney(data.umumiIssizlik) + '</td>';
        html += '<td class="rp-td-right">' + formatMoney(data.umumiTutulma) + '</td>';
        html += '</tr>';

        html += '</tbody></table>';
        wrap.innerHTML = html;
    }

})();

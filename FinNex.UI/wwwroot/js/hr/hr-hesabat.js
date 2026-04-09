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

    // ── Maaş Hesabatı ────────────────────────────────────
    document.getElementById('btnMaasGoster').addEventListener('click', loadMaasData);

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
        html += '<th class="rp-th-right">Brut (AZN)</th>';
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

})();

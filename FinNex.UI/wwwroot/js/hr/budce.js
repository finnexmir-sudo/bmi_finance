// ── HR Büdcə JS ──────────────────────────────────────────

(function () {
    'use strict';

    const ayAdlari = ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'İyun',
                      'İyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'];
    let cachedData = null;
    let currentIl  = null;

    function fmt(val) {
        var n = Number(val);
        return n.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // ── Yüklə ────────────────────────────────────────────
    function loadData() {
        currentIl = document.getElementById('budceIl').value;
        var body  = document.getElementById('budceBody');
        body.innerHTML = '<tr><td colspan="28" class="budce-empty"><i class="bi bi-hourglass-split"></i> Yüklənir...</td></tr>';
        closeDetay();

        fetch('/HR/Budce/GetData?il=' + currentIl)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                cachedData = data;
                renderTable(data);
                renderSummary(data);
            })
            .catch(function () {
                body.innerHTML = '<tr><td colspan="28" class="budce-empty">Xəta baş verdi</td></tr>';
            });
    }

    // ── Cədvəl render ────────────────────────────────────
    function renderTable(data) {
        var body = document.getElementById('budceBody');

        if (!data.departamentlar || data.departamentlar.length === 0) {
            body.innerHTML = '<tr><td colspan="28" class="budce-empty">Məlumat tapılmadı</td></tr>';
            return;
        }

        var html = '';

        data.departamentlar.forEach(function (dept) {
            html += '<tr>';
            html += '<td class="budce-td-dept">' +
                    '<a class="budce-dept-link" onclick="openHesabat(' + dept.departamentId + ')">' +
                    dept.departamentAd + '</a></td>';

            dept.aylar.forEach(function (a) {
                var planCls = a.plan === 0 ? 'budce-cell--zero' : 'budce-cell--plan';
                var faktCls = a.faktiki === 0 ? 'budce-cell--zero'
                            : (a.plan > 0 && a.faktiki > a.plan) ? 'budce-cell--over'
                            : 'budce-cell--fakt';

                var pct = a.plan > 0 ? Math.min(Math.round(a.faktiki / a.plan * 100), 100) : 0;
                var barCls = a.faktiki > a.plan && a.plan > 0 ? 'budce-bar-fill--over'
                           : pct > 80 ? 'budce-bar-fill--warn' : '';
                var bar = a.plan > 0
                    ? '<div class="budce-bar"><div class="budce-bar-fill ' + barCls + '" style="width:' + pct + '%"></div></div>'
                    : '';

                html += '<td class="' + planCls + ' budce-plan-cell" onclick="openEdit(' +
                        dept.departamentId + ',\'' + dept.departamentAd.replace(/'/g, "\\'") + '\',' +
                        a.ay + ',' + a.plan + ')" title="Planı redaktə et">' +
                        fmt(a.plan) + '</td>';

                html += '<td class="' + faktCls + ' budce-fakt-cell" onclick="openDetay(' +
                        dept.departamentId + ',\'' + dept.departamentAd.replace(/'/g, "\\'") + '\',' +
                        a.ay + ')" title="Xərcləri gör">' +
                        fmt(a.faktiki) + bar + '</td>';
            });

            var topFerq = dept.toplamPlan - dept.toplamFaktiki;
            html += '<td class="budce-cell--total">' + fmt(dept.toplamPlan) + '</td>';
            html += '<td class="budce-cell--total ' + (dept.toplamFaktiki > dept.toplamPlan && dept.toplamPlan > 0 ? 'budce-cell--over' : '') + '">' + fmt(dept.toplamFaktiki) + '</td>';
            html += '<td class="budce-cell--ferq ' + (topFerq < 0 ? 'budce-cell--neg' : (topFerq === 0 ? '' : 'budce-cell--pos')) + '">' + fmt(topFerq) + '</td>';
            html += '</tr>';
        });

        var totalFerq = data.umumiPlan - data.umumiFaktiki;
        html += '<tr class="budce-summary-row">';
        html += '<td>CƏMİ</td>';
        for (var i = 0; i < 24; i++) html += '<td></td>';
        html += '<td>' + fmt(data.umumiPlan) + '</td>';
        html += '<td>' + fmt(data.umumiFaktiki) + '</td>';
        html += '<td class="' + (totalFerq < 0 ? 'budce-cell--neg' : 'budce-cell--pos') + '">' + fmt(totalFerq) + '</td>';
        html += '</tr>';

        body.innerHTML = html;
    }

    // ── Xülasə kartlar ──────────────────────────────────
    function renderSummary(data) {
        document.getElementById('summaryCards').style.display = 'grid';
        document.getElementById('umumiPlan').textContent    = fmt(data.umumiPlan) + ' ₼';
        document.getElementById('umumiFaktiki').textContent = fmt(data.umumiFaktiki) + ' ₼';
        var ferq   = data.umumiPlan - data.umumiFaktiki;
        var ferqEl = document.getElementById('umumiFerq');
        ferqEl.textContent = fmt(ferq) + ' ₼';
        ferqEl.style.color = ferq >= 0 ? '#16a34a' : '#dc2626';
    }

    // ── Plan redaktə modalı ──────────────────────────────
    window.openEdit = function (deptId, deptAd, ay, plan) {
        document.getElementById('editDeptId').value = deptId;
        document.getElementById('editAy').value     = ay;
        document.getElementById('editDeptAd').value = deptAd;
        document.getElementById('editAyAd').value   = ayAdlari[ay - 1] + ' ' + currentIl;
        document.getElementById('editPlan').value   = plan;
        document.getElementById('editQeyd').value   = '';
        document.getElementById('editModal').style.display = 'flex';
        setTimeout(function () { document.getElementById('editPlan').select(); }, 50);
    };

    function closeModal() {
        document.getElementById('editModal').style.display = 'none';
    }

    function saveModal() {
        var btn = document.getElementById('btnModalSave');
        btn.disabled = true;
        btn.textContent = 'Saxlanır...';

        var payload = {
            departamentId : parseInt(document.getElementById('editDeptId').value),
            il            : parseInt(currentIl),
            ay            : parseInt(document.getElementById('editAy').value),
            planMebleg    : parseFloat(document.getElementById('editPlan').value) || 0,
            qeyd          : document.getElementById('editQeyd').value
        };

        var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        fetch('/HR/Budce/Create', {
            method  : 'POST',
            headers : { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body    : JSON.stringify(payload)
        })
        .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
        .then(function () { closeModal(); loadData(); })
        .catch(function () { alert('Yadda saxlama zamanı xəta baş verdi.'); })
        .finally(function () {
            btn.disabled    = false;
            btn.innerHTML   = '<i class="bi bi-save"></i> Yadda saxla';
        });
    }

    // ── Detay panel ──────────────────────────────────────
    window.openDetay = function (deptId, deptAd, ay) {
        var panel = document.getElementById('detayPanel');
        panel.classList.add('budce-detay--open');
        document.getElementById('detayTitle').textContent =
            deptAd + '  ·  ' + ayAdlari[ay - 1] + ' ' + currentIl;
        document.getElementById('detayBody').innerHTML =
            '<tr><td colspan="5" class="budce-empty"><i class="bi bi-hourglass-split"></i> Yüklənir...</td></tr>';
        document.getElementById('detayFooter').style.display = 'none';
        document.getElementById('detayHesabatLink').href =
            '/HR/XercHesabat?departamentId=' + deptId;

        fetch('/HR/Budce/GetDetay?departamentId=' + deptId + '&il=' + currentIl + '&ay=' + ay)
            .then(function (r) { return r.json(); })
            .then(function (data) { renderDetay(data); })
            .catch(function () {
                document.getElementById('detayBody').innerHTML =
                    '<tr><td colspan="5" class="budce-empty">Xəta baş verdi</td></tr>';
            });
    };

    function renderDetay(data) {
        var body = document.getElementById('detayBody');

        if (!data.xercler || data.xercler.length === 0) {
            body.innerHTML = '<tr><td colspan="5" class="budce-empty">' +
                '<i class="bi bi-inbox" style="font-size:2rem;display:block;margin-bottom:8px"></i>' +
                'Bu ay üçün xərc tapılmadı</td></tr>';
            document.getElementById('detayFooter').style.display = 'none';
            return;
        }

        var statusRenk = { 0: '#64748b', 1: '#2563eb', 2: '#7c3aed', 3: '#16a34a', 4: '#dc2626' };
        var html = '';
        data.xercler.forEach(function (x) {
            var renk  = statusRenk[x.status] || '#64748b';
            var badge = '<span style="font-size:10px;font-weight:600;padding:2px 8px;border-radius:20px;' +
                        'background:' + renk + '20;color:' + renk + '">' + x.statusAd + '</span>';
            var qebz  = x.qebzYolu
                ? ' <a href="' + x.qebzYolu + '" target="_blank" style="color:#667eea" title="Sənəd"><i class="bi bi-paperclip"></i></a>'
                : '';
            var manual = x.manualGiris
                ? '<span style="font-size:10px;background:#dbeafe;color:#1d4ed8;padding:1px 5px;border-radius:4px;margin-right:4px">M</span>'
                : '';
            html += '<tr>' +
                '<td style="white-space:nowrap;color:#64748b">' + x.tarix + '</td>' +
                '<td>' + manual + x.isci + '</td>' +
                '<td>' + x.kateqoriya + '</td>' +
                '<td class="budce-detay-tesvir" title="' + x.tesvir.replace(/"/g, '&quot;') + '">' + x.tesvir + badge + qebz + '</td>' +
                '<td style="text-align:right;font-weight:600;white-space:nowrap">' + fmt(x.mebleg) + ' ₼</td>' +
                '</tr>';
        });
        body.innerHTML = html;

        document.getElementById('detayToplam').textContent = fmt(data.odenilenToplam) + ' ₼';
        document.getElementById('detayFooter').style.display = 'flex';
    }

    function closeDetay() {
        var panel = document.getElementById('detayPanel');
        if (panel) panel.classList.remove('budce-detay--open');
    }

    // ── Şöbə hesabatına keç ──────────────────────────────
    window.openHesabat = function (deptId) {
        window.location.href = '/HR/XercHesabat?departamentId=' + deptId;
    };

    // ── Excel ────────────────────────────────────────────
    function exportExcel() {
        window.location.href = '/HR/Budce/ExportExcel?il=' + currentIl;
    }

    // ── Event listeners ──────────────────────────────────
    document.getElementById('btnYukle').addEventListener('click', loadData);
    document.getElementById('btnExcel').addEventListener('click', exportExcel);
    document.getElementById('btnModalClose').addEventListener('click', closeModal);
    document.getElementById('btnModalCancel').addEventListener('click', closeModal);
    document.getElementById('btnModalSave').addEventListener('click', saveModal);
    document.getElementById('btnDetayClose').addEventListener('click', closeDetay);

    document.getElementById('editModal').addEventListener('click', function (e) {
        if (e.target === this) closeModal();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { closeModal(); closeDetay(); }
    });

    loadData();

})();

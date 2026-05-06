// HR Buedce JS

(function () {
    'use strict';

    var ayAdlari = ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'Iyun',
                    'Iyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'];
    var TOTAL_COLS = 41; // 1 dept + 12*3 month cells + 4 totals
    var cachedData = null;
    var currentIl  = null;

    function fmt(val) {
        var n = Number(val) || 0;
        return n.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // -- Yuekle -----------------------------------------------
    function loadData() {
        currentIl = document.getElementById('budceIl').value;
        var body  = document.getElementById('budceBody');
        body.innerHTML = '<tr><td colspan="' + TOTAL_COLS + '" class="budce-empty">' +
            '<i class="bi bi-hourglass-split"></i> Yueklenilir...</td></tr>';
        closeDetay();

        fetch('/HR/Budce/GetData?il=' + currentIl)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                cachedData = data;
                renderTable(data);
                renderSummary(data);
            })
            .catch(function () {
                body.innerHTML = '<tr><td colspan="' + TOTAL_COLS + '" class="budce-empty">Xeta bas verdi</td></tr>';
            });
    }

    // -- Cedvel render ----------------------------------------
    function renderTable(data) {
        var body = document.getElementById('budceBody');

        if (!data.departamentlar || data.departamentlar.length === 0) {
            body.innerHTML = '<tr><td colspan="' + TOTAL_COLS + '" class="budce-empty">Melumat tapilmadi</td></tr>';
            return;
        }

        var html = '';

        data.departamentlar.forEach(function (dept) {
            html += '<tr>';
            html += '<td class="budce-td-dept">' +
                    '<a class="budce-dept-link" onclick="openHesabat(' + dept.departamentId + ')">' +
                    dept.departamentAd + '</a></td>';

            dept.aylar.forEach(function (a) {
                // P cell - Plan
                var planCls = a.plan === 0 ? 'budce-cell--zero' : 'budce-cell--plan';
                html += '<td class="' + planCls + ' budce-plan-cell" onclick="openEdit(' +
                        dept.departamentId + ',\'' + dept.departamentAd.replace(/'/g, "\\'") + '\',' +
                        a.ay + ',' + a.plan + ')" title="Plani redakte et">' +
                        fmt(a.plan) + '</td>';

                // F cell - Faktiki, progress bar uses faiz = (faktiki+rezerv)/plan
                var faktCls = a.faktiki === 0 ? 'budce-cell--zero'
                            : (a.plan > 0 && a.faiz >= 100) ? 'budce-cell--over'
                            : 'budce-cell--fakt';
                var pct    = Math.min(Number(a.faiz) || 0, 100);
                var barCls = Number(a.faiz) >= 100 ? 'budce-bar-fill--over'
                           : Number(a.faiz) > 79   ? 'budce-bar-fill--warn' : '';
                var bar    = a.plan > 0
                    ? '<div class="budce-bar"><div class="budce-bar-fill ' + barCls + '" style="width:' + pct + '%"></div></div>'
                    : '';

                html += '<td class="' + faktCls + ' budce-fakt-cell" onclick="openDetay(' +
                        dept.departamentId + ',\'' + dept.departamentAd.replace(/'/g, "\\'") + '\',' +
                        a.ay + ')" title="Xercleri goer">' +
                        fmt(a.faktiki) + bar + '</td>';

                // R cell - Rezerv (approved, not paid)
                var rezervVal = Number(a.rezerv) || 0;
                var rezervCls = rezervVal === 0 ? 'budce-cell--zero' : 'budce-cell--rezerv';
                html += '<td class="' + rezervCls + '" title="Rezerv - tesdiqlenib, odenil'+"'"+'meyib">' +
                        fmt(rezervVal) + '</td>';
            });

            // Totals
            var azadVal = Number(dept.toplamAzad) || 0;
            var rezervTotal = Number(dept.toplamRezerv) || 0;
            html += '<td class="budce-cell--total">' + fmt(dept.toplamPlan) + '</td>';
            html += '<td class="budce-cell--total">' + fmt(dept.toplamFaktiki) + '</td>';
            html += '<td class="budce-cell--total budce-cell--rezerv-total">' + fmt(rezervTotal) + '</td>';
            html += '<td class="budce-cell--ferq ' + (azadVal < 0 ? 'budce-cell--neg' : (azadVal === 0 ? '' : 'budce-cell--pos')) + '">' + fmt(azadVal) + '</td>';
            html += '</tr>';
        });

        // Summary row
        html += '<tr class="budce-summary-row">';
        html += '<td>CEMI</td>';
        for (var i = 0; i < 36; i++) html += '<td></td>';
        html += '<td>' + fmt(data.umumiPlan) + '</td>';
        html += '<td>' + fmt(data.umumiFaktiki) + '</td>';
        html += '<td>' + fmt(Number(data.umumiRezerv) || 0) + '</td>';
        var totalAzad = Number(data.umumiAzad) || 0;
        html += '<td class="' + (totalAzad < 0 ? 'budce-cell--neg' : 'budce-cell--pos') + '">' + fmt(totalAzad) + '</td>';
        html += '</tr>';

        body.innerHTML = html;
    }

    // -- Xuelase kartlar --------------------------------------
    function renderSummary(data) {
        document.getElementById('summaryCards').style.display = 'grid';
        document.getElementById('umumiPlan').textContent     = fmt(data.umumiPlan) + ' ₼';
        document.getElementById('umumiFaktiki').textContent  = fmt(data.umumiFaktiki) + ' ₼';

        var rezervEl = document.getElementById('umumiRezerv');
        if (rezervEl) rezervEl.textContent = fmt(Number(data.umumiRezerv) || 0) + ' ₼';

        var azad   = Number(data.umumiAzad) || 0;
        var azadEl = document.getElementById('umumiAzad');
        if (azadEl) {
            azadEl.textContent = fmt(azad) + ' ₼';
            azadEl.style.color = azad >= 0 ? '#16a34a' : '#dc2626';
        }
    }

    // -- Plan redakte modali ----------------------------------
    window.openEdit = function (deptId, deptAd, ay, plan) {
        document.getElementById('editDeptId').value = deptId;
        document.getElementById('editAy').value     = ay;
        document.getElementById('editDeptAd').textContent = deptAd;
        document.getElementById('editAyAd').textContent   = ayAdlari[ay - 1] + ' ' + currentIl;
        document.getElementById('editAyRow').style.display = '';
        document.getElementById('editPlan').value   = plan || '';
        document.getElementById('editQeyd').value   = '';
        document.getElementById('editTopluTetbiq').checked = false;
        document.getElementById('editModal').style.display = 'flex';
        setTimeout(function () { document.getElementById('editPlan').select(); }, 50);
    };

    function closeModal() {
        document.getElementById('editModal').style.display = 'none';
        document.getElementById('editTopluTetbiq').checked = false;
    }

    function saveModal() {
        var planVal = parseFloat(document.getElementById('editPlan').value);
        if (isNaN(planVal) || planVal < 0) {
            document.getElementById('editPlan').focus();
            return;
        }

        var topluTetbiq = document.getElementById('editTopluTetbiq').checked;
        var btn = document.getElementById('btnModalSave');
        btn.disabled = true;
        btn.textContent = 'Saxlanir...';

        var payload = {
            departamentId : parseInt(document.getElementById('editDeptId').value),
            il            : parseInt(currentIl),
            ay            : parseInt(document.getElementById('editAy').value),
            planMebleg    : planVal,
            qeyd          : document.getElementById('editQeyd').value,
            topluTetbiq   : topluTetbiq
        };

        var token = (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
        fetch('/HR/Budce/Create', {
            method  : 'POST',
            headers : { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
            body    : JSON.stringify(payload)
        })
        .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
        .then(function () { closeModal(); loadData(); })
        .catch(function () { alert('Yadda saxlama zamani xeta bas verdi.'); })
        .finally(function () {
            btn.disabled  = false;
            btn.innerHTML = '<i class="bi bi-save"></i> Yadda saxla';
        });
    }

    // TopluTetbiq checkbox - ay satirini gizlet/goester
    document.getElementById('editTopluTetbiq').addEventListener('change', function () {
        document.getElementById('editAyRow').style.display = this.checked ? 'none' : '';
    });

    // -- Detay panel ------------------------------------------
    window.openDetay = function (deptId, deptAd, ay) {
        var panel = document.getElementById('detayPanel');
        panel.classList.add('budce-detay--open');
        document.getElementById('detayTitle').textContent =
            deptAd + '  ·  ' + ayAdlari[ay - 1] + ' ' + currentIl;
        document.getElementById('detayBody').innerHTML =
            '<tr><td colspan="5" class="budce-empty"><i class="bi bi-hourglass-split"></i> Yueklenilir...</td></tr>';
        document.getElementById('detayFooter').style.display = 'none';
        document.getElementById('detayHesabatLink').href =
            '/HR/XercHesabat?departamentId=' + deptId;

        fetch('/HR/Budce/GetDetay?departamentId=' + deptId + '&il=' + currentIl + '&ay=' + ay)
            .then(function (r) { return r.json(); })
            .then(function (data) { renderDetay(data); })
            .catch(function () {
                document.getElementById('detayBody').innerHTML =
                    '<tr><td colspan="5" class="budce-empty">Xeta bas verdi</td></tr>';
            });
    };

    function renderDetay(data) {
        var body = document.getElementById('detayBody');

        if (!data.xercler || data.xercler.length === 0) {
            body.innerHTML = '<tr><td colspan="5" class="budce-empty">' +
                '<i class="bi bi-inbox" style="font-size:2rem;display:block;margin-bottom:8px"></i>' +
                'Bu ay ucun xerc tapilmadi</td></tr>';
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
                ? ' <a href="' + x.qebzYolu + '" target="_blank" style="color:#667eea" title="Senad"><i class="bi bi-paperclip"></i></a>'
                : '';
            var manual = x.manualGiris
                ? '<span style="font-size:10px;background:#dbeafe;color:#1d4ed8;padding:1px 5px;border-radius:4px;margin-right:4px">M</span>'
                : '';
            // Rezerv rows get a subtle amber background indicator
            var rowStyle = x.isRezerv ? ' style="background:#fffbeb"' : '';
            html += '<tr' + rowStyle + '>' +
                '<td style="white-space:nowrap;color:#64748b">' + x.tarix + '</td>' +
                '<td>' + manual + x.isci + '</td>' +
                '<td>' + x.kateqoriya + '</td>' +
                '<td class="budce-detay-tesvir" title="' + String(x.tesvir).replace(/"/g, '&quot;') + '">' + x.tesvir + ' ' + badge + qebz + '</td>' +
                '<td style="text-align:right;font-weight:600;white-space:nowrap">' + fmt(x.mebleg) + ' ₼</td>' +
                '</tr>';
        });
        body.innerHTML = html;

        var odenilenEl = document.getElementById('detayToplam');
        var rezervEl   = document.getElementById('detayRezervToplam');
        if (odenilenEl) odenilenEl.textContent = fmt(data.odenilenToplam) + ' ₼';
        if (rezervEl)   rezervEl.textContent   = fmt(Number(data.rezervToplam) || 0) + ' ₼';
        document.getElementById('detayFooter').style.display = 'flex';
    }

    function closeDetay() {
        var panel = document.getElementById('detayPanel');
        if (panel) panel.classList.remove('budce-detay--open');
    }

    // -- Sobe hesabatina kec ----------------------------------
    window.openHesabat = function (deptId) {
        window.location.href = '/HR/XercHesabat?departamentId=' + deptId;
    };

    // -- Excel ------------------------------------------------
    function exportExcel() {
        window.location.href = '/HR/Budce/ExportExcel?il=' + currentIl;
    }

    // -- Event listeners --------------------------------------
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

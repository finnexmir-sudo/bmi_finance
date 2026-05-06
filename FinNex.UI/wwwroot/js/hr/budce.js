// HR Buedce JS

(function () {
    'use strict';

    var ayAdlari = ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'Iyun',
                    'Iyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'];
    var TOTAL_COLS = 41; // 1 dept + 12*3 month cells + 4 totals
    var cachedData      = null;
    var cachedSirket    = null;
    var currentIl       = null;

    function fmt(val) {
        var n = Number(val) || 0;
        return n.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function token() {
        return (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
    }

    // ── Yüklə ────────────────────────────────────────────────
    function loadData() {
        currentIl = document.getElementById('budceIl').value;
        document.getElementById('sirketIlLabel').textContent = currentIl;

        var body = document.getElementById('budceBody');
        body.innerHTML = '<tr><td colspan="' + TOTAL_COLS + '" class="budce-empty">' +
            '<i class="bi bi-hourglass-split"></i> Yueklenilir...</td></tr>';
        closeDetay();

        Promise.all([
            fetch('/HR/Budce/GetData?il=' + currentIl).then(function (r) { return r.json(); }),
            fetch('/HR/Budce/GetSirketBudce?il=' + currentIl).then(function (r) { return r.json(); })
        ]).then(function (results) {
            cachedData   = results[0];
            cachedSirket = results[1];
            renderTable(cachedData);
            renderSummary(cachedData);
            renderSirketPanel(cachedSirket);
        }).catch(function () {
            body.innerHTML = '<tr><td colspan="' + TOTAL_COLS + '" class="budce-empty">Xeta bas verdi</td></tr>';
        });
    }

    // ── Şirkət büdcəsi paneli ────────────────────────────────
    function renderSirketPanel(data) {
        var sub    = document.getElementById('sirketSub');
        var center = document.getElementById('sirketCenter');

        if (!data || data.yoxdur) {
            sub.innerHTML = '<span style="color:#94a3b8"><i class="bi bi-info-circle"></i> ' +
                currentIl + ' ili üçün şirkət büdcəsi hələ təyin edilməyib</span>';
            center.style.display = 'none';
            return;
        }

        sub.innerHTML = '';
        center.style.display = 'flex';
        document.getElementById('sirketMebleg').textContent = fmt(data.mebleg) + ' ₼';
        document.getElementById('sirketBolusdurulub').textContent = fmt(data.bolusdurulub) + ' ₼';

        var qaliqEl = document.getElementById('sirketQaliq');
        qaliqEl.textContent = fmt(data.qaliq) + ' ₼';
        qaliqEl.style.color = data.qaliq < 0 ? '#dc2626' : '#16a34a';

        var faiz = Number(data.faiz) || 0;
        var fillEl = document.getElementById('sirketBarFill');
        fillEl.style.width = Math.min(faiz, 100) + '%';
        fillEl.className   = 'sirket-bar-fill' +
            (faiz >= 100 ? ' sirket-bar-fill--over' : faiz >= 80 ? ' sirket-bar-fill--warn' : '');
        document.getElementById('sirketFaiz').textContent = faiz + '%';
    }

    // ── Şirkət büdcəsi modal ─────────────────────────────────
    function openSirketModal() {
        document.getElementById('sirketModalSub').textContent = currentIl + ' ili üçün büdcə';
        document.getElementById('sirketMeblegInput').value = cachedSirket && !cachedSirket.yoxdur ? cachedSirket.mebleg : '';
        document.getElementById('sirketQeydInput').value   = (cachedSirket && cachedSirket.qeyd) || '';
        document.getElementById('bereberBolResult').style.display = 'none';

        var distWrap = document.getElementById('sirketModalDistWrap');
        distWrap.style.display = cachedSirket && !cachedSirket.yoxdur ? 'block' : 'none';

        document.getElementById('sirketModal').style.display = 'flex';
        setTimeout(function () { document.getElementById('sirketMeblegInput').select(); }, 50);
    }

    function closeSirketModal() {
        document.getElementById('sirketModal').style.display = 'none';
    }

    function saveSirketBudce() {
        var meblegVal = parseFloat(document.getElementById('sirketMeblegInput').value);
        if (isNaN(meblegVal) || meblegVal < 0) {
            document.getElementById('sirketMeblegInput').focus();
            return;
        }

        var btn = document.getElementById('btnSirketModalSave');
        btn.disabled = true;
        btn.textContent = 'Saxlanir...';

        fetch('/HR/Budce/SetSirketBudce', {
            method  : 'POST',
            headers : { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body    : JSON.stringify({
                il    : parseInt(currentIl),
                mebleg: meblegVal,
                qeyd  : document.getElementById('sirketQeydInput').value
            })
        })
        .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
        .then(function () {
            // show distribute section after saving
            document.getElementById('sirketModalDistWrap').style.display = 'block';
            // refresh panel without closing modal
            return fetch('/HR/Budce/GetSirketBudce?il=' + currentIl).then(function (r) { return r.json(); });
        })
        .then(function (data) {
            cachedSirket = data;
            renderSirketPanel(data);
            // update edit qaliq if modal is open
            updateEditQaliq();
        })
        .catch(function () { alert('Xeta bas verdi.'); })
        .finally(function () {
            btn.disabled  = false;
            btn.innerHTML = '<i class="bi bi-save"></i> Yadda saxla';
        });
    }

    function bereberBol() {
        var ayVal = parseInt(document.getElementById('bereberAy').value);
        var meblegVal = parseFloat(document.getElementById('sirketMeblegInput').value);
        if (isNaN(meblegVal) || meblegVal <= 0) { alert('Əvvəlcə büdcəni yadda saxlayın.'); return; }

        var btn = document.getElementById('btnBereberBol');
        btn.disabled = true;

        var requests = [];
        // payPerAy = ümumi büdcə / ay sayı (şöbə sayına bölmə server tərəfindədir)
        var aylar    = ayVal === 0 ? [1,2,3,4,5,6,7,8,9,10,11,12] : [ayVal];
        var payPerAy = Math.round((meblegVal / aylar.length) * 100) / 100;

        aylar.forEach(function (ay) {
            requests.push(
                fetch('/HR/Budce/BereberBol', {
                    method  : 'POST',
                    headers : { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                    body    : JSON.stringify({ il: parseInt(currentIl), ay: ay, mebleg: payPerAy })
                }).then(function (r) { return r.json(); })
            );
        });

        Promise.all(requests)
            .then(function () {
                var resultEl = document.getElementById('bereberBolResult');
                resultEl.style.display = 'block';
                resultEl.textContent = ayVal === 0
                    ? 'Bütün 12 ay: hər aya ' + fmt(payPerAy / 1) + ' ₼ bərabər paylandı.'
                    : ayAdlari[ayVal - 1] + ': hər şöbəyə ' + fmt(payPerAy) + ' ₼ paylandı.';
                loadData();
            })
            .catch(function () { alert('Xeta bas verdi.'); })
            .finally(function () { btn.disabled = false; });
    }

    // ── Cədvəl render ────────────────────────────────────────
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
                // P cell
                var planCls = a.plan === 0 ? 'budce-cell--zero' : 'budce-cell--plan';
                html += '<td class="' + planCls + ' budce-plan-cell" onclick="openEdit(' +
                        dept.departamentId + ',\'' + dept.departamentAd.replace(/'/g, "\\'") + '\',' +
                        a.ay + ',' + a.plan + ')" title="Plani redakte et">' +
                        fmt(a.plan) + '</td>';

                // F cell — progress bar uses faiz = (faktiki+rezerv)/plan
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

                // R cell — rezerv
                var rezervVal = Number(a.rezerv) || 0;
                var rezervCls = rezervVal === 0 ? 'budce-cell--zero' : 'budce-cell--rezerv';
                html += '<td class="' + rezervCls + '">' + fmt(rezervVal) + '</td>';
            });

            var azadVal     = Number(dept.toplamAzad) || 0;
            var rezervTotal = Number(dept.toplamRezerv) || 0;
            html += '<td class="budce-cell--total">' + fmt(dept.toplamPlan) + '</td>';
            html += '<td class="budce-cell--total">' + fmt(dept.toplamFaktiki) + '</td>';
            html += '<td class="budce-cell--total budce-cell--rezerv-total">' + fmt(rezervTotal) + '</td>';
            html += '<td class="budce-cell--ferq ' + (azadVal < 0 ? 'budce-cell--neg' : (azadVal === 0 ? '' : 'budce-cell--pos')) + '">' + fmt(azadVal) + '</td>';
            html += '</tr>';
        });

        // Cəm sətri
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

    // ── Xülasə kartlar ───────────────────────────────────────
    function renderSummary(data) {
        document.getElementById('summaryCards').style.display = 'grid';
        document.getElementById('umumiPlan').textContent    = fmt(data.umumiPlan) + ' ₼';
        document.getElementById('umumiFaktiki').textContent = fmt(data.umumiFaktiki) + ' ₼';

        var rezervEl = document.getElementById('umumiRezerv');
        if (rezervEl) rezervEl.textContent = fmt(Number(data.umumiRezerv) || 0) + ' ₼';

        var azad   = Number(data.umumiAzad) || 0;
        var azadEl = document.getElementById('umumiAzad');
        if (azadEl) {
            azadEl.textContent = fmt(azad) + ' ₼';
            azadEl.style.color = azad >= 0 ? '#16a34a' : '#dc2626';
        }
    }

    // ── Plan redaktə modalı ──────────────────────────────────
    function updateEditQaliq() {
        var qaliqRow = document.getElementById('editQaliqRow');
        var qaliqEl  = document.getElementById('editQaliqMebleg');
        if (!qaliqRow || !qaliqEl) return;
        if (cachedSirket && !cachedSirket.yoxdur) {
            qaliqRow.style.display = '';
            qaliqEl.textContent    = fmt(cachedSirket.qaliq) + ' ₼';
            qaliqEl.style.color    = cachedSirket.qaliq >= 0 ? '#16a34a' : '#dc2626';
        } else {
            qaliqRow.style.display = 'none';
        }
    }

    window.openEdit = function (deptId, deptAd, ay, plan) {
        document.getElementById('editDeptId').value = deptId;
        document.getElementById('editAy').value     = ay;
        document.getElementById('editDeptAd').textContent = deptAd;
        document.getElementById('editAyAd').textContent   = ayAdlari[ay - 1] + ' ' + currentIl;
        document.getElementById('editAyRow').style.display = '';
        document.getElementById('editPlan').value   = plan || '';
        document.getElementById('editQeyd').value   = '';
        document.getElementById('editTopluTetbiq').checked = false;
        updateEditQaliq();
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

        var btn = document.getElementById('btnModalSave');
        btn.disabled = true;
        btn.textContent = 'Saxlanir...';

        fetch('/HR/Budce/Create', {
            method  : 'POST',
            headers : { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body    : JSON.stringify({
                departamentId : parseInt(document.getElementById('editDeptId').value),
                il            : parseInt(currentIl),
                ay            : parseInt(document.getElementById('editAy').value),
                planMebleg    : planVal,
                qeyd          : document.getElementById('editQeyd').value,
                topluTetbiq   : document.getElementById('editTopluTetbiq').checked
            })
        })
        .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
        .then(function () { closeModal(); loadData(); })
        .catch(function () { alert('Yadda saxlama zamani xeta bas verdi.'); })
        .finally(function () {
            btn.disabled  = false;
            btn.innerHTML = '<i class="bi bi-save"></i> Yadda saxla';
        });
    }

    document.getElementById('editTopluTetbiq').addEventListener('change', function () {
        document.getElementById('editAyRow').style.display = this.checked ? 'none' : '';
    });

    // ── Detay panel ──────────────────────────────────────────
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
            var renk   = statusRenk[x.status] || '#64748b';
            var badge  = '<span style="font-size:10px;font-weight:600;padding:2px 8px;border-radius:20px;' +
                         'background:' + renk + '20;color:' + renk + '">' + x.statusAd + '</span>';
            var qebz   = x.qebzYolu
                ? ' <a href="' + x.qebzYolu + '" target="_blank" style="color:#667eea"><i class="bi bi-paperclip"></i></a>'
                : '';
            var manual = x.manualGiris
                ? '<span style="font-size:10px;background:#dbeafe;color:#1d4ed8;padding:1px 5px;border-radius:4px;margin-right:4px">M</span>'
                : '';
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

    // ── Şöbə hesabatına keç ──────────────────────────────────
    window.openHesabat = function (deptId) {
        window.location.href = '/HR/XercHesabat?departamentId=' + deptId;
    };

    // ── Excel ────────────────────────────────────────────────
    function exportExcel() {
        window.location.href = '/HR/Budce/ExportExcel?il=' + currentIl;
    }

    // ── Event listeners ──────────────────────────────────────
    document.getElementById('btnYukle').addEventListener('click', loadData);
    document.getElementById('btnExcel').addEventListener('click', exportExcel);
    document.getElementById('btnModalClose').addEventListener('click', closeModal);
    document.getElementById('btnModalCancel').addEventListener('click', closeModal);
    document.getElementById('btnModalSave').addEventListener('click', saveModal);
    document.getElementById('btnDetayClose').addEventListener('click', closeDetay);
    document.getElementById('btnSirketEdit').addEventListener('click', openSirketModal);
    document.getElementById('btnSirketModalClose').addEventListener('click', closeSirketModal);
    document.getElementById('btnSirketModalCancel').addEventListener('click', closeSirketModal);
    document.getElementById('btnSirketModalSave').addEventListener('click', saveSirketBudce);
    document.getElementById('btnBereberBol').addEventListener('click', bereberBol);

    document.getElementById('editModal').addEventListener('click', function (e) {
        if (e.target === this) closeModal();
    });
    document.getElementById('sirketModal').addEventListener('click', function (e) {
        if (e.target === this) closeSirketModal();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { closeModal(); closeDetay(); closeSirketModal(); }
    });

    loadData();

})();

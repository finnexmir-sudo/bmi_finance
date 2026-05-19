// HR Buedce JS

(function () {
    'use strict';

    var ayAdlari = ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'Iyun',
                    'Iyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'];
    var TOTAL_COLS = 41; // 1 dept + 12*3 month cells + 4 totals
    var cachedData      = null;
    var cachedSirket    = null;
    var currentIl       = null;
    var undoSnapshot    = null;   // bölüşdürmədən əvvəlki planlar
    var UNDO_KEY        = 'budce_undo_snapshot';

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
            // localStorage-da undo varsa düyməni göstər
            showUndoBtn(!!loadUndo());
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
        document.getElementById('bereberTesdiq').style.display = 'none';

        // Bölüşdürmə sahəsini qalıq məbləğlə əvvəlcədən doldur
        var bereberMeblegEl   = document.getElementById('bereberMebleg');
        var bereberMeblegHint = document.getElementById('bereberMeblegHint');
        if (cachedSirket && !cachedSirket.yoxdur) {
            var qaliq = Number(cachedSirket.qaliq) || 0;
            bereberMeblegEl.value       = qaliq > 0 ? qaliq : '';
            bereberMeblegEl.placeholder = qaliq > 0
                ? 'Qalıq: ' + fmt(qaliq) + ' ₼'
                : 'Boş = tam büdcə (' + fmt(cachedSirket.mebleg) + ' ₼)';
            bereberMeblegHint.textContent = qaliq > 0
                ? 'Qalıq ' + fmt(qaliq) + ' ₼ avtomatik dolduruldu. İstədiyiniz məbləği dəyişə bilərsiniz.'
                : '';
        } else {
            bereberMeblegEl.value = '';
        }

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

    // localStorage undo idarəsi
    function saveUndo(snap) {
        try { localStorage.setItem(UNDO_KEY, JSON.stringify({ il: currentIl, snap: snap })); } catch(e) {}
        showUndoBtn(true);
    }
    function loadUndo() {
        try {
            var raw = localStorage.getItem(UNDO_KEY);
            if (!raw) return null;
            var obj = JSON.parse(raw);
            return (obj && obj.il == currentIl) ? obj.snap : null;
        } catch(e) { return null; }
    }
    function clearUndo() {
        try { localStorage.removeItem(UNDO_KEY); } catch(e) {}
        showUndoBtn(false);
        document.getElementById('bereberBolResult').style.display = 'none';
    }
    function showUndoBtn(show) {
        var wrap = document.getElementById('sirketUndoWrap');
        if (wrap) wrap.style.display = show ? 'block' : 'none';
    }

    // Snapshot çək — bölüşdürmədən əvvəl cari planları yadda saxla
    function snapshotPlans(aylar) {
        if (!cachedData || !cachedData.departamentlar) return [];
        var snap = [];
        cachedData.departamentlar.forEach(function (dept) {
            aylar.forEach(function (ay) {
                var a = dept.aylar.find(function (x) { return x.ay === ay; });
                snap.push({
                    departamentId : dept.departamentId,
                    il            : parseInt(currentIl),
                    ay            : ay,
                    planMebleg    : a ? (Number(a.plan) || 0) : 0
                });
            });
        });
        return snap;
    }

    function bereberBol() {
        var sirketVal = parseFloat(document.getElementById('sirketMeblegInput').value);
        if (isNaN(sirketVal) || sirketVal <= 0) { alert('Evvelce budceni yadda saxlayin.'); return; }

        // İstifadəçinin daxil etdiyi məbləğ — boşsa tam büdcə
        var customVal = parseFloat(document.getElementById('bereberMebleg').value);
        var meblegVal = (!isNaN(customVal) && customVal > 0) ? customVal : sirketVal;

        if (meblegVal > sirketVal) {
            alert('Bölüşdürüləcək məbləğ (' + fmt(meblegVal) + ' ₼) şirkət büdcəsini (' + fmt(sirketVal) + ' ₼) aşa bilməz.');
            return;
        }

        var ayVal    = parseInt(document.getElementById('bereberAy').value) || 0;
        var aylar    = ayVal === 0 ? [1,2,3,4,5,6,7,8,9,10,11,12] : [ayVal];
        var payPerAy = Math.round((meblegVal / aylar.length) * 100) / 100;

        // Əvvəlcə təsdiq göstər
        var tesdiqEl = document.getElementById('bereberTesdiq');
        var msgEl    = document.getElementById('bereberTesdiqMsg');
        var ayLabel  = ayVal === 0 ? 'bütün 12 ay' : ayAdlari[ayVal - 1];
        var deptSay  = cachedData && cachedData.departamentlar ? cachedData.departamentlar.length : '?';

        msgEl.innerHTML = '<strong>' + ayLabel + '</strong> üzrə <strong>' + deptSay +
            '</strong> şöbəyə hər birinə <strong>' + fmt(payPerAy) + ' ₼</strong> paylanacaq.';
        document.getElementById('bereberBolResult').style.display = 'none';
        tesdiqEl.style.display = 'flex';

        // Təsdiq — Bəli
        document.getElementById('btnTesdiqBeli').onclick = function () {
            tesdiqEl.style.display = 'none';

            // Snapshot saxla — localStorage-da da
            undoSnapshot = snapshotPlans(aylar);
            saveUndo(undoSnapshot);

            var btn = document.getElementById('btnBereberBol');
            btn.disabled = true;

            var requests = aylar.map(function (ay) {
                return fetch('/HR/Budce/BereberBol', {
                    method  : 'POST',
                    headers : { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                    body    : JSON.stringify({ il: parseInt(currentIl), ay: ay, mebleg: payPerAy })
                }).then(function (r) { return r.json(); });
            });

            Promise.all(requests)
                .then(function () {
                    var resultEl = document.getElementById('bereberBolResult');
                    var msgBoxEl = document.getElementById('bereberBolMsg');
                    msgBoxEl.textContent = ayLabel + ': hər şöbəyə ' + fmt(payPerAy) + ' ₼ paylandı.';
                    resultEl.style.display = 'flex';
                    loadData();
                })
                .catch(function () { alert('Xeta bas verdi.'); undoSnapshot = null; })
                .finally(function () { btn.disabled = false; });
        };

        // Təsdiq — Xeyr
        document.getElementById('btnTesdiqXeyr').onclick = function () {
            tesdiqEl.style.display = 'none';
        };
    }

    function geriQaytar() {
        // localStorage-dan da yoxla
        if (!undoSnapshot) undoSnapshot = loadUndo();
        if (!undoSnapshot || undoSnapshot.length === 0) {
            alert('Geri qaytarılacaq plan tapılmadı. Yalnız bu seansda edilən son bölüşdürmə geri qaytarıla bilər.');
            return;
        }

        var btn = document.getElementById('btnGeriQaytar');
        btn.disabled = true;
        btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Bərpa edilir...';

        fetch('/HR/Budce/RestorePlans', {
            method  : 'POST',
            headers : { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body    : JSON.stringify(undoSnapshot)
        })
        .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
        .then(function () {
            undoSnapshot = null;
            clearUndo();
            loadData();
        })
        .catch(function () { alert('Bərpa zamanı xeta bas verdi.'); })
        .finally(function () {
            btn.disabled  = false;
            btn.innerHTML = '<i class="bi bi-arrow-counterclockwise"></i> Geri qaytar';
        });
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
    // ── Planları sıfırla ─────────────────────────────────────
    function sifirlaPlans() {
        var deptSay = cachedData && cachedData.departamentlar ? cachedData.departamentlar.length : '?';
        if (!confirm(currentIl + ' ili üçün ' + deptSay + ' şöbənin bütün aylıq plan məbləğləri 0-a çəkiləcək.\n\nDavam etmək istəyirsiniz?')) return;

        var btn = document.getElementById('btnSifirla');
        btn.disabled = true;
        btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Sıfırlanır...';

        fetch('/HR/Budce/SifirlaPlans', {
            method  : 'POST',
            headers : { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
            body    : JSON.stringify({ il: parseInt(currentIl) })
        })
        .then(function (r) { if (!r.ok) throw new Error(); return r.json(); })
        .then(function () { clearUndo(); loadData(); })
        .catch(function () { alert('Xeta bas verdi.'); })
        .finally(function () {
            btn.disabled  = false;
            btn.innerHTML = '<i class="bi bi-trash3"></i> Planları sıfırla';
        });
    }

    document.getElementById('btnSifirla').addEventListener('click', sifirlaPlans);
    document.getElementById('btnBereberBol').addEventListener('click', bereberBol);
    document.getElementById('btnGeriQaytar').addEventListener('click', geriQaytar);
    document.getElementById('btnGeriQaytarPanel').addEventListener('click', geriQaytar);

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

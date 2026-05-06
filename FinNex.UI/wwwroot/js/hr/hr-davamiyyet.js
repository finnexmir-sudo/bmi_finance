document.addEventListener('DOMContentLoaded', function () {

    // ── Endpoint URL-ləri ──────────────────────────────────────────
    // Səhifənin kök elementində `data-endpoint-*` atributu varsa onun dəyəri
    // istifadə olunur (boş dəyər → endpoint deaktiv); yoxdursa default HR
    // Davamiyyet controller-ə yönəlir. Bu yolla eyni JS həm
    // `/HR/Davamiyyet/Index`, həm də `/HR/RehberDashboard/Davamiyyet`
    // səhifələrində işləyir — rəhbərin HR-a girişi olmasa belə.
    var pageEl = document.querySelector('.hrd-page');
    function endpoint(name, defaultUrl) {
        if (pageEl && pageEl.hasAttribute('data-endpoint-' + name)) {
            return pageEl.getAttribute('data-endpoint-' + name);
        }
        return defaultUrl;
    }
    var endpoints = {
        getByTarix:    endpoint('getbytarix',    '/HR/Davamiyyet/GetByTarix'),
        getGozlenilen: endpoint('getgozlenilen', '/HR/Davamiyyet/GetGozlenilen'),
        isciAxtar:     endpoint('isciaxtar',     '/HR/Davamiyyet/IsciAxtar'),
        exportExcel:   endpoint('exportexcel',   '/HR/Davamiyyet/ExportExcel'),
        deviceStatus:  endpoint('devicestatus',  '/HR/ADMSTest/GetRecentLogs'),
        tarixler:      endpoint('tarixler',      '')
    };

    // ── DOM Elements ──
    var tabs = document.querySelectorAll('.hrd-tab');
    var filterTarix = document.getElementById('filterTarix');
    var filterAraliq = document.getElementById('filterAraliq');
    var filterIsci = document.getElementById('filterIsci');

    var inputTarix = document.getElementById('hrdTarix');
    var selectStatus = document.getElementById('hrdStatus');
    var btnAxtar = document.getElementById('hrdAxtar');

    var inputBaslangic = document.getElementById('hrdBaslangic');
    var inputSon = document.getElementById('hrdSon');
    var selectStatusAraliq = document.getElementById('hrdStatusAraliq');
    var btnAraliqAxtar = document.getElementById('hrdAraliqAxtar');

    var inputIsciAxtar = document.getElementById('hrdIsciAxtar');
    var isciResults = document.getElementById('hrdIsciResults');
    var selectedIsciEl = document.getElementById('hrdSelectedIsci');
    var inputIsciBaslangic = document.getElementById('hrdIsciBaslangic');
    var inputIsciSon = document.getElementById('hrdIsciSon');

    var tableBody = document.getElementById('hrdTableBody');
    var recordCount = document.getElementById('hrdRecordCount');
    var btnExport = document.getElementById('hrdExport');
    var extraStats = document.getElementById('hrdExtraStats');
    var enCoxGecikenEl = document.getElementById('hrdEnCoxGeciken');

    var kpiGelib = document.getElementById('kpiGelib');
    var kpiGecikme = document.getElementById('kpiGecikme');
    var kpiQayib = document.getElementById('kpiQayib');
    var kpiIcazeli = document.getElementById('kpiIcazeli');
    var kpiCemi = document.getElementById('kpiCemi');
    var kpiOrtaSaat = document.getElementById('kpiOrtaSaat');
    var kpiXestelik = document.getElementById('kpiXestelik');
    var kpiEzamiyyet = document.getElementById('kpiEzamiyyet');
    var kpiTezCixan = document.getElementById('kpiTezCixan');

    var deviceStatus = document.getElementById('hrdDeviceStatus');
    var deviceText = document.getElementById('hrdDeviceText');

    var selectedIsciId = null;
    var searchTimeout = null;
    var currentParams = {};
    var currentMode = 'tarix';
    var isGozlenilenMode = false;

    // İş parametrləri — default dəyərlər, GetByTarix cavabında yenilənir
    var isParametriData = {
        girisVaxti: '09:00',
        cixisVaxti: '17:45',
        gecikmeTolerans: 5,
        tezCixmaTolerans: 15
    };

    // "HH:MM" → {hours, minutes}
    function parseTime(str) {
        var parts = (str || '00:00').split(':');
        return { hours: parseInt(parts[0], 10), minutes: parseInt(parts[1], 10) };
    }

    // DateTime string → "HH:MM" lokal vaxt
    function toTimeStr(dateStr) {
        var d = new Date(dateStr);
        return pad(d.getHours()) + ':' + pad(d.getMinutes());
    }

    // ── Tab switching ──
    tabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            tabs.forEach(function (t) { t.classList.remove('active'); });
            tab.classList.add('active');
            currentMode = tab.getAttribute('data-mode');
            filterTarix.style.display = currentMode === 'tarix' ? 'flex' : 'none';
            filterAraliq.style.display = currentMode === 'araliq' ? 'flex' : 'none';
            filterIsci.style.display = currentMode === 'isci' ? 'flex' : 'none';
        });
    });

    // ── KPI kart klik — filtr ──
    document.querySelectorAll('.hrd-kpi--clickable').forEach(function (kpi) {
        kpi.style.cursor = 'pointer';
        kpi.addEventListener('click', function () {
            var statusVal = kpi.getAttribute('data-status');
            // Remove active from all
            document.querySelectorAll('.hrd-kpi--clickable').forEach(function (k) { k.classList.remove('hrd-kpi--active'); });

            if (statusVal === 'gozlenilen') {
                kpi.classList.add('hrd-kpi--active');
                loadGozlenilen();
            } else if (statusVal === '') {
                selectStatus.value = '';
                var p = getBaseParams();
                loadData(p);
            } else if (statusVal === '1,2') {
                kpi.classList.add('hrd-kpi--active');
                var p = getBaseParams();
                loadData(p, [1, 2]);
            } else if (statusVal === 'tezCixan') {
                kpi.classList.add('hrd-kpi--active');
                var p = getBaseParams();
                loadData(p, null, true);
            } else {
                kpi.classList.add('hrd-kpi--active');
                selectStatus.value = statusVal;
                var p = getBaseParams();
                p.status = statusVal;
                loadData(p);
            }
        });
    });

    // ── Gözlənilən işçiləri yüklə ──
    function loadGozlenilen() {
        var tarix = inputTarix.value || toLocalDateStr(new Date());
        var url = endpoints.getGozlenilen + '?tarix=' + encodeURIComponent(tarix);

        tableBody.innerHTML = '<tr><td colspan="7"><div class="hrd-empty"><div class="spinner-border spinner-border-sm text-muted"></div><div style="margin-top:8px">Yüklənir...</div></div></td></tr>';
        isGozlenilenMode = true;

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                renderTable(data.records || [], true);
                recordCount.textContent = (data.count || 0) + ' gözlənilən işçi';
                extraStats.style.display = 'none';
            })
            .catch(function (err) {
                console.error('Gözlənilən işçilər yüklənmədi:', err);
                tableBody.innerHTML = '<tr><td colspan="7"><div class="hrd-empty"><i class="bi bi-exclamation-triangle"></i><div>Xəta baş verdi</div></div></td></tr>';
            });
    }

    // ── Qayıb Yaz Modal ──
    var qayibModalEl = document.getElementById('qayibModal');
    var qayibModal = qayibModalEl ? new bootstrap.Modal(qayibModalEl) : null;
    var qayibYazBtn = document.getElementById('qayibYazBtn');

    if (qayibModalEl) {
        document.getElementById('qayibModal').addEventListener('show.bs.modal', function () {
            document.getElementById('qayibSebeb').value = '';
            document.getElementById('qayibMaasdanKes').checked = false;
        });
    }

    if (qayibYazBtn && qayibModal) {
        qayibYazBtn.addEventListener('click', function () {
        var isciId = parseInt(document.getElementById('qayibIsciId').value);
        var tarix = document.getElementById('qayibTarix').value;
        var maasdanKes = document.getElementById('qayibMaasdanKes').checked;
        var sebeb = document.getElementById('qayibSebeb').value.trim();

        qayibYazBtn.disabled = true;
        qayibYazBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saxlanılır...';

        fetch('/HR/Davamiyyet/QayibYaz', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ isciId: isciId, tarix: tarix, maasdanKes: maasdanKes, qayibSebebi: sebeb })
        })
            .then(function (r) { return r.json().then(function (d) { return { ok: r.ok, data: d }; }); })
            .then(function (res) {
                if (res.ok) {
                    qayibModal.hide();
                    var tarix = inputTarix.value || toLocalDateStr(new Date());
                    // KPI-ları yenilə (Qayıb artır)
                    fetch(endpoints.getByTarix + '?tarix=' + encodeURIComponent(tarix))
                        .then(function (r) { return r.json(); })
                        .then(function (data) { updateKPI(data.stats); });
                    // Gözlənilir siyahısını yenilə (işçi çıxır, KPI azalır)
                    fetch(endpoints.getGozlenilen + '?tarix=' + encodeURIComponent(tarix))
                        .then(function (r) { return r.json(); })
                        .then(function (data) {
                            var kpiGoz = document.getElementById('kpiGozlenilen');
                            if (kpiGoz) kpiGoz.textContent = data.count || 0;
                            renderTable(data.records || [], true);
                            recordCount.textContent = (data.count || 0) + ' gözlənilən işçi';
                            extraStats.style.display = 'none';
                        });
                } else {
                    alert(res.data.error || 'Xəta baş verdi.');
                }
            })
            .catch(function () { alert('Şəbəkə xətası.'); })
            .finally(function () {
                qayibYazBtn.disabled = false;
                qayibYazBtn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Qayıb yaz';
            });
        });
    }

    // ── Qayıb Düzəliş Modal ──
    var qayibDuzeltModalEl = document.getElementById('qayibDuzeltModal');
    var qayibDuzeltModal = qayibDuzeltModalEl ? new bootstrap.Modal(qayibDuzeltModalEl) : null;
    var duzeltSaxlaBtn = document.getElementById('duzeltSaxlaBtn');

    if (duzeltSaxlaBtn && qayibDuzeltModal) {
        duzeltSaxlaBtn.addEventListener('click', function () {
        var id = parseInt(document.getElementById('duzeltId').value);
        var maasdanKes = document.getElementById('duzeltMaasdanKes').checked;
        var sebeb = document.getElementById('duzeltSebeb').value.trim();

        duzeltSaxlaBtn.disabled = true;
        duzeltSaxlaBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saxlanılır...';

        fetch('/HR/Davamiyyet/QayibDuzelt', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ id: id, maasdanKes: maasdanKes, qayibSebebi: sebeb })
        })
            .then(function (r) { return r.json().then(function (d) { return { ok: r.ok, data: d }; }); })
            .then(function (res) {
                if (res.ok) {
                    qayibDuzeltModal.hide();
                    // Cədvəli cari parametrlərlə yenilə
                    var p = getBaseParams();
                    loadData(p);
                } else {
                    alert(res.data.error || 'Xəta baş verdi.');
                }
            })
            .catch(function () { alert('Şəbəkə xətası.'); })
            .finally(function () {
                duzeltSaxlaBtn.disabled = false;
                duzeltSaxlaBtn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Saxla';
            });
        });
    }

    // ── Custom tarix seçici (yalnız mövcud tarixlər) ──
    var datepickWrap = document.getElementById('hrdTarixPick');
    if (datepickWrap && endpoints.tarixler) {
        var dpDisplay  = document.getElementById('hrdTarixDisplay');
        var dpText     = document.getElementById('hrdTarixText');
        var dpPanel    = document.getElementById('hrdTarixPanel');
        var dpSearch   = document.getElementById('hrdTarixSearch');
        var dpList     = document.getElementById('hrdTarixList');
        var dpOpen     = false;
        var dpAllDates = [];
        var DAY_AZ     = ['Bazar', 'B.ertəsi', 'Çərt.ertəsi', 'Çərşənbə', 'Cümə.axş.', 'Cümə', 'Şənbə'];

        function dpFormatDisplay(iso) {
            var d = new Date(iso + 'T00:00:00');
            return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear()
                + ' — ' + DAY_AZ[d.getDay()];
        }

        function dpRender(q) {
            var filter = (q || '').trim().toLowerCase();
            var list = filter
                ? dpAllDates.filter(function (iso) {
                    var d = new Date(iso + 'T00:00:00');
                    var txt = pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear();
                    return txt.includes(filter) || iso.includes(filter);
                })
                : dpAllDates;

            if (list.length === 0) {
                dpList.innerHTML = '<div style="padding:14px;text-align:center;color:#94a3b8;font-size:13px;">Nəticə tapılmadı</div>';
                return;
            }
            var cur = inputTarix.value;
            var html = '';
            list.forEach(function (iso) {
                html += '<div class="hrd-datepick-item' + (iso === cur ? ' active' : '') + '" data-iso="' + iso + '">'
                    + dpFormatDisplay(iso) + '</div>';
            });
            dpList.innerHTML = html;
            dpList.querySelectorAll('.hrd-datepick-item').forEach(function (item) {
                item.addEventListener('click', function () {
                    var iso = item.getAttribute('data-iso');
                    inputTarix.value = iso;
                    var d = new Date(iso + 'T00:00:00');
                    dpText.textContent = pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear();
                    dpClose();
                    document.querySelectorAll('.hrd-kpi--clickable').forEach(function (k) { k.classList.remove('hrd-kpi--active'); });
                    var params = { tarix: iso };
                    if (selectStatus.value) params.status = selectStatus.value;
                    loadData(params);
                });
            });
        }

        function dpOpen2() {
            dpPanel.style.display = 'block';
            dpDisplay.classList.add('open');
            dpSearch.value = '';
            dpOpen = true;
            dpSearch.focus();
            if (dpAllDates.length === 0) {
                fetch(endpoints.tarixler)
                    .then(function (r) { return r.json(); })
                    .then(function (data) { dpAllDates = data; dpRender(''); });
            } else {
                dpRender('');
            }
        }

        function dpClose() {
            dpPanel.style.display = 'none';
            dpDisplay.classList.remove('open');
            dpOpen = false;
        }

        dpDisplay.addEventListener('click', function () { dpOpen ? dpClose() : dpOpen2(); });
        dpSearch.addEventListener('input', function () { dpRender(dpSearch.value); });
        dpSearch.addEventListener('keydown', function (e) { if (e.key === 'Escape') dpClose(); });
        document.addEventListener('click', function (e) {
            if (dpOpen && !datepickWrap.contains(e.target)) dpClose();
        });
    }

    // ── Tarixə görə axtarış ──
    btnAxtar.addEventListener('click', function () {
        document.querySelectorAll('.hrd-kpi--clickable').forEach(function (k) { k.classList.remove('hrd-kpi--active'); });
        var params = {};
        if (inputTarix.value) params.tarix = inputTarix.value;
        if (selectStatus.value) params.status = selectStatus.value;
        loadData(params);
    });

    // ── Tarix aralığı axtarışı ──
    btnAraliqAxtar.addEventListener('click', function () {
        document.querySelectorAll('.hrd-kpi--clickable').forEach(function (k) { k.classList.remove('hrd-kpi--active'); });
        var params = {};
        if (inputBaslangic.value) params.baslangic = inputBaslangic.value;
        if (inputSon.value) params.son = inputSon.value;
        if (selectStatusAraliq.value) params.status = selectStatusAraliq.value;
        loadData(params);
    });

    // ── İşçi axtarışı ──
    inputIsciAxtar.addEventListener('input', function () {
        clearTimeout(searchTimeout);
        var q = inputIsciAxtar.value.trim();
        if (q.length < 1) { isciResults.style.display = 'none'; return; }

        searchTimeout = setTimeout(function () {
            fetch(endpoints.isciAxtar + '?q=' + encodeURIComponent(q))
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (data.length === 0) {
                        isciResults.innerHTML = '<div style="padding:14px;color:#94a3b8;text-align:center;font-size:13px">Nəticə tapılmadı</div>';
                    } else {
                        var html = '';
                        data.forEach(function (isci) {
                            var initials = isci.tamAd.substring(0, 2).toUpperCase();
                            html += '<div class="hrd-search-item" data-id="' + isci.id + '" data-name="' + isci.tamAd + '">' +
                                '<div class="hrd-search-av">' + initials + '</div>' +
                                '<div><div class="hrd-search-name">' + isci.tamAd + '</div>' +
                                '<div class="hrd-search-dept">' + isci.sobe + '</div></div></div>';
                        });
                        isciResults.innerHTML = html;

                        isciResults.querySelectorAll('.hrd-search-item').forEach(function (item) {
                            item.addEventListener('click', function () {
                                selectedIsciId = parseInt(item.getAttribute('data-id'));
                                var name = item.getAttribute('data-name');
                                selectedIsciEl.innerHTML =
                                    '<span class="hrd-selected-name">' + name + '</span>' +
                                    '<button class="hrd-selected-clear" id="clearIsci">&times;</button>';
                                document.getElementById('clearIsci').addEventListener('click', clearSelectedIsci);
                                isciResults.style.display = 'none';
                                inputIsciAxtar.value = '';

                                // Load with optional date range
                                var params = { isciId: selectedIsciId };
                                if (inputIsciBaslangic.value) params.baslangic = inputIsciBaslangic.value;
                                if (inputIsciSon.value) params.son = inputIsciSon.value;
                                loadData(params);
                            });
                        });
                    }
                    isciResults.style.display = 'block';
                });
        }, 300);
    });

    document.addEventListener('click', function (e) {
        if (!inputIsciAxtar.contains(e.target) && !isciResults.contains(e.target)) {
            isciResults.style.display = 'none';
        }
    });

    function clearSelectedIsci() {
        selectedIsciId = null;
        selectedIsciEl.innerHTML = '<span class="hrd-no-selection">Heç kim seçilməyib</span>';
        loadData({ tarix: inputTarix.value || toLocalDateStr(new Date()) });
    }

    function getBaseParams() {
        if (currentMode === 'araliq') {
            var p = {};
            if (inputBaslangic.value) p.baslangic = inputBaslangic.value;
            if (inputSon.value) p.son = inputSon.value;
            return p;
        } else if (currentMode === 'isci' && selectedIsciId) {
            var p = { isciId: selectedIsciId };
            if (inputIsciBaslangic.value) p.baslangic = inputIsciBaslangic.value;
            if (inputIsciSon.value) p.son = inputIsciSon.value;
            return p;
        }
        return { tarix: inputTarix.value || toLocalDateStr(new Date()) };
    }

    // ── Excel Export ──
    btnExport.addEventListener('click', function () {
        var params = new URLSearchParams(currentParams).toString();
        window.location.href = endpoints.exportExcel + '?' + params;
    });

    // ── Data loading ──
    // clientFilterStatuses: array of status ints (e.g. [1,2]) or null
    // filterTezCixan: if true, show only records where cixisVaxti < effective end - tolerans
    function loadData(params, clientFilterStatuses, filterTezCixan) {
        isGozlenilenMode = false;
        currentParams = params;
        var url = endpoints.getByTarix + '?' + new URLSearchParams(params).toString();

        tableBody.innerHTML = '<tr><td colspan="7"><div class="hrd-empty"><div class="spinner-border spinner-border-sm text-muted"></div><div style="margin-top:8px">Yüklənir...</div></div></td></tr>';

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.isParametri) {
                    isParametriData = data.isParametri;
                }
                updateKPI(data.stats);

                var records = data.records;
                if (clientFilterStatuses && clientFilterStatuses.length > 0) {
                    records = records.filter(function (r) { return clientFilterStatuses.indexOf(r.status) >= 0; });
                }
                if (filterTezCixan) {
                    var cp = parseTime(isParametriData.cixisVaxti);
                    var cixisTotal = cp.hours * 60 + cp.minutes;
                    var hedd = cixisTotal - (isParametriData.tezCixmaTolerans || 15);
                    records = records.filter(function (r) {
                        if (!r.cixisVaxti) return false;
                        var d = new Date(r.cixisVaxti);
                        var dTotal = d.getHours() * 60 + d.getMinutes();
                        return dTotal < hedd;
                    });
                }

                renderTable(records, false);
                recordCount.textContent = records.length + ' qeyd';

                if (data.stats.enCoxGecikenDept && data.stats.enCoxGecikenDeptSay > 0) {
                    enCoxGecikenEl.textContent = data.stats.enCoxGecikenDept + ' (' + data.stats.enCoxGecikenDeptSay + ' nəfər)';
                    extraStats.style.display = 'flex';
                } else {
                    extraStats.style.display = 'none';
                }
            })
            .catch(function (err) {
                console.error('Davamiyyat yuklanmadi:', err);
                tableBody.innerHTML = '<tr><td colspan="7"><div class="hrd-empty"><i class="bi bi-exclamation-triangle"></i><div>Xeta bas verdi</div></div></td></tr>';
            });
    }

    function updateKPI(stats) {
        if (kpiGelib) kpiGelib.textContent = stats.gelib ?? 0;
        if (kpiGecikme) kpiGecikme.textContent = stats.gecikme ?? 0;
        if (kpiQayib) kpiQayib.textContent = stats.qayib ?? 0;
        if (kpiIcazeli) kpiIcazeli.textContent = stats.icazeli ?? 0;
        if (kpiXestelik) kpiXestelik.textContent = stats.xestelik ?? 0;
        if (kpiEzamiyyet) kpiEzamiyyet.textContent = stats.ezamiyyet ?? 0;
        if (kpiTezCixan) kpiTezCixan.textContent = stats.tezCixan ?? 0;
        if (kpiCemi) kpiCemi.textContent = stats.cemi ?? 0;
        if (kpiOrtaSaat) kpiOrtaSaat.textContent = stats.ortaIsSaati ?? 0;
    }

    function renderTable(records, showQayibBtn) {
        if (records.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="' + (showQayibBtn ? 8 : 7) + '"><div class="hrd-empty"><i class="bi bi-inbox"></i><div>Heç bir davamiyyət qeydi tapılmadı</div></div></td></tr>';
            return;
        }

        // Başlıq — Qayıb Yaz sütunu varsa əlavə et
        var thead = document.querySelector('.hrd-table thead tr');
        if (showQayibBtn) {
            if (!document.getElementById('thQayibYaz')) {
                var th = document.createElement('th');
                th.id = 'thQayibYaz';
                th.textContent = 'Əməliyyat';
                thead.appendChild(th);
            }
        } else {
            var existing = document.getElementById('thQayibYaz');
            if (existing) existing.remove();
        }

        var html = '';
        records.forEach(function (r) {
            var initials = r.isciTamAd.substring(0, 2).toUpperCase();
            var tarix = formatDate(r.tarix);
            var giris = r.girisVaxti ? formatTime(r.girisVaxti) : '<span class="hrd-nodata">--:--</span>';
            var cixis = r.cixisVaxti ? formatTime(r.cixisVaxti) : '<span class="hrd-nodata">--:--</span>';

            var girisClass = 'hrd-time';
            if (r.girisVaxti) {
                var gt = new Date(r.girisVaxti);
                var gp = parseTime(isParametriData.girisVaxti);
                var gecTolerans = isParametriData.gecikmeTolerans || 5;
                var hedefDeq = gp.hours * 60 + gp.minutes + gecTolerans;
                var girisDaq = gt.getHours() * 60 + gt.getMinutes();
                if (girisDaq > hedefDeq) {
                    girisClass = 'hrd-time hrd-time--late';
                }
            }

            // Tez çıxma qeyd — çıxış vaxtı var VƏ erkəndir
            var cixisClass = 'hrd-time';
            if (r.cixisVaxti) {
                var ct = new Date(r.cixisVaxti);
                var cp2 = parseTime(isParametriData.cixisVaxti);
                var tezTolerans = isParametriData.tezCixmaTolerans || 15;
                var cixisHedd = cp2.hours * 60 + cp2.minutes - tezTolerans;
                var cixisDaq = ct.getHours() * 60 + ct.getMinutes();
                if (cixisDaq < cixisHedd) {
                    cixisClass = 'hrd-time hrd-time--early';
                }
            }

            var duration = '<span class="hrd-nodata">---</span>';
            if (r.girisVaxti && r.cixisVaxti) {
                var diff = new Date(r.cixisVaxti) - new Date(r.girisVaxti);
                var h = Math.floor(diff / 3600000);
                var m = Math.floor((diff % 3600000) / 60000);
                var durCls = h < 8 ? 'hrd-dur hrd-dur--short' : 'hrd-dur hrd-dur--ok';
                duration = '<span class="' + durCls + '">' + h + ' s ' + m + ' d</span>';
            }

            var badge = getStatusBadge(r.status);
            var tarixRaw = r.tarix ? toLocalDateStr(r.tarix) : toLocalDateStr(new Date());

            // Qayıb sıralarında kəsinti indikatoru + düzəliş + sil düymələri
            var qayibExtra = '';
            if (r.status === 3) {
                var kesIcon = r.maasdanKes
                    ? '<span title="Maaşdan kəsilir" style="color:#dc2626;font-size:11px;margin-left:6px;"><i class="bi bi-scissors"></i> Kəsilir</span>'
                    : '<span title="Maaşdan kəsilmir" style="color:#94a3b8;font-size:11px;margin-left:6px;"><i class="bi bi-dash-circle"></i> Kəsilmir</span>';
                badge += kesIcon +
                    '<button class="btn btn-sm qayib-duzelt-btn" ' +
                    'data-id="' + r.id + '" ' +
                    'data-isci-ad="' + r.isciTamAd + '" ' +
                    'data-tarix-raw="' + tarixRaw + '" ' +
                    'data-maasdan-kes="' + (r.maasdanKes ? '1' : '0') + '" ' +
                    'data-sebeb="' + (r.qayibSebebi || '') + '" ' +
                    'style="font-size:11px;padding:2px 7px;margin-left:6px;border:1px solid #6366f1;color:#6366f1;border-radius:5px;" ' +
                    'title="Düzəliş et"><i class="bi bi-pencil"></i></button>' +
                    '<button class="btn btn-sm qayib-sil-btn" ' +
                    'data-id="' + r.id + '" ' +
                    'data-isci-ad="' + r.isciTamAd + '" ' +
                    'style="font-size:11px;padding:2px 7px;margin-left:4px;border:1px solid #dc2626;color:#dc2626;border-radius:5px;" ' +
                    'title="Sil"><i class="bi bi-trash"></i></button>';
            }

            var actionCell = '';
            if (showQayibBtn && r.status === 0) {
                actionCell = '<td><button class="btn btn-sm btn-outline-danger qayib-yaz-btn" ' +
                    'data-isci-id="' + r.isciId + '" ' +
                    'data-isci-ad="' + r.isciTamAd + '" ' +
                    'data-tarix="' + tarixRaw + '" ' +
                    'style="font-size:12px;border-radius:6px;padding:4px 10px;">' +
                    '<i class="bi bi-x-circle me-1"></i>Qayıb yaz</button></td>';
            } else if (showQayibBtn) {
                actionCell = '<td></td>';
            }

            html += '<tr>' +
                '<td><div class="hrd-emp"><div class="hrd-emp-av">' + initials + '</div><div class="hrd-emp-name">' + r.isciTamAd + '</div></div></td>' +
                '<td><span class="hrd-dept">' + r.departamentAd + '</span></td>' +
                '<td><div class="hrd-date">' + tarix + '</div></td>' +
                '<td><span class="' + girisClass + '">' + giris + '</span></td>' +
                '<td><span class="' + cixisClass + '">' + cixis + '</span></td>' +
                '<td>' + duration + '</td>' +
                '<td>' + badge + '</td>' +
                actionCell +
                '</tr>';
        });
        tableBody.innerHTML = html;

        // "Qayıb yaz" düymələrinin klik hadisəsi
        tableBody.querySelectorAll('.qayib-yaz-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                document.getElementById('qayibIsciId').value = btn.getAttribute('data-isci-id');
                document.getElementById('qayibTarix').value = btn.getAttribute('data-tarix');
                document.getElementById('qayibIsciAd').textContent = btn.getAttribute('data-isci-ad');
                var d = new Date(btn.getAttribute('data-tarix'));
                document.getElementById('qayibTarixGoster').textContent =
                    pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear();
                qayibModal.show();
            });
        });

        // "Düzəliş et" düymələrinin klik hadisəsi
        tableBody.querySelectorAll('.qayib-duzelt-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                document.getElementById('duzeltId').value = btn.getAttribute('data-id');
                document.getElementById('duzeltIsciAd').textContent = btn.getAttribute('data-isci-ad');
                document.getElementById('duzeltMaasdanKes').checked = btn.getAttribute('data-maasdan-kes') === '1';
                document.getElementById('duzeltSebeb').value = btn.getAttribute('data-sebeb') || '';
                var d = new Date(btn.getAttribute('data-tarix-raw'));
                document.getElementById('duzeltTarixGoster').textContent =
                    pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear();
                qayibDuzeltModal.show();
            });
        });

        // "Sil" düymələrinin klik hadisəsi
        tableBody.querySelectorAll('.qayib-sil-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var isciAd = btn.getAttribute('data-isci-ad');
                if (!confirm(isciAd + ' üçün qayıb qeydini silmək istədiyinizə əminsiniz?')) return;

                var id = parseInt(btn.getAttribute('data-id'));
                btn.disabled = true;

                fetch('/HR/Davamiyyet/QayibSil', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: id })
                })
                    .then(function (r) { return r.json().then(function (d) { return { ok: r.ok, data: d }; }); })
                    .then(function (res) {
                        if (res.ok) {
                            var p = getBaseParams();
                            loadData(p);
                            // Qayıb silindi → həmin işçi gözlənilənə qayıdır, KPI-ı yenilə
                            var tarix = inputTarix.value || toLocalDateStr(new Date());
                            fetch(endpoints.getGozlenilen + '?tarix=' + encodeURIComponent(tarix))
                                .then(function (r) { return r.json(); })
                                .then(function (data) {
                                    var kpiGoz = document.getElementById('kpiGozlenilen');
                                    if (kpiGoz) kpiGoz.textContent = data.count || 0;
                                });
                        } else {
                            alert(res.data.error || 'Xəta baş verdi.');
                            btn.disabled = false;
                        }
                    })
                    .catch(function () {
                        alert('Şəbəkə xətası.');
                        btn.disabled = false;
                    });
            });
        });
    }

    function getStatusBadge(status) {
        switch (status) {
            case 0: return '<span class="hrd-badge" style="background:rgba(99,102,241,.1);color:#6366f1;"><span class="hrd-badge-dot" style="background:#6366f1;"></span>Gözlənilir</span>';
            case 1: return '<span class="hrd-badge hrd-badge--isde"><span class="hrd-badge-dot"></span>İşdə</span>';
            case 2: return '<span class="hrd-badge hrd-badge--gecikme"><span class="hrd-badge-dot"></span>Gecikmə</span>';
            case 3: return '<span class="hrd-badge hrd-badge--qayib"><span class="hrd-badge-dot"></span>Qayıb</span>';
            case 4: return '<span class="hrd-badge hrd-badge--icazeli"><span class="hrd-badge-dot"></span>İcazəli</span>';
            case 5: return '<span class="hrd-badge hrd-badge--xestelik" style="background:rgba(168,85,247,.1);color:#a855f7;"><span class="hrd-badge-dot" style="background:#a855f7;"></span>Xəstəlik</span>';
            case 6: return '<span class="hrd-badge hrd-badge--ezamiyyet" style="background:rgba(249,115,22,.1);color:#f97316;"><span class="hrd-badge-dot" style="background:#f97316;"></span>Ezamiyyət</span>';
            default: return '<span class="hrd-badge">Naməlum</span>';
        }
    }

    function formatDate(dateStr) {
        var d = new Date(dateStr);
        return pad(d.getDate()) + '.' + pad(d.getMonth() + 1) + '.' + d.getFullYear();
    }

    function formatTime(dateStr) {
        var d = new Date(dateStr);
        return pad(d.getHours()) + ':' + pad(d.getMinutes());
    }

    function pad(n) { return n < 10 ? '0' + n : '' + n; }

    // toISOString() UTC-yə çevirir — UTC+4-də tarix 1 gün geri düşür.
    // Bu funksiya lokal tarix komponentlərindən yyyy-MM-dd düzəldir.
    function toLocalDateStr(date) {
        var d = (typeof date === 'string') ? new Date(date) : date;
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
    }

    // ── Device status check ──
    function checkDevice() {
        // Cihaz status — endpoint və ya UI elementi yoxdursa keç
        if (!endpoints.deviceStatus || !deviceStatus || !deviceText) return;
        fetch(endpoints.deviceStatus)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.isOnline) {
                    deviceStatus.classList.add('online');
                    deviceText.textContent = 'Cihaz onlayn · ' + (data.lastContact || '');
                } else {
                    deviceStatus.classList.remove('online');
                    deviceText.textContent = 'Cihaz oflayn';
                }
            })
            .catch(function () {
                deviceStatus.classList.remove('online');
                deviceText.textContent = 'Cihaz statusu bilinmir';
            });
    }

    if (deviceStatus && deviceText) {
        checkDevice();
        setInterval(checkDevice, 30000);
    }

    // ── İş Parametrləri Modal ────────────────────────────────────
    var isParametriModal = document.getElementById('isParametriModal');
    var btnIsParametri = document.getElementById('btnIsParametri');

    function openIsParametriModal() {
        var ipGiris = document.getElementById('ipGirisVaxti');
        var ipCixis = document.getElementById('ipCixisVaxti');
        var ipGec = document.getElementById('ipGecikmeTolerans');
        var ipTez = document.getElementById('ipTezCixmaTolerans');
        var msg = document.getElementById('isParametriMsg');
        if (ipGiris) ipGiris.value = isParametriData.girisVaxti || '09:00';
        if (ipCixis) ipCixis.value = isParametriData.cixisVaxti || '17:45';
        if (ipGec) ipGec.value = isParametriData.gecikmeTolerans ?? 5;
        if (ipTez) ipTez.value = isParametriData.tezCixmaTolerans ?? 15;
        if (msg) { msg.style.display = 'none'; msg.textContent = ''; }
        if (isParametriModal) isParametriModal.style.display = 'flex';
    }

    function closeIsParametriModal() {
        if (isParametriModal) isParametriModal.style.display = 'none';
    }

    if (btnIsParametri) {
        btnIsParametri.addEventListener('click', openIsParametriModal);
    }
    var ipCloseBtn = document.getElementById('isParametriClose');
    var ipCancelBtn = document.getElementById('isParametriCancel');
    if (ipCloseBtn) ipCloseBtn.addEventListener('click', closeIsParametriModal);
    if (ipCancelBtn) ipCancelBtn.addEventListener('click', closeIsParametriModal);
    if (isParametriModal) {
        isParametriModal.addEventListener('click', function (e) {
            if (e.target === isParametriModal) closeIsParametriModal();
        });
    }

    var ipSaveBtn = document.getElementById('isParametriSave');
    if (ipSaveBtn) {
        ipSaveBtn.addEventListener('click', function () {
            var giris = (document.getElementById('ipGirisVaxti') || {}).value || '09:00';
            var cixis = (document.getElementById('ipCixisVaxti') || {}).value || '17:45';
            var gecTol = parseInt((document.getElementById('ipGecikmeTolerans') || {}).value || '5', 10);
            var tezTol = parseInt((document.getElementById('ipTezCixmaTolerans') || {}).value || '15', 10);
            var msg = document.getElementById('isParametriMsg');

            ipSaveBtn.disabled = true;
            ipSaveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saxlanilir...';

            fetch('/HR/Davamiyyet/SaveIsParametri', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ girisVaxti: giris, cixisVaxti: cixis, gecikmeTolerans: gecTol, tezCixmaTolerans: tezTol })
            })
                .then(function (r) { return r.json().then(function (d) { return { ok: r.ok, data: d }; }); })
                .then(function (res) {
                    if (res.ok) {
                        isParametriData.girisVaxti = giris;
                        isParametriData.cixisVaxti = cixis;
                        isParametriData.gecikmeTolerans = gecTol;
                        isParametriData.tezCixmaTolerans = tezTol;
                        if (msg) {
                            msg.style.display = 'block';
                            msg.style.background = '#f0fdf4';
                            msg.style.border = '1px solid #bbf7d0';
                            msg.style.color = '#16a34a';
                            msg.textContent = res.data.message || 'Yadda saxlandi.';
                        }
                        // Reload current data with new thresholds
                        var p = getBaseParams();
                        loadData(p);
                        setTimeout(closeIsParametriModal, 1200);
                    } else {
                        if (msg) {
                            msg.style.display = 'block';
                            msg.style.background = '#fef2f2';
                            msg.style.border = '1px solid #fecaca';
                            msg.style.color = '#dc2626';
                            msg.textContent = res.data.error || 'Xeta bas verdi.';
                        }
                    }
                })
                .catch(function () {
                    if (msg) {
                        msg.style.display = 'block';
                        msg.style.color = '#dc2626';
                        msg.textContent = 'Sebeke xetasi.';
                    }
                })
                .finally(function () {
                    ipSaveBtn.disabled = false;
                    ipSaveBtn.innerHTML = '<i class="bi bi-save"></i> Yadda saxla';
                });
        });
    }

    // Fetch IsParametri on page load to get current settings before first data load
    fetch('/HR/Davamiyyet/GetIsParametri')
        .then(function (r) { return r.json(); })
        .then(function (d) {
            isParametriData = {
                girisVaxti: d.girisVaxti || '09:00',
                cixisVaxti: d.cixisVaxti || '17:45',
                gecikmeTolerans: d.gecikmeTolerans ?? 5,
                tezCixmaTolerans: d.tezCixmaTolerans ?? 15
            };
        });

    // Səhifə ilk açılanda JS render ilə yüklə ki, redaktə/sil düymələri görünsün
    loadData({ tarix: inputTarix.value || toLocalDateStr(new Date()) });
    fetch(endpoints.getGozlenilen + '?tarix=' + encodeURIComponent(inputTarix.value || toLocalDateStr(new Date())))
        .then(function (r) { return r.json(); })
        .then(function (data) {
            var kpiGoz = document.getElementById('kpiGozlenilen');
            if (kpiGoz) kpiGoz.textContent = data.count || 0;
        });
});

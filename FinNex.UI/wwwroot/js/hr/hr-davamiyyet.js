document.addEventListener('DOMContentLoaded', function () {

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

    var tableBody = document.getElementById('hrdTableBody');
    var recordCount = document.getElementById('hrdRecordCount');

    // KPI elements
    var kpiIsde = document.getElementById('kpiIsde');
    var kpiGecikme = document.getElementById('kpiGecikme');
    var kpiQayib = document.getElementById('kpiQayib');
    var kpiIcazeli = document.getElementById('kpiIcazeli');
    var kpiCemi = document.getElementById('kpiCemi');

    var deviceStatus = document.getElementById('hrdDeviceStatus');
    var deviceText = document.getElementById('hrdDeviceText');

    var selectedIsciId = null;
    var searchTimeout = null;

    // ── Tab switching ──
    tabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            tabs.forEach(function (t) { t.classList.remove('active'); });
            tab.classList.add('active');

            var mode = tab.getAttribute('data-mode');
            filterTarix.style.display = mode === 'tarix' ? 'flex' : 'none';
            filterAraliq.style.display = mode === 'araliq' ? 'flex' : 'none';
            filterIsci.style.display = mode === 'isci' ? 'flex' : 'none';
        });
    });

    // ── Tarixə görə axtarış ──
    btnAxtar.addEventListener('click', function () {
        var params = {};
        if (inputTarix.value) params.tarix = inputTarix.value;
        if (selectStatus.value) params.status = selectStatus.value;
        loadData(params);
    });

    // ── Tarix aralığı axtarışı ──
    btnAraliqAxtar.addEventListener('click', function () {
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

        if (q.length < 2) {
            isciResults.style.display = 'none';
            return;
        }

        searchTimeout = setTimeout(function () {
            fetch('/HR/Davamiyyet/IsciAxtar?q=' + encodeURIComponent(q))
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

                        // Click handlers
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

                                // Load this employee's data
                                loadData({ isciId: selectedIsciId });
                            });
                        });
                    }
                    isciResults.style.display = 'block';
                });
        }, 300);
    });

    // Close dropdown on outside click
    document.addEventListener('click', function (e) {
        if (!inputIsciAxtar.contains(e.target) && !isciResults.contains(e.target)) {
            isciResults.style.display = 'none';
        }
    });

    function clearSelectedIsci() {
        selectedIsciId = null;
        selectedIsciEl.innerHTML = '<span class="hrd-no-selection">Heç kim seçilməyib</span>';
        loadData({ tarix: inputTarix.value || new Date().toISOString().split('T')[0] });
    }

    // ── Data loading ──
    function loadData(params) {
        var url = '/HR/Davamiyyet/GetByTarix?' + new URLSearchParams(params).toString();

        tableBody.innerHTML = '<tr><td colspan="7"><div class="hrd-empty"><div class="spinner-border spinner-border-sm text-muted" role="status"></div><div style="margin-top:8px">Yüklənir...</div></div></td></tr>';

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                updateKPI(data.stats);
                renderTable(data.records);
                recordCount.textContent = data.records.length + ' qeyd';
            })
            .catch(function () {
                tableBody.innerHTML = '<tr><td colspan="7"><div class="hrd-empty"><i class="bi bi-exclamation-triangle"></i><div>Xəta baş verdi</div></div></td></tr>';
            });
    }

    function updateKPI(stats) {
        kpiIsde.textContent = stats.isde;
        kpiGecikme.textContent = stats.gecikme;
        kpiQayib.textContent = stats.qayib;
        kpiIcazeli.textContent = stats.icazeli;
        kpiCemi.textContent = stats.cemi;
    }

    function renderTable(records) {
        if (records.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="7"><div class="hrd-empty"><i class="bi bi-inbox"></i><div>Heç bir davamiyyət qeydi tapılmadı</div></div></td></tr>';
            return;
        }

        var html = '';
        records.forEach(function (r) {
            var initials = r.isciTamAd.substring(0, 2).toUpperCase();
            var tarix = formatDate(r.tarix);

            var giris = r.girisVaxti ? formatTime(r.girisVaxti) : '<span class="hrd-nodata">--:--</span>';
            var cixis = r.cixisVaxti ? formatTime(r.cixisVaxti) : '<span class="hrd-nodata">--:--</span>';

            // Late check
            var girisClass = 'hrd-time';
            if (r.girisVaxti) {
                var gt = new Date(r.girisVaxti);
                if (gt.getHours() > 9 || (gt.getHours() === 9 && gt.getMinutes() > 5)) {
                    girisClass = 'hrd-time hrd-time--late';
                }
            }

            // Duration
            var duration = '<span class="hrd-nodata">---</span>';
            if (r.girisVaxti && r.cixisVaxti) {
                var diff = new Date(r.cixisVaxti) - new Date(r.girisVaxti);
                var h = Math.floor(diff / 3600000);
                var m = Math.floor((diff % 3600000) / 60000);
                var durCls = h < 8 ? 'hrd-dur hrd-dur--short' : 'hrd-dur hrd-dur--ok';
                duration = '<span class="' + durCls + '">' + h + ' s ' + m + ' d</span>';
            }

            var badge = getStatusBadge(r.status);

            html += '<tr>' +
                '<td><div class="hrd-emp"><div class="hrd-emp-av">' + initials + '</div><div class="hrd-emp-name">' + r.isciTamAd + '</div></div></td>' +
                '<td><span class="hrd-dept">' + r.departamentAd + '</span></td>' +
                '<td><div class="hrd-date">' + tarix + '</div></td>' +
                '<td><span class="' + girisClass + '">' + giris + '</span></td>' +
                '<td><span class="hrd-time">' + cixis + '</span></td>' +
                '<td>' + duration + '</td>' +
                '<td>' + badge + '</td>' +
                '</tr>';
        });

        tableBody.innerHTML = html;
    }

    function getStatusBadge(status) {
        switch (status) {
            case 1: return '<span class="hrd-badge hrd-badge--isde"><span class="hrd-badge-dot"></span>İşdə</span>';
            case 2: return '<span class="hrd-badge hrd-badge--gecikme"><span class="hrd-badge-dot"></span>Gecikmə</span>';
            case 3: return '<span class="hrd-badge hrd-badge--qayib"><span class="hrd-badge-dot"></span>Qayıb</span>';
            case 4: return '<span class="hrd-badge hrd-badge--icazeli"><span class="hrd-badge-dot"></span>İcazəli</span>';
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

    function pad(n) {
        return n < 10 ? '0' + n : '' + n;
    }

    // ── Device status check ──
    function checkDevice() {
        fetch('/HR/ADMSTest/GetRecentLogs')
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

    checkDevice();
    setInterval(checkDevice, 30000);

});

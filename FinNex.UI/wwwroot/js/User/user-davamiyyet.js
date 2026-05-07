document.addEventListener('DOMContentLoaded', function () {

    var inputBaslangic = document.getElementById('udBaslangic');
    var inputSon = document.getElementById('udSon');
    var selectStatus = document.getElementById('udStatus');
    var btnAxtar = document.getElementById('udAxtar');

    var tableBody = document.getElementById('udTableBody');
    var recordCount = document.getElementById('udRecordCount');

    var kpiIsde = document.getElementById('kpiIsde');
    var kpiGecikme = document.getElementById('kpiGecikme');
    var kpiQayib = document.getElementById('kpiQayib');
    var kpiIcazeli = document.getElementById('kpiIcazeli');
    var kpiTezCixan = document.getElementById('kpiTezCixan');
    var kpiCixisYox = document.getElementById('kpiCixisYox');
    var kpiCemi = document.getElementById('kpiCemi');

    var intizamSection = document.getElementById('udIntizamSection');
    var intizamChips = document.getElementById('udIntizamChips');

    var ip = window.isParametriData || { girisVaxti: '09:00', cixisVaxti: '17:45', gecikmeTolerans: 5, tezCixmaTolerans: 15 };

    // Default dates to today
    var todayStr = new Date().toISOString().split('T')[0];
    inputBaslangic.value = todayStr;
    inputSon.value = todayStr;

    // Show intizam section on initial load
    if (window.udInitStats) {
        updateIntizamSection(window.udInitStats.gecikme, window.udInitStats.tezCixan, window.udInitStats.qayib, window.udInitStats.cixisYox);
    }

    // KPI card click handlers
    document.querySelectorAll('.ud-kpi--clickable').forEach(function (card) {
        card.addEventListener('click', function () {
            var filterVal = card.getAttribute('data-filter');
            selectStatus.value = filterVal;
            // Clear date range to show full year
            inputBaslangic.value = '';
            inputSon.value = '';
            setActiveKpi(card);
            var isTezCixan = filterVal === 'tezCixan';
            var isCixisYox = filterVal === 'cixisYox';
            var params = {};
            if (!isTezCixan && !isCixisYox) params.status = filterVal;
            loadData(params, isTezCixan, isCixisYox);
        });
    });

    btnAxtar.addEventListener('click', function () {
        var statusVal = selectStatus.value;
        var isTezCixan = statusVal === 'tezCixan';
        var isCixisYox = statusVal === 'cixisYox';

        var params = {};
        if (inputBaslangic.value) params.baslangic = inputBaslangic.value;
        if (inputSon.value) params.son = inputSon.value;
        if (statusVal && !isTezCixan && !isCixisYox) params.status = statusVal;

        setActiveKpi(null);
        loadData(params, isTezCixan, isCixisYox);
    });

    function setActiveKpi(activeCard) {
        document.querySelectorAll('.ud-kpi--clickable').forEach(function (c) {
            c.classList.remove('ud-kpi--active');
        });
        if (activeCard) activeCard.classList.add('ud-kpi--active');
    }

    function loadData(params, filterTezCixan, filterCixisYox) {
        var url = '/User/Davamiyyet/GetMyRecords?' + new URLSearchParams(params).toString();

        tableBody.innerHTML = '<tr><td colspan="5"><div class="ud-empty"><div class="spinner-border spinner-border-sm text-muted" role="status"></div><div style="margin-top:8px">Yüklənir...</div></div></td></tr>';

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!data || !data.stats) return;

                var records = data.records;

                if (filterTezCixan) {
                    records = applyTezCixanFilter(records);
                } else if (filterCixisYox) {
                    records = records.filter(function (r) { return r.girisVaxti && !r.cixisVaxti; });
                }

                updateKPI(data.stats);
                renderTable(records);
                recordCount.textContent = records.length + ' qeyd';
                updateIntizamSection(data.stats.gecikme || 0, data.stats.tezCixan || 0, data.stats.qayib || 0, data.stats.cixisYox || 0);
            })
            .catch(function () {
                tableBody.innerHTML = '<tr><td colspan="5"><div class="ud-empty"><i class="bi bi-exclamation-triangle"></i><div>Xəta baş verdi</div></div></td></tr>';
            });
    }

    function applyTezCixanFilter(records) {
        var cp = parseTime(ip.cixisVaxti);
        var hedd = (cp.h * 60 + cp.m) - (ip.tezCixmaTolerans || 15);
        return records.filter(function (r) {
            if (!r.cixisVaxti) return false;
            var d = new Date(r.cixisVaxti);
            return (d.getHours() * 60 + d.getMinutes()) < hedd;
        });
    }

    function parseTime(str) {
        if (!str) return { h: 17, m: 45 };
        var parts = str.split(':');
        return { h: parseInt(parts[0]) || 0, m: parseInt(parts[1]) || 0 };
    }

    function updateKPI(stats) {
        kpiIsde.textContent = stats.isde;
        kpiGecikme.textContent = stats.gecikme;
        kpiQayib.textContent = stats.qayib;
        kpiIcazeli.textContent = stats.icazeli;
        if (kpiTezCixan) kpiTezCixan.textContent = stats.tezCixan || 0;
        if (kpiCixisYox) kpiCixisYox.textContent = stats.cixisYox || 0;
        kpiCemi.textContent = stats.cemi;
    }

    function updateIntizamSection(gecikme, tezCixan, qayib, cixisYox) {
        var total = gecikme + tezCixan + qayib + (cixisYox || 0);
        if (total === 0) {
            intizamSection.style.display = 'none';
            return;
        }
        var chips = '';
        if (gecikme > 0) chips += '<span class="ud-intizam-chip ud-intizam-chip--gecikme"><i class="bi bi-clock-fill"></i> Gecikmə: ' + gecikme + '</span>';
        if (tezCixan > 0) chips += '<span class="ud-intizam-chip ud-intizam-chip--tez"><i class="bi bi-box-arrow-right"></i> Erkən çıxış: ' + tezCixan + '</span>';
        if (cixisYox > 0) chips += '<span class="ud-intizam-chip ud-intizam-chip--slate"><i class="bi bi-door-open"></i> Çıxış yoxdur: ' + cixisYox + '</span>';
        if (qayib > 0) chips += '<span class="ud-intizam-chip ud-intizam-chip--qayib"><i class="bi bi-x-circle-fill"></i> Qayıb: ' + qayib + '</span>';
        intizamChips.innerHTML = chips;
        intizamSection.style.display = '';
    }

    function renderTable(records) {
        if (records.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="5"><div class="ud-empty"><i class="bi bi-inbox"></i><div>Heç bir davamiyyət qeydi tapılmadı</div></div></td></tr>';
            return;
        }

        var giris = parseTime(ip.girisVaxti);
        var lateThreshMin = giris.h * 60 + giris.m + (ip.gecikmeTolerans || 5);
        var cixis = parseTime(ip.cixisVaxti);
        var earlyThreshMin = cixis.h * 60 + cixis.m - (ip.tezCixmaTolerans || 15);

        var html = '';
        records.forEach(function (r) {
            var tarix = formatDate(r.tarix);

            var girisHtml = '<span class="ud-nodata">--:--</span>';
            if (r.girisVaxti) {
                var gt = new Date(r.girisVaxti);
                var gtMin = gt.getHours() * 60 + gt.getMinutes();
                var lateCls = gtMin > lateThreshMin ? ' ud-time--late' : '';
                girisHtml = '<span class="ud-time' + lateCls + '">' + formatTime(r.girisVaxti) + '</span>';
            }

            var cixisHtml = '<span class="ud-nodata">--:--</span>';
            if (r.cixisVaxti) {
                var ct = new Date(r.cixisVaxti);
                var ctMin = ct.getHours() * 60 + ct.getMinutes();
                var earlyCls = ctMin < earlyThreshMin ? ' ud-time--early' : '';
                cixisHtml = '<span class="ud-time' + earlyCls + '">' + formatTime(r.cixisVaxti) + '</span>';
            }

            var duration = '<span class="ud-nodata">---</span>';
            if (r.girisVaxti && r.cixisVaxti) {
                var diff = new Date(r.cixisVaxti) - new Date(r.girisVaxti);
                var h = Math.floor(diff / 3600000);
                var m = Math.floor((diff % 3600000) / 60000);
                var durCls = h < 8 ? 'ud-dur ud-dur--short' : 'ud-dur ud-dur--ok';
                duration = '<span class="' + durCls + '">' + h + ' s ' + m + ' d</span>';
            }

            var badge = getStatusBadge(r.status);

            html += '<tr>' +
                '<td><div class="ud-date">' + tarix + '</div></td>' +
                '<td>' + girisHtml + '</td>' +
                '<td>' + cixisHtml + '</td>' +
                '<td>' + duration + '</td>' +
                '<td>' + badge + '</td>' +
                '</tr>';
        });

        tableBody.innerHTML = html;
    }

    function getStatusBadge(status) {
        switch (status) {
            case 1: return '<span class="ud-badge ud-badge--isde"><span class="ud-badge-dot"></span>İşdə</span>';
            case 2: return '<span class="ud-badge ud-badge--gecikme"><span class="ud-badge-dot"></span>Gecikmə</span>';
            case 3: return '<span class="ud-badge ud-badge--qayib"><span class="ud-badge-dot"></span>Qayıb</span>';
            case 4: return '<span class="ud-badge ud-badge--icazeli"><span class="ud-badge-dot"></span>İcazəli</span>';
            case 5: return '<span class="ud-badge" style="background:rgba(168,85,247,.1);color:#a855f7;"><span class="ud-badge-dot"></span>Xəstəlik</span>';
            case 6: return '<span class="ud-badge" style="background:rgba(249,115,22,.1);color:#f97316;"><span class="ud-badge-dot"></span>Ezamiyyət</span>';
            default: return '<span class="ud-badge">Naməlum</span>';
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

});

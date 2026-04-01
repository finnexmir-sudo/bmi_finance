$(function () {
    var $dateStart = $('#dvmDateStart');
    var $dateEnd = $('#dvmDateEnd');
    var $statusFilter = $('#dvmStatusFilter');
    var $tableBody = $('#dvmTableBody');
    var $recordCount = $('#dvmRecordCount');

    // Stat elements
    var $statIsde = $('#statIsde');
    var $statGecikme = $('#statGecikme');
    var $statQayib = $('#statQayib');
    var $statCemi = $('#statCemi');

    // Summary elements
    var $summaryIsde = $('#summaryIsde');
    var $summaryGecikme = $('#summaryGecikme');
    var $summaryQayib = $('#summaryQayib');
    var $summaryIcazeli = $('#summaryIcazeli');

    var allRecords = [];

    // Parse initial data from the page
    try {
        var dataEl = document.getElementById('dvmInitialData');
        if (dataEl) {
            allRecords = JSON.parse(dataEl.textContent);
        }
    } catch (e) {
        allRecords = [];
    }

    applyFilters();

    // Events
    $statusFilter.on('change', function () {
        applyFilters();
    });

    $('#dvmFilterBtn').on('click', function () {
        var start = $dateStart.val();
        var end = $dateEnd.val();

        if (start && end) {
            loadData(start, end);
        }
    });

    $('#dvmResetBtn').on('click', function () {
        $dateStart.val('');
        $dateEnd.val('');
        $statusFilter.val('');
        loadData(null, null);
    });

    function loadData(start, end) {
        var url = '/User/Davamiyyet/GetByTarix';
        var params = {};
        if (start) params.baslangic = start;
        if (end) params.son = end;

        $.getJSON(url, params, function (data) {
            allRecords = data;
            applyFilters();
        });
    }

    function applyFilters() {
        var status = $statusFilter.val();
        var filtered = allRecords;

        if (status !== '' && status !== undefined && status !== null) {
            var statusInt = parseInt(status);
            filtered = filtered.filter(function (r) {
                return r.status === statusInt;
            });
        }

        // Sort by date descending
        filtered.sort(function (a, b) {
            return new Date(b.tarix) - new Date(a.tarix);
        });

        updateStats(allRecords);
        updateSummary(allRecords);
        renderTable(filtered);
        $recordCount.text(filtered.length + ' qeyd');
    }

    function updateStats(records) {
        var isde = 0, gecikme = 0, qayib = 0;

        records.forEach(function (r) {
            switch (r.status) {
                case 1: isde++; break;
                case 2: gecikme++; break;
                case 3: qayib++; break;
            }
        });

        $statIsde.text(isde);
        $statGecikme.text(gecikme);
        $statQayib.text(qayib);
        $statCemi.text(records.length);
    }

    function updateSummary(records) {
        var total = records.length || 1;
        var isde = 0, gecikme = 0, qayib = 0, icazeli = 0;

        records.forEach(function (r) {
            switch (r.status) {
                case 1: isde++; break;
                case 2: gecikme++; break;
                case 3: qayib++; break;
                case 4: icazeli++; break;
            }
        });

        animateProgress($summaryIsde, isde, total, '#10b981');
        animateProgress($summaryGecikme, gecikme, total, '#f59e0b');
        animateProgress($summaryQayib, qayib, total, '#ef4444');
        animateProgress($summaryIcazeli, icazeli, total, '#2d6cdf');
    }

    function animateProgress($el, count, total, color) {
        if (!$el.length) return;
        var pct = Math.round((count / total) * 100);
        $el.find('.fn-dvm-progress-fill').css({ width: pct + '%', background: color });
        $el.find('.fn-dvm-progress-val').text(count);
    }

    function renderTable(records) {
        if (records.length === 0) {
            $tableBody.html(
                '<tr><td colspan="6">' +
                '<div class="fn-dvm-empty">' +
                '<div class="fn-dvm-empty-icon">' +
                '<svg width="44" height="44" viewBox="0 0 44 44" fill="none">' +
                '<rect x="7" y="8" width="30" height="28" rx="4" stroke="#dde1ea" stroke-width="2"/>' +
                '<path d="M7 15h30M15 8v6M29 8v6M15 22h14M15 28h10" stroke="#dde1ea" stroke-width="1.8" stroke-linecap="round"/>' +
                '</svg></div>' +
                '<div class="fn-dvm-empty-title">Davamiyyat qeydi tapilmadi</div>' +
                '<div class="fn-dvm-empty-sub">Secilmis dovrda heç bir qeyd yoxdur</div>' +
                '</div></td></tr>'
            );
            return;
        }

        var gunler = ['Bazar', 'Bazar ertəsi', 'Çərşənbə axşamı', 'Cümə axşamı', 'Çərşənbə', 'Cümə axşamı', 'Cümə', 'Şənbə'];

        var html = '';
        records.forEach(function (r) {
            var tarix = new Date(r.tarix);
            var tarixStr = pad(tarix.getDate()) + '.' + pad(tarix.getMonth() + 1) + '.' + tarix.getFullYear();
            var gunAdi = getGunAdi(tarix.getDay());

            var giris = r.girisVaxti ? formatTime(r.girisVaxti) : '<span class="fn-dvm-nodata">--:--</span>';
            var cixis = r.cixisVaxti ? formatTime(r.cixisVaxti) : '<span class="fn-dvm-nodata">--:--</span>';

            // Is saati
            var duration = '';
            if (r.girisVaxti && r.cixisVaxti) {
                var diff = new Date(r.cixisVaxti) - new Date(r.girisVaxti);
                var hours = Math.floor(diff / 3600000);
                var mins = Math.floor((diff % 3600000) / 60000);
                var durationClass = hours < 8 ? 'fn-dvm-duration--short' : 'fn-dvm-duration--ok';
                duration = '<span class="fn-dvm-duration ' + durationClass + '">' + hours + ' saat ' + mins + ' dəq</span>';
            } else {
                duration = '<span class="fn-dvm-nodata">---</span>';
            }

            // Check late entry (after 09:05)
            var girisClass = 'fn-dvm-time';
            if (r.girisVaxti) {
                var gt = new Date(r.girisVaxti);
                if (gt.getHours() > 9 || (gt.getHours() === 9 && gt.getMinutes() > 5)) {
                    girisClass = 'fn-dvm-time fn-dvm-time--late';
                }
            }

            var statusBadge = getStatusBadge(r.status);

            html += '<tr>' +
                '<td><div class="fn-dvm-date">' + tarixStr + '</div><div class="fn-dvm-day">' + gunAdi + '</div></td>' +
                '<td><span class="' + girisClass + '">' + giris + '</span></td>' +
                '<td><span class="fn-dvm-time">' + cixis + '</span></td>' +
                '<td>' + duration + '</td>' +
                '<td>' + statusBadge + '</td>' +
                '</tr>';
        });

        $tableBody.html(html);
    }

    function getStatusBadge(status) {
        switch (status) {
            case 1:
                return '<span class="fn-dvm-badge fn-dvm-badge--isde"><span class="fn-dvm-badge-dot"></span>İşdə</span>';
            case 2:
                return '<span class="fn-dvm-badge fn-dvm-badge--gecikme"><span class="fn-dvm-badge-dot"></span>Gecikmə</span>';
            case 3:
                return '<span class="fn-dvm-badge fn-dvm-badge--qayib"><span class="fn-dvm-badge-dot"></span>Qayıb</span>';
            case 4:
                return '<span class="fn-dvm-badge fn-dvm-badge--icazeli"><span class="fn-dvm-badge-dot"></span>İcazəli</span>';
            default:
                return '<span class="fn-dvm-badge">Naməlum</span>';
        }
    }

    function formatTime(dateStr) {
        var d = new Date(dateStr);
        return pad(d.getHours()) + ':' + pad(d.getMinutes());
    }

    function pad(n) {
        return n < 10 ? '0' + n : '' + n;
    }

    function getGunAdi(day) {
        var gunler = ['Bazar', 'B.e.', 'Ç.a.', 'Çərşənbə', 'C.a.', 'Cümə', 'Şənbə'];
        return gunler[day] || '';
    }
});

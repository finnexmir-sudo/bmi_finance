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
    var kpiCemi = document.getElementById('kpiCemi');

    btnAxtar.addEventListener('click', function () {
        var params = {};
        if (inputBaslangic.value) params.baslangic = inputBaslangic.value;
        if (inputSon.value) params.son = inputSon.value;
        if (selectStatus.value) params.status = selectStatus.value;
        loadData(params);
    });

    function loadData(params) {
        var url = '/User/Davamiyyet/GetMyRecords?' + new URLSearchParams(params).toString();

        tableBody.innerHTML = '<tr><td colspan="5"><div class="ud-empty"><div class="spinner-border spinner-border-sm text-muted" role="status"></div><div style="margin-top:8px">Yüklənir...</div></div></td></tr>';

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                updateKPI(data.stats);
                renderTable(data.records);
                recordCount.textContent = data.records.length + ' qeyd';
            })
            .catch(function () {
                tableBody.innerHTML = '<tr><td colspan="5"><div class="ud-empty"><i class="bi bi-exclamation-triangle"></i><div>Xəta baş verdi</div></div></td></tr>';
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
            tableBody.innerHTML = '<tr><td colspan="5"><div class="ud-empty"><i class="bi bi-inbox"></i><div>Heç bir davamiyyət qeydi tapılmadı</div></div></td></tr>';
            return;
        }

        var html = '';
        records.forEach(function (r) {
            var tarix = formatDate(r.tarix);
            var giris = r.girisVaxti ? formatTime(r.girisVaxti) : '<span class="ud-nodata">--:--</span>';
            var cixis = r.cixisVaxti ? formatTime(r.cixisVaxti) : '<span class="ud-nodata">--:--</span>';

            var girisClass = 'ud-time';
            if (r.girisVaxti) {
                var gt = new Date(r.girisVaxti);
                if (gt.getHours() > 9 || (gt.getHours() === 9 && gt.getMinutes() > 5)) {
                    girisClass = 'ud-time ud-time--late';
                }
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
                '<td><span class="' + girisClass + '">' + giris + '</span></td>' +
                '<td><span class="ud-time">' + cixis + '</span></td>' +
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

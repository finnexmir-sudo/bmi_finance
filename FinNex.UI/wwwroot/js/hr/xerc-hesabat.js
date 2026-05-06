// ── Xərc Hesabatı JS ─────────────────────────────────────

(function () {
    'use strict';

    function fmt(val) {
        return Number(val).toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    var statusRenk = {
        0: { bg: '#f1f5f9', cl: '#64748b' },
        1: { bg: '#dbeafe', cl: '#1d4ed8' },
        2: { bg: '#ede9fe', cl: '#6d28d9' },
        3: { bg: '#dcfce7', cl: '#15803d' },
        4: { bg: '#fee2e2', cl: '#b91c1c' }
    };

    // ── Axtar ────────────────────────────────────────────
    function axtar() {
        var body = document.getElementById('xhBody');
        body.innerHTML = '<tr><td colspan="9" class="xh-empty"><i class="bi bi-hourglass-split"></i> Yüklənir...</td></tr>';
        document.getElementById('xhSummary').style.display  = 'none';
        document.getElementById('xhBreakdown').style.display = 'none';

        var params = new URLSearchParams({
            basTarix    : document.getElementById('fBasTarix').value,
            sonTarix    : document.getElementById('fSonTarix').value,
            departamentId: document.getElementById('fDept').value,
            kateqoriyaId : document.getElementById('fKat').value,
            status       : document.getElementById('fStatus').value,
            axtaris      : document.getElementById('fAxtaris').value
        });

        fetch('/HR/XercHesabat/GetData?' + params.toString())
            .then(function (r) { return r.json(); })
            .then(function (data) {
                renderTable(data.xercler);
                renderSummary(data.umumi);
                renderBreakdown(data.kateqoriyaBreakdown, data.sobeBreakdown);
            })
            .catch(function () {
                body.innerHTML = '<tr><td colspan="9" class="xh-empty">Xəta baş verdi</td></tr>';
            });
    }

    // ── Cədvəl ───────────────────────────────────────────
    function renderTable(xercler) {
        var body = document.getElementById('xhBody');

        if (!xercler || xercler.length === 0) {
            body.innerHTML = '<tr><td colspan="9" class="xh-empty">' +
                '<i class="bi bi-inbox" style="font-size:2rem;display:block;margin-bottom:10px;color:#cbd5e1"></i>' +
                'Nəticə tapılmadı</td></tr>';
            return;
        }

        var html = '';
        xercler.forEach(function (x) {
            var sr = statusRenk[x.status] || statusRenk[0];
            var badge = '<span style="font-size:11px;font-weight:600;padding:2px 9px;border-radius:20px;' +
                        'background:' + sr.bg + ';color:' + sr.cl + ';white-space:nowrap">' + x.statusAd + '</span>';
            var manual = x.manualGiris
                ? '<span class="xh-manual-badge">Manual</span> '
                : '';
            var qebz = x.qebzYolu
                ? '<a href="' + x.qebzYolu + '" target="_blank" class="xh-qebz-btn" title="Sənədi aç"><i class="bi bi-paperclip"></i> Bax</a>'
                : '<span style="color:#cbd5e1;font-size:12px">—</span>';
            var deptLink = x.departamentId
                ? '<a class="xh-dept-link" onclick="filterByDept(' + x.departamentId + ')">' + x.sobeAd + '</a>'
                : x.sobeAd;

            html += '<tr>' +
                '<td style="white-space:nowrap;color:#64748b;font-size:12px">' + x.tarix + '</td>' +
                '<td>' + manual + x.isciSobe + '</td>' +
                '<td>' + deptLink + '</td>' +
                '<td><span class="xh-kat-badge">' + x.kateqoriya + '</span></td>' +
                '<td class="xh-tesvir" title="' + x.tesvir.replace(/"/g, '&quot;') + '">' + x.tesvir + '</td>' +
                '<td style="text-align:right;font-weight:600;white-space:nowrap">' + fmt(x.mebleg) + ' ₼</td>' +
                '<td>' + badge + '</td>' +
                '<td style="font-size:12px;color:#64748b">' + (x.manualGiris ? 'Manual' : 'İşçi') + '</td>' +
                '<td>' + qebz + '</td>' +
                '</tr>';
        });
        body.innerHTML = html;
    }

    // ── Xülasə ───────────────────────────────────────────
    function renderSummary(u) {
        document.getElementById('xhSummary').style.display  = 'grid';
        document.getElementById('xhSay').textContent        = u.say;
        document.getElementById('xhOdenildi').textContent   = fmt(u.odenilenCem) + ' ₼';
        document.getElementById('xhGozleme').textContent    = fmt(u.gozlemecdeCem) + ' ₼';
        document.getElementById('xhImtina').textContent     = u.imtinaEdilen;
    }

    // ── Breakdown ────────────────────────────────────────
    function renderBreakdown(katList, sobeList) {
        var wrap = document.getElementById('xhBreakdown');
        if ((!katList || katList.length === 0) && (!sobeList || sobeList.length === 0)) {
            wrap.style.display = 'none';
            return;
        }
        wrap.style.display = 'grid';
        renderBkTable('xhKatBreakdown', katList, 'ad', null);
        renderBkTable('xhSobeBreakdown', sobeList, 'ad', 'departamentId');
    }

    function renderBkTable(elId, list, adKey, deptIdKey) {
        var el = document.getElementById(elId);
        if (!list || list.length === 0) { el.innerHTML = '<p style="color:#94a3b8;font-size:13px;padding:8px 0">Məlumat yoxdur</p>'; return; }

        var maxMebleg = list[0].mebleg;
        var html = '';
        list.slice(0, 8).forEach(function (item) {
            var pct = maxMebleg > 0 ? Math.round(item.mebleg / maxMebleg * 100) : 0;
            var adEl = deptIdKey && item[deptIdKey]
                ? '<a class="xh-dept-link" onclick="filterByDept(' + item[deptIdKey] + ')">' + item[adKey] + '</a>'
                : item[adKey];
            html += '<div class="xh-bk-row">' +
                '<div class="xh-bk-ad">' + adEl + ' <span class="xh-bk-say">(' + item.say + ')</span></div>' +
                '<div class="xh-bk-bar-wrap">' +
                '<div class="xh-bk-bar" style="width:' + pct + '%"></div>' +
                '</div>' +
                '<div class="xh-bk-mebleg">' + fmt(item.mebleg) + ' ₼</div>' +
                '</div>';
        });
        el.innerHTML = html;
    }

    // ── Şöbəyə görə filter et ────────────────────────────
    window.filterByDept = function (deptId) {
        document.getElementById('fDept').value = deptId;
        axtar();
    };

    // ── Excel export ─────────────────────────────────────
    function exportExcel() {
        var params = new URLSearchParams({
            basTarix     : document.getElementById('fBasTarix').value,
            sonTarix     : document.getElementById('fSonTarix').value,
            departamentId: document.getElementById('fDept').value,
            kateqoriyaId : document.getElementById('fKat').value,
            status       : document.getElementById('fStatus').value
        });
        window.location.href = '/HR/XercHesabat/ExportExcel?' + params.toString();
    }

    // ── Sıfırla ──────────────────────────────────────────
    function sifirla() {
        document.getElementById('fBasTarix').value = '';
        document.getElementById('fSonTarix').value  = '';
        document.getElementById('fDept').value      = '';
        document.getElementById('fKat').value       = '';
        document.getElementById('fStatus').value    = '';
        document.getElementById('fAxtaris').value   = '';
        document.getElementById('xhBody').innerHTML =
            '<tr><td colspan="9" class="xh-empty">' +
            '<i class="bi bi-search" style="font-size:2rem;display:block;margin-bottom:10px;color:#cbd5e1"></i>' +
            'Filtrlər seçib "Axtar" düyməsini basın</td></tr>';
        document.getElementById('xhSummary').style.display   = 'none';
        document.getElementById('xhBreakdown').style.display = 'none';
    }

    // ── Event listeners ──────────────────────────────────
    document.getElementById('btnAxtar').addEventListener('click', axtar);
    document.getElementById('btnSifirla').addEventListener('click', sifirla);
    document.getElementById('btnExcel').addEventListener('click', exportExcel);

    document.getElementById('fAxtaris').addEventListener('keydown', function (e) {
        if (e.key === 'Enter') axtar();
    });

    // ── Drill-down ilkin yükləmə ─────────────────────────
    (function initFromDrilldown() {
        var deptId = document.getElementById('initDeptId').value;
        var ay     = document.getElementById('initAy').value;
        var il     = document.getElementById('initIl').value;

        if (!deptId && !ay) return;

        if (deptId) document.getElementById('fDept').value = deptId;

        if (ay && il) {
            var year  = parseInt(il);
            var month = parseInt(ay);
            var bas   = new Date(year, month - 1, 1);
            var son   = new Date(year, month, 0);
            document.getElementById('fBasTarix').value = bas.toISOString().slice(0, 10);
            document.getElementById('fSonTarix').value  = son.toISOString().slice(0, 10);
        }

        axtar();
    })();

})();

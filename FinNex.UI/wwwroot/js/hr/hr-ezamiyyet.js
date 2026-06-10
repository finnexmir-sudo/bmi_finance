'use strict';

let _ezCurrentId = null;
let _ezIsciTimer  = null;

function ezIsciAxtar() {
    clearTimeout(_ezIsciTimer);
    _ezIsciTimer = setTimeout(ezYukle, 350);
}

async function ezYukle() {
    var p      = new URLSearchParams();
    var isci   = document.getElementById('ezIsci')?.value?.trim();
    var status = document.getElementById('ezStatus')?.value;
    var dep    = document.getElementById('ezDep')?.value;
    var mekan  = document.getElementById('ezMekan')?.value;
    var b      = document.getElementById('ezBaslangic')?.value;
    var s      = document.getElementById('ezSon')?.value;
    if (isci)   p.append('isciAd', isci);
    if (status) p.append('status', status);
    if (dep)    p.append('departamentId', dep);
    if (mekan)  p.append('mekanId', mekan);
    if (b)      p.append('baslangic', b);
    if (s)      p.append('son', s);

    var tbody = document.getElementById('ezTbody');
    tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:#94a3b8">Yüklənir…</td></tr>';

    var res  = await fetch('/HR/Ezamiyyet/GetMuracietler?' + p.toString());
    var data = await res.json();

    if (!data.length) {
        tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:#94a3b8">Müraciət yoxdur.</td></tr>';
        return;
    }

    tbody.innerHTML = data.map(function (r) {
        var st = ezStatusBadge(r.status);
        var tarix = r.baslamaTarixi + (r.baslamaTarixi !== r.bitmeTarixi ? '<br><small style="color:#94a3b8">– ' + r.bitmeTarixi + '</small>' : '');
        var sened = r.senedYolu
            ? '<a href="/dms/' + r.senedYolu + '" download="' + ezEsc(r.senedAd || 'sened') + '" style="color:#6366f1;font-size:12px"><i class="bi bi-paperclip"></i> ' + (r.senedAd || 'Sənəd') + '</a>'
            : '<span style="color:#94a3b8">—</span>';
        var emel = r.status === 1
            ? '<button class="fn-btn fn-btn--outline fn-btn--sm" onclick="ezOpenModal(' + r.id + ',\'' + ezEsc(r.isciTamAd) + '\',\'' + ezEsc(r.baslig) + '\',\'' + ezEsc(r.mekanAd) + '\',\'' + r.baslamaTarixi + '\',\'' + r.bitmeTarixi + '\')">' +
              '<i class="bi bi-check-square"></i> Bax</button>'
            : '<span style="font-size:11px;color:#94a3b8">' + (r.rehberTamAd ? r.rehberTamAd + '<br>' + (r.rehberTesdiqTarixi || '') : '') + '</span>';
        var geriNot = r.geriDonusQeydi
            ? '<div style="margin-top:4px;font-size:11px;color:#166534;background:#f0fdf4;padding:3px 7px;border-radius:5px"><i class="bi bi-check2-circle"></i> ' + ezEsc(r.geriDonusQeydi) + '</div>'
            : '';

        return '<tr style="border-top:1px solid #f1f5f9">' +
            '<td style="padding:10px 14px"><strong>' + r.isciTamAd + '</strong>' +
                (r.isciVezife ? '<br><small style="color:#94a3b8">' + r.isciVezife + '</small>' : '') + '</td>' +
            '<td style="padding:10px 14px;color:#0f172a">' + r.baslig + '</td>' +
            '<td style="padding:10px 14px;color:#374151"><i class="bi bi-geo-alt text-muted"></i> ' + r.mekanAd + '</td>' +
            '<td style="padding:10px 14px;white-space:nowrap">' + tarix + '<br><small style="color:#6366f1">' + r.gunSayi + ' gün</small></td>' +
            '<td style="padding:10px 14px">' + st +
                (r.rehberQeydi && r.status === 3 ? '<br><small style="color:#ef4444">' + r.rehberQeydi + '</small>' : '') +
                geriNot + '</td>' +
            '<td style="padding:10px 14px">' + sened + '</td>' +
            '<td style="padding:10px 14px">' + emel + '</td>' +
            '</tr>';
    }).join('');
}

function ezFilterStatus(s) {
    var sel = document.getElementById('ezStatus');
    if (sel) { sel.value = s; ezYukle(); }
}

function ezSifirla() {
    ['ezIsci','ezStatus','ezDep','ezMekan','ezBaslangic','ezSon'].forEach(function (id) {
        var el = document.getElementById(id);
        if (el) el.value = '';
    });
    ezYukle();
}

function ezStatusBadge(s) {
    var map = {
        1: ['#fef3c7','#b45309','Gözləyir'],
        2: ['#dcfce7','#15803d','Təsdiqləndi'],
        3: ['#fee2e2','#dc2626','Rədd edildi'],
        4: ['#f1f5f9','#64748b','Ləğv edildi']
    };
    var d = map[s] || ['#f1f5f9','#64748b','—'];
    return '<span style="display:inline-block;padding:2px 10px;border-radius:12px;font-size:12px;font-weight:600;background:' + d[0] + ';color:' + d[1] + '">' + d[2] + '</span>';
}

function ezOpenModal(id, isci, baslig, mekan, bas, bit) {
    _ezCurrentId = id;
    document.getElementById('ezQeyd').value = '';
    document.getElementById('ezModalBody').innerHTML =
        '<div style="background:#f8fafc;border-radius:8px;padding:12px;font-size:13px">' +
        '<div style="margin-bottom:6px"><strong>İşçi:</strong> ' + isci + '</div>' +
        '<div style="margin-bottom:6px"><strong>Başlıq:</strong> ' + baslig + '</div>' +
        '<div style="margin-bottom:6px"><strong>Məkan:</strong> ' + mekan + '</div>' +
        '<div><strong>Tarix:</strong> ' + bas + (bas !== bit ? ' – ' + bit : '') + '</div>' +
        '</div>';
    document.getElementById('ezOverlay').style.display = 'block';
    document.getElementById('ezModal').style.display   = 'block';
}

function ezCloseModal() {
    document.getElementById('ezOverlay').style.display = 'none';
    document.getElementById('ezModal').style.display   = 'none';
    _ezCurrentId = null;
}

async function ezTesdiq(tesdiq) {
    if (!_ezCurrentId) return;
    var qeyd = document.getElementById('ezQeyd').value.trim();
    if (!tesdiq && !qeyd) {
        alert('Rədd etmə səbəbini daxil edin.');
        return;
    }
    var res  = await fetch('/HR/Ezamiyyet/Tesdiq', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: _ezCurrentId, tesdiq: tesdiq, qeyd: qeyd })
    });
    var json = await res.json();
    if (json.success === false) { alert(json.message || 'Xəta baş verdi.'); return; }
    ezCloseModal();
    ezYukle();
}

function ezEsc(s) {
    return String(s || '').replace(/'/g, "\\'").replace(/"/g, '&quot;');
}

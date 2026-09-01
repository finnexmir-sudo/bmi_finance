'use strict';

let _ezCurrentId = null;
// Son yüklənən sətirlər — modal maşın məlumatını Id ilə buradan oxuyur.
var _ezRows = [];
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
    // Modal sətri Id ilə buradan tapır (01.09.2026). `ezOpenModal`-a 8 mövqe
    // arqumenti onsuz da ötürülür; yeni sahələri də ora yığmaq apostroflu
    // mətndə (məs. maşın modeli) onclick-i sındıra bilərdi.
    _ezRows = data;

    if (!data.length) {
        tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:#94a3b8">Müraciət yoxdur.</td></tr>';
        return;
    }

    tbody.innerHTML = data.map(function (r) {
        var st = ezStatusBadge(r.status);
        var tarix = r.baslamaTarixi + (r.baslamaTarixi !== r.bitmeTarixi ? '<br><small style="color:#94a3b8">– ' + r.bitmeTarixi + '</small>' : '');
        if (r.cihazCixisVaxti) {
            tarix += '<br><small style="color:#6366f1"><i class="bi bi-box-arrow-right"></i> ' + r.cihazCixisVaxti + '</small>';
            tarix += r.cihazQayidisVaxti
                ? '<br><small style="color:#22c55e"><i class="bi bi-box-arrow-in-left"></i> ' + r.cihazQayidisVaxti + '</small>'
                : '<br><small style="color:#f59e0b"><i class="bi bi-hourglass-split"></i> qayıtmayıb</small>';
        }
        // İşçinin yazdığı gündaxili saat aralığı (varsa) — rəhbər hansı saatda gedəcəyini görsün
        var saat = r.baslamaSaati
            ? '<br><small style="color:#0ea5e9"><i class="bi bi-clock"></i> ' + r.baslamaSaati + (r.bitisSaati ? '–' + r.bitisSaati : '') + '</small>'
            : '';
        var sened = r.senedYolu
            ? '<a href="/dms/' + r.senedYolu + '" download="' + ezEsc(r.senedAd || 'sened') + '" style="color:#6366f1;font-size:12px"><i class="bi bi-paperclip"></i> ' + (r.senedAd || 'Sənəd') + '</a>'
            : '<span style="color:#94a3b8">—</span>';
        var emel = r.status === 1
            ? '<button class="fn-btn fn-btn--outline fn-btn--sm" onclick="ezOpenModal(' + r.id + ',\'' + ezEsc(r.isciTamAd) + '\',\'' + ezEsc(r.baslig) + '\',\'' + ezEsc(r.mekanAd) + '\',\'' + r.baslamaTarixi + '\',\'' + r.bitmeTarixi + '\',\'' + (r.baslamaSaati || '') + '\',\'' + (r.bitisSaati || '') + '\')">' +
              '<i class="bi bi-check-square"></i> Bax</button>'
            : '<span style="font-size:11px;color:#94a3b8">' + (r.rehberTamAd ? r.rehberTamAd + '<br>' + (r.rehberTesdiqTarixi || '') : '') + '</span>';
        // Təsdiqlənmiş ezamiyyət üçün HR/Admin əl ilə çıxış/qayıdış düzəlişi
        if (window.ezCanEdit && r.status === 2) {
            emel += '<br><button class="fn-btn fn-btn--outline fn-btn--sm" style="margin-top:4px" ' +
                    'onclick="ezOpenDuzelt(' + r.id + ',\'' + ezEsc(r.isciTamAd) + '\',\'' +
                    (r.cihazCixisIso || '') + '\',\'' + (r.cihazQayidisIso || '') + '\',\'' + (r.baslamaIso || '') + '\',\'' + (r.bitmeIso || '') + '\')">' +
                    '<i class="bi bi-pencil"></i> Düzəlt</button>';
        }
        var geriNot = r.geriDonusQeydi
            ? '<div style="margin-top:4px;font-size:11px;color:#166534;background:#f0fdf4;padding:3px 7px;border-radius:5px"><i class="bi bi-check2-circle"></i> ' + ezEsc(r.geriDonusQeydi) + '</div>'
            : '';

        return '<tr style="border-top:1px solid #f1f5f9">' +
            '<td style="padding:10px 14px"><strong>' + r.isciTamAd + '</strong>' +
                (r.isciVezife ? '<br><small style="color:#94a3b8">' + r.isciVezife + '</small>' : '') + '</td>' +
            '<td style="padding:10px 14px;color:#0f172a">' + r.baslig + '</td>' +
            '<td style="padding:10px 14px;color:#374151"><i class="bi bi-geo-alt text-muted"></i> ' + r.mekanAd +
                (r.masinVar
                    ? '<div style="font-size:11px;color:#0f766e;margin-top:3px"><i class="bi bi-car-front"></i> ' +
                      (r.masinAdi ? ezEsc(r.masinAdi) : 'xidməti maşınla') + '</div>'
                    : '') + '</td>' +
            '<td style="padding:10px 14px;white-space:nowrap">' + tarix + '<br><small style="color:#6366f1">' + r.gunSayi + ' gün</small>' + saat + '</td>' +
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

function ezOpenModal(id, isci, baslig, mekan, bas, bit, basSaat, bitSaat) {
    _ezCurrentId = id;
    document.getElementById('ezQeyd').value = '';
    var saatMetn = basSaat ? (basSaat + (bitSaat ? ' – ' + bitSaat : '')) : 'Tam gün';
    document.getElementById('ezModalBody').innerHTML =
        '<div style="background:#f8fafc;border-radius:8px;padding:12px;font-size:13px">' +
        '<div style="margin-bottom:6px"><strong>İşçi:</strong> ' + isci + '</div>' +
        '<div style="margin-bottom:6px"><strong>Başlıq:</strong> ' + baslig + '</div>' +
        '<div style="margin-bottom:6px"><strong>Məkan:</strong> ' + mekan + '</div>' +
        '<div style="margin-bottom:6px"><strong>Tarix:</strong> ' + bas + (bas !== bit ? ' – ' + bit : '') + '</div>' +
        '<div><strong>Saat:</strong> <span style="color:#0ea5e9"><i class="bi bi-clock"></i> ' + saatMetn + '</span></div>' +
        ezMasinSetri(id) +
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

// ── HR əl ilə çıxış/qayıdış düzəlişi (insan faktoru) ──
function ezSplitIso(iso) {              // "2026-06-09T15:30" → ["2026-06-09","15:30"]
    if (!iso) return ['', ''];
    var p = iso.split('T');
    return [p[0] || '', (p[1] || '').slice(0, 5)];
}

function ezOpenDuzelt(id, isci, cixisIso, qayidisIso, baslamaIso, bitmeIso) {
    document.getElementById('ezDuzId').value = id;
    document.getElementById('ezDuzAd').textContent = isci || '';

    // Çıxış tarixi səfərin başlama tarixindən (mövcud çıxış varsa ondan) — HR yalnız saat yazır
    var cx = ezSplitIso(cixisIso);
    var cixisDate = cx[0] || baslamaIso || '';
    document.getElementById('ezDuzCixisD').value = cixisDate;
    document.getElementById('ezDuzCixisT').value = cx[1];
    document.getElementById('ezDuzCixisDLbl').textContent = ezFmtDate(cixisDate);

    // Qayıdış tarixi səfərin bitmə tarixindən; qayıdış saatı boşdursa, çıxış varsa 17:45 təklif
    var qy = ezSplitIso(qayidisIso);
    var qayidisDate = qy[0] || bitmeIso || '';
    var qayidisTime = qy[1] || ((!qayidisIso && cixisIso) ? '17:45' : '');
    document.getElementById('ezDuzQayidisD').value = qayidisDate;
    document.getElementById('ezDuzQayidisT').value = qayidisTime;
    document.getElementById('ezDuzQayidisDLbl').textContent = ezFmtDate(qayidisDate);

    document.getElementById('ezDuzOverlay').style.display = 'block';
    document.getElementById('ezDuzModal').style.display   = 'block';
}

// "2026-06-03" → "03.06.2026"
function ezFmtDate(iso) {
    if (!iso) return '—';
    var p = iso.split('-');
    return p.length === 3 ? p[2] + '.' + p[1] + '.' + p[0] : iso;
}

function ezDuzeltClose() {
    document.getElementById('ezDuzOverlay').style.display = 'none';
    document.getElementById('ezDuzModal').style.display   = 'none';
}

async function ezDuzeltSaxla() {
    var id = document.getElementById('ezDuzId').value;
    if (!id) return;

    var re = /^([01]?\d|2[0-3]):[0-5]\d$/;     // 24 saat SS:DD
    function birlesdir(dId, tId, etiket) {
        var d = document.getElementById(dId).value;        // tarix avtomatik (səfərdən)
        var t = document.getElementById(tId).value.trim();
        if (!t) return '';                                 // saat boş → sahəni təmizlə
        if (!re.test(t)) throw etiket + ' saatı düzgün deyil (SS:DD, məs: 17:45).';
        if (!d) throw etiket + ' üçün səfər tarixi tapılmadı.';
        return d + 'T' + t;
    }

    var cixis, qayidis;
    try {
        cixis   = birlesdir('ezDuzCixisD',   'ezDuzCixisT',   'Çıxış');
        qayidis = birlesdir('ezDuzQayidisD', 'ezDuzQayidisT', 'Qayıdış');
    } catch (msg) { alert(msg); return; }

    var res = await fetch('/HR/Ezamiyyet/CihazQayidisDuzelt', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: parseInt(id, 10), cixisVaxt: cixis, qayidisVaxt: qayidis })
    });
    var json = await res.json();
    if (json.success === false) { alert(json.message || 'Xəta baş verdi.'); return; }
    ezDuzeltClose();
    ezYukle();
}

/**
 * Təsdiq modalında «Xidməti maşın» sətri (01.09.2026).
 *
 * NİYƏ LAZIMDIR: bu ezamiyyəti təsdiqləmək EYNİ ZAMANDA maşın açarına icazə
 * verməkdir — rəhbər bunu bilmədən düyməyə basmamalıdır. İşçi portalındakı
 * təsdiq ekranında (`Tesdiq/EzamiyyetDetal.cshtml`) eyni sətir var; təsdiqin
 * İKİ giriş nöqtəsi var, birini unutsaq xəta yalnız o yolda görünər.
 *
 * Maşın istənməyibsə heç nə yazılmır (boş sətir).
 */
function ezMasinSetri(id) {
    var r = _ezRows.filter(function (x) { return x.id === id; })[0];
    if (!r || !r.masinVar) return '';

    var ad = r.masinAdi ? ezEsc(r.masinAdi) : 'seçilib';
    return '<div style="margin-top:8px;padding-top:8px;border-top:1px solid #e2e8f0">' +
           '<strong>Xidməti maşın:</strong> ' +
           '<span style="color:#0f766e"><i class="bi bi-car-front"></i> ' + ad + '</span>' +
           '<div style="font-size:11px;color:#64748b;margin-top:2px">' +
           'Təsdiqlədikdə maşın müraciəti avtomatik yaranır və açar üçün kassaya düşür.' +
           '</div></div>';
}

function ezEsc(s) {
    return String(s || '').replace(/'/g, "\\'").replace(/"/g, '&quot;');
}

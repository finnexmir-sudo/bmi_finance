'use strict';

// HR → Görüşlər idarəetməsi (04.11.2026) — yalnız oxuma. Ezamiyyət İdarəetməsinin
// (hr-ezamiyyet.js) eyni "filtr → fetch → tbody.innerHTML" naxışını təkrarlayır.

let _grAxtarisTimer = null;

function grAxtarisDebounce() {
    clearTimeout(_grAxtarisTimer);
    _grAxtarisTimer = setTimeout(grYukle, 350);
}

async function grYukle() {
    var p = new URLSearchParams();
    var axtaris = document.getElementById('grAxtaris')?.value?.trim();
    var status = document.getElementById('grStatus')?.value;
    var dep = document.getElementById('grDep')?.value;
    var from = document.getElementById('grTarixFrom')?.value;
    var to = document.getElementById('grTarixTo')?.value;
    if (axtaris) p.append('axtaris', axtaris);
    if (status) p.append('status', status);
    if (dep) p.append('departamentId', dep);
    if (from) p.append('tarixFrom', from);
    if (to) p.append('tarixTo', to);

    var tbody = document.getElementById('grTbody');
    tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:#94a3b8">Yüklənir…</td></tr>';

    var res = await fetch('/HR/Gorush/GetGorushler?' + p.toString());
    var data = await res.json();

    grKpiHesabla(data);

    if (!data.length) {
        tbody.innerHTML = '<tr><td colspan="7" style="padding:20px;text-align:center;color:#94a3b8">Görüş yoxdur.</td></tr>';
        return;
    }

    tbody.innerHTML = data.map(function (r) {
        var saat = r.baslamaSaati + (r.bitisSaati ? '–' + r.bitisSaati : '');
        var yer = r.yer
            ? '<i class="bi bi-geo-alt text-muted"></i> ' + grEsc(r.yer)
            : (r.nov === 2 ? '<i class="bi bi-camera-video text-muted"></i> Onlayn' : '<span style="color:#94a3b8">—</span>');
        return '<tr style="border-top:1px solid #f1f5f9">' +
            '<td style="padding:10px 14px"><strong>' + grEsc(r.bashliq) + '</strong></td>' +
            '<td style="padding:10px 14px;color:#374151">' + grEsc(r.teshkilatciAd) + '</td>' +
            '<td style="padding:10px 14px;white-space:nowrap">' + r.tarix + '<br><small style="color:#6366f1">' + saat + '</small></td>' +
            '<td style="padding:10px 14px;color:#374151">' + yer + '</td>' +
            '<td style="padding:10px 14px;text-align:center">' + r.ishtirakciSayi + '</td>' +
            '<td style="padding:10px 14px">' + grStatusBadge(r.statusClass, r.statusText) + '</td>' +
            '<td style="padding:10px 14px">' +
                '<a href="/HR/Gorush/Detay/' + r.id + '" class="fn-btn fn-btn--outline fn-btn--sm"><i class="bi bi-eye"></i> Bax</a>' +
            '</td>' +
            '</tr>';
    }).join('');
}

function grKpiHesabla(data) {
    var cemi = document.getElementById('grKpiCemi');
    var plan = document.getElementById('grKpiPlan');
    var bitdi = document.getElementById('grKpiBitdi');
    var legv = document.getElementById('grKpiLegv');
    if (cemi) cemi.textContent = data.length;
    if (plan) plan.textContent = data.filter(function (r) { return r.status === 1 || r.status === 2; }).length;
    if (bitdi) bitdi.textContent = data.filter(function (r) { return r.status === 3; }).length;
    if (legv) legv.textContent = data.filter(function (r) { return r.status === 4; }).length;
}

// `statusClass` serverdən "grsh-status--plan/--active/--done/--cancel" kimi gəlir
// (`GorushListDto.HesablanmisStatusClass`) — gorush.css bu səhifədə yüklənmir deyə,
// rənglər burda MÜSTƏQİL təyin olunur (Ezamiyyət-in öz `ezStatusBadge`-i kimi).
function grStatusBadge(cls, text) {
    var renk = { 'grsh-status--plan': '#0ea5e9', 'grsh-status--active': '#f59e0b', 'grsh-status--done': '#22c55e', 'grsh-status--cancel': '#ef4444' }[cls] || '#94a3b8';
    return '<span style="font-size:11.5px;font-weight:600;padding:3px 9px;border-radius:20px;background:' + renk + '1a;color:' + renk + '">' + grEsc(text) + '</span>';
}

function grFilterStatus(s) {
    var sel = document.getElementById('grStatus');
    if (sel) { sel.value = s; grYukle(); }
}

function grSifirla() {
    ['grAxtaris', 'grStatus', 'grDep', 'grTarixFrom', 'grTarixTo'].forEach(function (id) {
        var el = document.getElementById(id);
        if (el) el.value = '';
    });
    grYukle();
}

function grEsc(s) {
    return (s || '').replace(/[&<>"']/g, function (c) {
        return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
}

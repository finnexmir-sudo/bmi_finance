/* hr-jeton.js — HR Jeton İdarəetməsi */
'use strict';

let hjCurrentRedimId = null;
let hjCurrentLegvetId = null;
let hjSelectedJetonId = null;

// ── Tab idarəsi ───────────────────────────────────────────
function hjSwitchTab(btn, tab) {
    document.querySelectorAll('.hj-tab').forEach(t => t.classList.remove('hj-tab--active'));
    btn.classList.add('hj-tab--active');
    document.querySelectorAll('.hj-panel').forEach(p => p.style.display = 'none');
    document.getElementById('hjTab' + tab.charAt(0).toUpperCase() + tab.slice(1)).style.display = '';
    if (tab === 'redimler') hjLoadRedimler();
    if (tab === 'tarixce') hjLoadTarixce();
}

// ── Sorğu tarixçəsi ───────────────────────────────────────
async function hjLoadTarixce() {
    const tbody = document.getElementById('hjTarixceBody');
    const statusFilter = document.getElementById('hjTarixceStatusFilter').value;
    tbody.innerHTML = '<tr><td colspan="9" class="hj-empty">Yüklənir…</td></tr>';

    try {
        const res = await fetch('/HR/Jeton/GetRedimTarixcesi');
        const json = await res.json();
        if (!json.success || !json.data.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="hj-empty">Tarixçə boşdur.</td></tr>';
            return;
        }

        let data = json.data;
        if (statusFilter) data = data.filter(r => String(r.status) === statusFilter);
        if (!data.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="hj-empty">Filterə uyğun qeyd yoxdur.</td></tr>';
            return;
        }

        tbody.innerHTML = data.map(r => `
            <tr>
                <td>${r.isciTamAd ?? '—'}</td>
                <td>${hjRedimNov(r.redimNovu)}</td>
                <td><strong>${r.cemiSaat} saat</strong></td>
                <td>${hjDate(r.telabTarixi)}</td>
                <td>${r.neticeTarixi ? hjDate(r.neticeTarixi) : '—'}</td>
                <td>${r.tesdiqleyenAd ?? '—'}</td>
                <td>${hjStatusBadge(r.status + 10)}</td>
                <td class="hj-td-sebeb">${r.qeyd ?? '—'}</td>
                <td>
                    <button class="hj-btn hj-btn--ghost hj-btn--sm" onclick='hjOpenTarixceDetay(${JSON.stringify(r).replace(/'/g,"&#39;")})'>
                        <i class="bi bi-eye"></i>
                    </button>
                </td>
            </tr>`).join('');
    } catch {
        tbody.innerHTML = '<tr><td colspan="9" class="hj-empty">Xəta baş verdi.</td></tr>';
    }
}

function hjOpenTarixceDetay(r) {
    const jetonList = (r.xerclenenJetonlar || [])
        .map(j => `<li>${hjBadge(j)} <small>— ${j.sebeb ?? ''}</small></li>`).join('');

    document.getElementById('hjRedimModalBody').innerHTML = `
        <div class="hj-redim-info">
            <div class="hj-redim-row"><strong>İşçi</strong><span>${r.isciTamAd ?? '—'}</span></div>
            <div class="hj-redim-row"><strong>Xərcləmə növü</strong><span>${hjRedimNov(r.redimNovu)}</span></div>
            <div class="hj-redim-row"><strong>Cəmi saat</strong><span><b>${r.cemiSaat} saat</b></span></div>
            <div class="hj-redim-row"><strong>Sorğu tarixi</strong><span>${hjDate(r.telabTarixi)}</span></div>
            <div class="hj-redim-row"><strong>Cavab tarixi</strong><span>${r.neticeTarixi ? hjDate(r.neticeTarixi) : '—'}</span></div>
            <div class="hj-redim-row"><strong>Cavab verən</strong><span>${r.tesdiqleyenAd ?? '—'}</span></div>
            <div class="hj-redim-row"><strong>Status</strong><span>${hjStatusBadge(r.status + 10)}</span></div>
            ${r.qeyd ? `<div class="hj-redim-row"><strong>Qeyd</strong><span>${r.qeyd}</span></div>` : ''}
            ${jetonList ? `<div class="hj-redim-row"><strong>Jetonlar</strong><ul style="margin:0;padding-left:18px">${jetonList}</ul></div>` : ''}
        </div>`;

    // Tarixçə modalında təsdiq/rədd düymələri gizlənir — yalnız read-only
    document.getElementById('hjBtnTesdiq').style.display = 'none';
    document.getElementById('hjBtnRedd').style.display = 'none';

    document.getElementById('hjRedimOverlay').classList.add('hj-open');
    document.getElementById('hjRedimModal').classList.add('hj-open');
}

// ── Əməliyyatlar ──────────────────────────────────────────
async function hjLoadEmeliyyatlar() {
    const isciId = document.getElementById('hjIsciFilter').value;
    const url = isciId ? `/HR/Jeton/GetEmeliyyatlar?isciId=${isciId}` : '/HR/Jeton/GetEmeliyyatlar';
    const tbody = document.getElementById('hjEmeliyyatlarBody');
    tbody.innerHTML = '<tr><td colspan="9" class="hj-empty">Yüklənir…</td></tr>';

    try {
        const res = await fetch(url);
        const json = await res.json();
        if (!json.success || !json.data.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="hj-empty">Əməliyyat yoxdur.</td></tr>';
            return;
        }
        tbody.innerHTML = json.data.map(r => `
            <tr>
                <td>${r.isciTamAd ?? '—'}</td>
                <td>${hjBadge(r)}</td>
                <td style="white-space:nowrap">${hjDeyerGoster(r, true)}</td>
                <td style="white-space:nowrap">${hjQalanGoster(r)}</td>
                <td style="white-space:nowrap;font-size:12px">${hjXercGoster(r)}</td>
                <td style="white-space:nowrap">${hjDate(r.qazanmaTarixi)}</td>
                <td class="hj-td-sebeb">${r.sebeb ?? '—'}</td>
                <td>${hjStatusBadge(r.status)}</td>
                <td>
                    ${r.status === 1
                        ? `<button class="hj-btn hj-btn--ghost hj-btn--sm" onclick="hjOpenLegvetModal(${r.id})">
                               <i class="bi bi-trash3"></i>
                           </button>`
                        : ''}
                </td>
            </tr>`).join('');
    } catch {
        tbody.innerHTML = '<tr><td colspan="9" class="hj-empty">Xəta baş verdi.</td></tr>';
    }
}

// Jeton tam dəyərini göstərir (vahidə görə saat/gün)
function hjDeyerGoster(r, tam = false) {
    const saat = r.jetonSaatDeyeri ?? 0;
    if (saat <= 0) return '<span style="color:#6b7280">Cəza</span>';
    if (r.jetonVahid === 2)
        return `<strong>${(saat / 8).toFixed(2).replace(/\.?0+$/, '')} gün</strong> <span style="color:#9ca3af;font-size:11px">(${saat} saat)</span>`;
    return `<strong>${saat.toString().replace(/\.?0+$/, '')} saat</strong>`;
}

// Qalan dəyəri göstərir
function hjQalanGoster(r) {
    // Aktiv deyilsə qalan göstərməyə ehtiyac yoxdur
    if (r.status !== 1) return '<span style="color:#9ca3af">—</span>';
    const tam = r.jetonSaatDeyeri ?? 0;
    // qalanSaat null deməkdir tam qalıb
    const qalan = r.qalanSaat != null ? r.qalanSaat : tam;
    const renk = qalan < tam ? '#f59e0b' : '#22c55e';
    if (r.jetonVahid === 2) {
        const gun = (qalan / 8).toFixed(2).replace(/\.?0+$/, '');
        return `<span style="color:${renk};font-weight:600">${gun} gün</span>`
             + (qalan < tam ? ` <span style="color:#9ca3af;font-size:11px">(${qalan} saat)</span>` : '');
    }
    return `<span style="color:${renk};font-weight:600">${qalan} saat</span>`;
}

// Xərclənmə mənbəyini göstərir
function hjXercGoster(r) {
    if (r.icazeId) {
        const tarix = r.icazeTarixi ? new Date(r.icazeTarixi).toLocaleDateString('az-AZ', { day:'2-digit', month:'2-digit', year:'numeric' }) : '';
        const saat = (r.icazeBaslamaSaati ?? '') + (r.icazeBitisSaati ? '–' + r.icazeBitisSaati : '');
        return `<span style="color:#6366f1"><i class="bi bi-clock"></i> İcazə</span><br><span style="color:#6b7280">${tarix} ${saat}</span>`;
    }
    if (r.redimTelebiId)
        return `<span style="color:#0891b2"><i class="bi bi-cash-coin"></i> Redim sorğusu</span>`;
    if (r.status !== 1)
        return '<span style="color:#9ca3af">—</span>';
    return '<span style="color:#9ca3af">—</span>';
}

// ── Redim sorğuları ───────────────────────────────────────
let hjRehberView = false;

async function hjLoadRedimler() {
    const tbody = document.getElementById('hjRedimBody');
    tbody.innerHTML = '<tr><td colspan="6" class="hj-empty">Yüklənir…</td></tr>';

    try {
        const res = await fetch('/HR/Jeton/GetRedimler');
        const json = await res.json();
        const badge = document.getElementById('hjRedimBadge');
        hjRehberView = !!json.rehberView;

        if (!json.success || !json.data.length) {
            tbody.innerHTML = `<tr><td colspan="6" class="hj-empty">${hjRehberView ? 'Sizin departament üçün gözləyən sorğu yoxdur.' : 'Rəhbər təsdiqindən keçmiş sorğu yoxdur.'}</td></tr>`;
            badge.style.display = 'none';
            return;
        }
        badge.textContent = json.data.length;
        badge.style.display = 'inline-block';

        tbody.innerHTML = json.data.map(r => `
            <tr>
                <td>${r.isciTamAd ?? '—'}</td>
                <td>${hjRedimNov(r.redimNovu)}${r.icazeTarixi ? `<br><small style="color:#6b7280">${hjDate(r.icazeTarixi).split(',')[0]} ${r.baslamaSaati ?? ''}–${r.bitisSaati ?? ''}</small>` : ''}</td>
                <td><strong>${r.cemiSaat} saat</strong></td>
                <td>${hjDate(r.telabTarixi)}</td>
                <td>${hjStatusBadge(r.status + 10)}${r.rehberTesdiq ? '<br><small style="color:#16a34a">✓ Rəhbər</small>' : ''}</td>
                <td>
                    <button class="hj-btn hj-btn--ghost hj-btn--sm" onclick="hjOpenRedimModal(${JSON.stringify(r).replace(/"/g,'&quot;')})">
                        <i class="bi bi-eye"></i> Bax
                    </button>
                </td>
            </tr>`).join('');
    } catch {
        tbody.innerHTML = '<tr><td colspan="6" class="hj-empty">Xəta baş verdi.</td></tr>';
    }
}

// ── Jeton Ver Modal ───────────────────────────────────────
function hjOpenVerModal() {
    hjSelectedJetonId = null;
    document.querySelectorAll('.hj-jeton-card').forEach(c => c.classList.remove('hj-jeton-card--selected'));
    document.getElementById('hjVerIsci').value = '';
    document.getElementById('hjVerSebeb').value = '';
    const edInp = document.getElementById('hjVerEded');
    if (edInp) edInp.value = 1;
    document.getElementById('hjVerOverlay').classList.add('hj-open');
    document.getElementById('hjVerModal').classList.add('hj-open');
}
function hjCloseVerModal() {
    document.getElementById('hjVerOverlay').classList.remove('hj-open');
    document.getElementById('hjVerModal').classList.remove('hj-open');
}
function hjSelectJeton(card) {
    document.querySelectorAll('.hj-jeton-card').forEach(c => c.classList.remove('hj-jeton-card--selected'));
    card.classList.add('hj-jeton-card--selected');
    hjSelectedJetonId = parseInt(card.dataset.id);
}
async function hjSubmitJetonVer() {
    const isciId = parseInt(document.getElementById('hjVerIsci').value);
    const sebeb = document.getElementById('hjVerSebeb').value.trim();
    let eded = parseInt(document.getElementById('hjVerEded')?.value) || 1;
    if (eded < 1) eded = 1;
    if (eded > 50) eded = 50;
    if (!isciId) return hjToast('İşçi seçin.', 'warn');
    if (!hjSelectedJetonId) return hjToast('Jeton növü seçin.', 'warn');
    if (!sebeb) return hjToast('Səbəb daxil edin.', 'warn');

    const res = await fetch('/HR/Jeton/JetonVer', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isciId, jetonTeyinatiId: hjSelectedJetonId, sebeb, eded })
    });
    const json = await res.json();
    if (json.success) {
        hjToast(json.message || 'Jeton verildi.', 'success');
        hjCloseVerModal();
        hjLoadEmeliyyatlar();
    } else {
        hjToast(json.message || 'Xəta baş verdi.', 'error');
    }
}

// ── Legvet Modal ──────────────────────────────────────────
function hjOpenLegvetModal(id) {
    hjCurrentLegvetId = id;
    document.getElementById('hjLegvetSebeb').value = '';
    document.getElementById('hjLegvetOverlay').classList.add('hj-open');
    document.getElementById('hjLegvetModal').classList.add('hj-open');
}
function hjCloseLegvetModal() {
    document.getElementById('hjLegvetOverlay').classList.remove('hj-open');
    document.getElementById('hjLegvetModal').classList.remove('hj-open');
}
async function hjSubmitLegvet() {
    const sebeb = document.getElementById('hjLegvetSebeb').value.trim();
    if (!sebeb) return hjToast('Ləğvetmə səbəbini daxil edin.', 'warn');

    const res = await fetch('/HR/Jeton/JetonLegvet', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isciJetonuId: hjCurrentLegvetId, sebeb })
    });
    const json = await res.json();
    if (json.success) {
        hjToast(json.message || 'Jeton ləğv edildi.', 'success');
        hjCloseLegvetModal();
        hjLoadEmeliyyatlar();
    } else {
        hjToast(json.message || 'Xəta baş verdi.', 'error');
    }
}

// ── Redim Modal ───────────────────────────────────────────
function hjOpenRedimModal(r) {
    hjCurrentRedimId = r.id;
    document.getElementById('hjBtnTesdiq').style.display = '';
    document.getElementById('hjBtnRedd').style.display = '';
    const jetonList = (r.xerclenenJetonlar || [])
        .map(j => `<li>${hjBadge(j)}</li>`).join('');

    const icazeBlock = r.redimNovu === 1 && r.icazeTarixi ? `
            <div class="hj-redim-row"><strong>İcazə tarixi</strong><span>${hjDate(r.icazeTarixi).split(',')[0]}</span></div>
            <div class="hj-redim-row"><strong>Saat aralığı</strong><span>${(r.baslamaSaati ?? '').slice(0,5)} – ${(r.bitisSaati ?? '').slice(0,5)}</span></div>
        ` : '';

    const rehberBlock = r.rehberTesdiq ? `
            <div class="hj-redim-row"><strong>Rəhbər təsdiqi</strong><span>✅ ${r.rehberAd ?? ''} • ${hjDate(r.rehberTesdiqTarixi)}</span></div>
        ` : '';

    document.getElementById('hjRedimModalBody').innerHTML = `
        <div class="hj-redim-info">
            <div class="hj-redim-row"><strong>İşçi</strong><span>${r.isciTamAd ?? '—'}</span></div>
            <div class="hj-redim-row"><strong>Xərcləmə növü</strong><span>${hjRedimNov(r.redimNovu)}</span></div>
            <div class="hj-redim-row"><strong>Cəmi saat</strong><span><b>${r.cemiSaat} saat</b></span></div>
            ${icazeBlock}
            <div class="hj-redim-row"><strong>Sorğu tarixi</strong><span>${hjDate(r.telabTarixi)}</span></div>
            ${rehberBlock}
            ${jetonList ? `<div class="hj-redim-row"><strong>Jetonlar</strong><ul style="margin:0;padding-left:18px">${jetonList}</ul></div>` : ''}
        </div>
        <div class="hj-qeyd-field">
            <label>Qeyd (rədd zamanı)</label>
            <textarea id="hjRedimQeyd" class="hj-textarea" rows="2" placeholder="Rədd etmə səbəbi…"></textarea>
        </div>`;

    document.getElementById('hjRedimOverlay').classList.add('hj-open');
    document.getElementById('hjRedimModal').classList.add('hj-open');
}
function hjCloseRedimModal() {
    document.getElementById('hjRedimOverlay').classList.remove('hj-open');
    document.getElementById('hjRedimModal').classList.remove('hj-open');
}
async function hjTesdiqleRedim() {
    // Rəhbər görünüşündə RehberTesdiq endpointinə, HR-də mövcud TesdiqleRedim-ə gedir
    const url = hjRehberView ? '/HR/Jeton/RehberTesdiq' : '/HR/Jeton/TesdiqleRedim';
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ redimId: hjCurrentRedimId })
    });
    const json = await res.json();
    if (json.success) {
        hjToast(json.message || 'Sorğu təsdiqləndi.', 'success');
        hjCloseRedimModal();
        hjLoadRedimler();
    } else {
        hjToast(json.message || 'Xəta baş verdi.', 'error');
    }
}
async function hjReddEtRedim() {
    const qeyd = document.getElementById('hjRedimQeyd').value.trim();
    if (!qeyd) return hjToast('Rədd etmə səbəbini daxil edin.', 'warn');

    const url = hjRehberView ? '/HR/Jeton/RehberRedd' : '/HR/Jeton/ReddEtRedim';
    const res = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ redimId: hjCurrentRedimId, qeyd })
    });
    const json = await res.json();
    if (json.success) {
        hjToast(json.message || 'Sorğu rədd edildi.', 'success');
        hjCloseRedimModal();
        hjLoadRedimler();
    } else {
        hjToast(json.message || 'Xəta baş verdi.', 'error');
    }
}

// ── Helpers ───────────────────────────────────────────────
function hjBadge(r) {
    return `<span class="hj-jeton-badge" style="background:${r.jetonRengKodu ?? '#6b7280'}">
                <i class="${r.jetonIkon ?? 'bi bi-award-fill'}"></i>
                ${r.jetonAd ?? r.ad ?? ''}
            </span>`;
}

function hjStatusBadge(status) {
    const map = {
        1: ['Aktiv', 'aktiv'],
        2: ['İstifadə olunub', 'istifade'],
        3: ['Ləğv edildi', 'legvedildi'],
        11: ['Gözlənilir', 'gozlenilir'],
        12: ['Təsdiqləndi', 'tesdiqlendi'],
        13: ['Rədd edildi', 'redd'],
    };
    const [label, cls] = map[status] ?? ['—', 'aktiv'];
    return `<span class="hj-status hj-status--${cls}">${label}</span>`;
}

function hjRedimNov(nov) {
    return nov === 1 ? '<i class="bi bi-clock"></i> İcazə' : '<i class="bi bi-cash-coin"></i> Maaşa əlavə';
}

function hjDate(val) {
    if (!val) return '—';
    const d = new Date(val);
    return d.toLocaleDateString('az-AZ', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function hjToast(msg, type = 'info') {
    const colors = { success: '#22c55e', error: '#ef4444', warn: '#f59e0b', info: '#6366f1' };
    const el = document.createElement('div');
    el.textContent = msg;
    Object.assign(el.style, {
        position: 'fixed', bottom: '24px', right: '24px', zIndex: 9999,
        background: colors[type] ?? '#333', color: '#fff',
        padding: '12px 20px', borderRadius: '10px',
        fontSize: '14px', fontWeight: '600',
        boxShadow: '0 4px 20px rgba(0,0,0,.18)',
        transition: 'opacity .3s', opacity: '1'
    });
    document.body.appendChild(el);
    setTimeout(() => { el.style.opacity = '0'; setTimeout(() => el.remove(), 350); }, 3000);
}

// ── Init ──────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    hjLoadEmeliyyatlar();
    // Gözləyən redim badge-ini yüklə
    fetch('/HR/Jeton/GetRedimler')
        .then(r => r.json())
        .then(json => {
            if (json.success && json.data.length) {
                const badge = document.getElementById('hjRedimBadge');
                badge.textContent = json.data.length;
                badge.style.display = 'inline-block';
            }
        }).catch(() => {});

    // ?tab=redimler və ya ?tab=tarixce ilə müvafiq tab açılır
    const params = new URLSearchParams(window.location.search);
    const tab = params.get('tab');
    if (tab) {
        const btn = document.querySelector(`.hj-tab[data-tab="${tab}"]`);
        if (btn) hjSwitchTab(btn, tab);
    }
});

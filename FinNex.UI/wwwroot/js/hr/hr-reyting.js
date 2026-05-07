/* hr-reyting.js */
'use strict';

const AY_ADLARI = ['', 'Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'İyun',
                   'İyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'];

let rrData = [];

document.addEventListener('DOMContentLoaded', () => {
    rrInitFilters();
    rrLoad();
});

function rrInitFilters() {
    const ilEl = document.getElementById('rrIl');
    const ayEl = document.getElementById('rrAy');
    const now = new Date();

    for (let y = now.getFullYear(); y >= now.getFullYear() - 3; y--) {
        const o = document.createElement('option');
        o.value = y; o.textContent = y;
        if (y === now.getFullYear()) o.selected = true;
        ilEl.appendChild(o);
    }
    for (let m = 1; m <= 12; m++) {
        const o = document.createElement('option');
        o.value = m; o.textContent = AY_ADLARI[m];
        if (m === now.getMonth() + 1) o.selected = true;
        ayEl.appendChild(o);
    }
}

async function rrLoad() {
    const il = document.getElementById('rrIl').value;
    const ay = document.getElementById('rrAy').value;
    const wrap = document.getElementById('rrTableWrap');
    wrap.innerHTML = '<div class="rr-empty">Yüklənir…</div>';

    const res = await fetch(`/HR/Reyting/GetData?il=${il}&ay=${ay}`);
    const json = await res.json();
    rrData = json.data || [];

    if (!rrData.length) {
        wrap.innerHTML = '<div class="rr-empty">Bu ay üçün reytinq hesablanmayıb. "Hamısını hesabla" düyməsinə basın.</div>';
        return;
    }

    wrap.innerHTML = `
        <table class="rr-table">
            <thead>
                <tr>
                    <th>İşçi</th>
                    <th>Departament / Vəzifə</th>
                    <th>Avtomatik Xal</th>
                    <th>Manual Xal</th>
                    <th>Cəmi Xal</th>
                    <th>Kateqoriya</th>
                    <th>Pul Əmsal</th>
                    <th>Saat Əmsal</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                ${rrData.map(r => `
                <tr>
                    <td><strong>${r.isciAd}</strong></td>
                    <td>
                        <div style="font-size:13px">${r.departament ?? '—'}</div>
                        <div style="font-size:11.5px;color:#6b7280">${r.vezife ?? ''}</div>
                    </td>
                    <td><span class="rr-xal ${r.avtomatikXal >= 0 ? 'rr-xal--pos' : 'rr-xal--neg'}">${r.avtomatikXal > 0 ? '+' : ''}${r.avtomatikXal}</span></td>
                    <td><span class="rr-xal ${r.manualXal > 0 ? 'rr-xal--pos' : r.manualXal < 0 ? 'rr-xal--neg' : 'rr-xal--zero'}">${r.manualXal > 0 ? '+' : ''}${r.manualXal}</span></td>
                    <td><span class="rr-xal ${r.cemiXal >= 0 ? 'rr-xal--pos' : 'rr-xal--neg'}" style="font-size:17px">${r.cemiXal}</span></td>
                    <td>
                        ${r.kateqoriyaAd
                            ? `<span class="rr-kat-badge" style="background:${r.kateqoriyaReng ?? '#e5e7eb'}22;color:${r.kateqoriyaReng ?? '#6b7280'};border:1px solid ${r.kateqoriyaReng ?? '#e5e7eb'}44">${r.kateqoriyaAd}</span>`
                            : '<span style="color:#6b7280;font-size:12px">—</span>'}
                    </td>
                    <td><span class="rr-amsal">${r.pulAmsali}x</span></td>
                    <td><span class="rr-amsal">${r.saatAmsali}x</span></td>
                    <td>
                        <div class="rr-actions">
                            <button class="rr-btn rr-btn--ghost rr-btn--sm"
                                onclick="rrOpenManual(${r.isciId}, '${r.isciAd.replace(/'/g, "\\'")}')">
                                <i class="bi bi-plus-circle"></i> Xal
                            </button>
                            ${r.manualQeydler && r.manualQeydler.length
                                ? `<button class="rr-btn rr-btn--ghost rr-btn--sm"
                                    onclick="rrOpenTarix(${r.isciId}, '${r.isciAd.replace(/'/g, "\\'")}')">
                                    <i class="bi bi-clock-history"></i> (${r.manualQeydler.length})
                                   </button>`
                                : ''}
                            <button class="rr-btn rr-btn--success rr-btn--sm"
                                onclick="rrHesabla(true, ${r.isciId})">
                                <i class="bi bi-arrow-clockwise"></i>
                            </button>
                        </div>
                    </td>
                </tr>`).join('')}
            </tbody>
        </table>`;
}

// ── Hesabla ──────────────────────────────────────────────────

async function rrHesabla(tekIsci, isciId) {
    const il = parseInt(document.getElementById('rrIl').value);
    const ay = parseInt(document.getElementById('rrAy').value);
    const dto = { il, ay, isciId: tekIsci ? isciId : null };

    const res = await fetch('/HR/Reyting/Hesabla', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    });
    const json = await res.json();
    rrToast(json.message, json.success ? 'success' : 'error');
    if (json.success) rrLoad();
}

// ── Manual Xal Modal ─────────────────────────────────────────

function rrOpenManual(isciId, isciAd) {
    document.getElementById('rrManualIsciId').value = isciId;
    document.getElementById('rrManualIl').value = document.getElementById('rrIl').value;
    document.getElementById('rrManualAy').value = document.getElementById('rrAy').value;
    document.getElementById('rrManualIsciAd').textContent = isciAd;
    document.getElementById('rrManualXal').value = '';
    document.getElementById('rrManualSebeb').value = '';
    document.getElementById('rrManualOverlay').classList.add('rr-open');
    document.getElementById('rrManualModal').classList.add('rr-open');
}

function rrCloseManual() {
    document.getElementById('rrManualOverlay').classList.remove('rr-open');
    document.getElementById('rrManualModal').classList.remove('rr-open');
}

async function rrSubmitManual() {
    const dto = {
        isciId: parseInt(document.getElementById('rrManualIsciId').value),
        il: parseInt(document.getElementById('rrManualIl').value),
        ay: parseInt(document.getElementById('rrManualAy').value),
        xal: parseInt(document.getElementById('rrManualXal').value),
        sebeb: document.getElementById('rrManualSebeb').value.trim()
    };
    if (!dto.xal) return rrToast('Xal daxil edin.', 'warn');
    if (!dto.sebeb) return rrToast('Səbəb daxil edin.', 'warn');

    const res = await fetch('/HR/Reyting/ManualElave', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    });
    const json = await res.json();
    rrToast(json.message, json.success ? 'success' : 'error');
    if (json.success) { rrCloseManual(); rrLoad(); }
}

// ── Tarixçə Modal ────────────────────────────────────────────

function rrOpenTarix(isciId, isciAd) {
    document.getElementById('rrTarixTitle').textContent = `${isciAd} — Manual Xal Tarixçəsi`;
    const r = rrData.find(x => x.isciId === isciId);
    const body = document.getElementById('rrTarixBody');
    if (!r || !r.manualQeydler.length) {
        body.innerHTML = '<div class="rr-empty">Heç bir manual qeyd yoxdur.</div>';
    } else {
        body.innerHTML = '<div class="rr-tarix-list">' + r.manualQeydler.map(q => `
            <div class="rr-tarix-item">
                <div class="rr-tarix-xal ${q.xal >= 0 ? 'rr-xal--pos' : 'rr-xal--neg'}">${q.xal > 0 ? '+' : ''}${q.xal}</div>
                <div class="rr-tarix-info">
                    <div class="rr-tarix-sebeb">${q.sebeb}</div>
                    <div class="rr-tarix-meta">${q.elavEden} · ${rrDate(q.tarix)}</div>
                </div>
            </div>`).join('') + '</div>';
    }
    document.getElementById('rrTarixOverlay').classList.add('rr-open');
    document.getElementById('rrTarixModal').classList.add('rr-open');
}

function rrCloseTarix() {
    document.getElementById('rrTarixOverlay').classList.remove('rr-open');
    document.getElementById('rrTarixModal').classList.remove('rr-open');
}

// ── Helpers ──────────────────────────────────────────────────

function rrDate(val) {
    if (!val) return '—';
    return new Date(val).toLocaleDateString('az-AZ', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function rrToast(msg, type = 'info') {
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

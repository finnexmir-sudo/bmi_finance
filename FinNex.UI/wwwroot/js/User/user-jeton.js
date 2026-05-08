/* user-jeton.js — Cüzdanım */
'use strict';

let ujJetonlar = []; // aktiv jetonlar cache
let ujSelectedIds = new Set();

// ── Tab ─────────────────────────────────────────────────
function ujSwitchTab(btn, tab) {
    document.querySelectorAll('.uj-tab').forEach(t => t.classList.remove('uj-tab--active'));
    btn.classList.add('uj-tab--active');
    document.querySelectorAll('.uj-panel').forEach(p => p.style.display = 'none');
    document.getElementById('ujTab' + tab.charAt(0).toUpperCase() + tab.slice(1)).style.display = '';
    if (tab === 'redimler') ujLoadRedimler();
}

// ── Jetonlarım ───────────────────────────────────────────
async function ujLoadJetonlar() {
    const container = document.getElementById('ujJetonList');
    container.innerHTML = '<div class="uj-empty">Yüklənir…</div>';

    try {
        const res = await fetch('/User/Jeton/GetJetonlarim');
        const json = await res.json();
        ujJetonlar = json.data || [];

        if (!ujJetonlar.length) {
            container.innerHTML = '<div class="uj-empty">Aktiv jetonunuz yoxdur.</div>';
            return;
        }
        container.innerHTML = ujJetonlar.map(j => `
            <div class="uj-jeton-item" style="--jc:${j.jetonRengKodu ?? '#9ca3af'}">
                <div class="uj-jeton-item-icon"><i class="${j.jetonIkon ?? 'bi bi-award-fill'}"></i></div>
                <div class="uj-jeton-item-name">${j.jetonAd ?? ''}</div>
                <div class="uj-jeton-item-saat">${j.jetonSaatDeyeri > 0 ? j.jetonSaatDeyeri + ' saat' : 'Cəza'}</div>
                <div class="uj-jeton-item-date">${ujDate(j.qazanmaTarixi)}</div>
                ${j.sebeb ? `<div class="uj-jeton-item-sebeb">${j.sebeb}</div>` : ''}
            </div>`).join('');
    } catch {
        container.innerHTML = '<div class="uj-empty">Xəta baş verdi.</div>';
    }
}

// ── Redim tarixçəsi ──────────────────────────────────────
async function ujLoadRedimler() {
    const tbody = document.getElementById('ujRedimBody');
    tbody.innerHTML = '<tr><td colspan="5" class="uj-empty">Yüklənir…</td></tr>';

    try {
        const res = await fetch('/User/Jeton/GetRedimTarixcesi');
        const json = await res.json();
        if (!json.success || !json.data.length) {
            tbody.innerHTML = '<tr><td colspan="5" class="uj-empty">Xərcləmə tarixçəsi yoxdur.</td></tr>';
            return;
        }
        tbody.innerHTML = json.data.map(r => `
            <tr>
                <td>${r.redimNovu === 1 ? '<i class="bi bi-clock"></i> İcazə' : '<i class="bi bi-cash-coin"></i> Maaşa əlavə'}</td>
                <td><strong>${r.cemiSaat} saat</strong></td>
                <td>${ujDate(r.telabTarixi)}</td>
                <td>${ujStatusBadge(r.status)}</td>
                <td>${r.qeyd ?? '—'}</td>
            </tr>`).join('');
    } catch {
        tbody.innerHTML = '<tr><td colspan="5" class="uj-empty">Xəta baş verdi.</td></tr>';
    }
}

// ── Redim Modal ───────────────────────────────────────────
function ujOpenRedimModal() {
    if (window.ujQaraVar) {
        ujToast('Qara jeton mövcuddur — jeton xərcləmək mümkün deyil.', 'error');
        return;
    }
    ujSelectedIds.clear();
    ujRenderSelectList();
    document.getElementById('ujSelectedInfo').style.display = 'none';
    document.getElementById('ujRedimOverlay').classList.add('uj-open');
    document.getElementById('ujRedimModal').classList.add('uj-open');
}
function ujCloseRedimModal() {
    document.getElementById('ujRedimOverlay').classList.remove('uj-open');
    document.getElementById('ujRedimModal').classList.remove('uj-open');
}

function ujRenderSelectList() {
    const container = document.getElementById('ujJetonSelectList');
    if (!ujJetonlar.length) {
        container.innerHTML = '<p style="color:#6b7280;font-size:13px">Aktiv jeton yoxdur.</p>';
        return;
    }
    container.innerHTML = ujJetonlar.map(j => `
        <div class="uj-jeton-select-item ${ujSelectedIds.has(j.id) ? 'uj-jeton-select-item--selected' : ''}"
             style="--jc:${j.jetonRengKodu ?? '#9ca3af'}"
             onclick="ujToggleJeton(${j.id}, ${j.jetonSaatDeyeri})">
            <div class="uj-jsi-icon"><i class="${j.jetonIkon ?? 'bi bi-award-fill'}"></i></div>
            <div class="uj-jsi-info">
                <div class="uj-jsi-name">${j.jetonAd ?? ''}</div>
                <div class="uj-jsi-saat">${j.jetonSaatDeyeri} saat</div>
            </div>
            <div class="uj-jsi-check"><i class="bi bi-check2"></i></div>
        </div>`).join('');
}

function ujToggleJeton(id, saat) {
    if (ujSelectedIds.has(id)) ujSelectedIds.delete(id);
    else ujSelectedIds.add(id);
    ujRenderSelectList();
    ujUpdateSelectedInfo();
}

function ujUpdateSelectedInfo() {
    const total = ujJetonlar
        .filter(j => ujSelectedIds.has(j.id))
        .reduce((acc, j) => acc + (j.jetonSaatDeyeri ?? 0), 0);
    const info = document.getElementById('ujSelectedInfo');
    if (ujSelectedIds.size > 0) {
        info.style.display = 'block';
        document.getElementById('ujSelectedSaat').textContent = total.toFixed(2).replace('.00', '');
    } else {
        info.style.display = 'none';
    }
}

async function ujSubmitRedim() {
    if (!ujSelectedIds.size) return ujToast('Jeton seçin.', 'warn');
    const redimNov = parseInt(document.querySelector('input[name="ujRedimNov"]:checked').value);

    const body = { jetonIds: [...ujSelectedIds], redimNovu: redimNov };

    // İcazə sorğusu üçün tarix və saat aralığı məcburidir
    if (redimNov === 1) {
        const tarix = document.getElementById('ujIcazeTarixi').value;
        const bas = document.getElementById('ujBaslamaSaati').value;
        const bitis = document.getElementById('ujBitisSaati').value;
        if (!tarix) return ujToast('İcazə tarixini seçin.', 'warn');
        if (!bas || !bitis) return ujToast('Saat aralığını daxil edin.', 'warn');
        if (bas >= bitis) return ujToast('Bitmə saatı başlamadan sonra olmalıdır.', 'warn');
        body.icazeTarixi = tarix;
        body.baslamaSaati = bas;
        body.bitisSaati = bitis;
    }

    const res = await fetch('/User/Jeton/RedimGonder', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    const json = await res.json();
    if (json.success) {
        ujToast(json.message || 'Sorğu göndərildi.', 'success');
        ujCloseRedimModal();
        location.reload();
    } else {
        ujToast(json.message || 'Xəta baş verdi.', 'error');
    }
}

// İcazə radio seçilərkən tarix/saat sahələrini göstərir
function ujRedimNovChanged() {
    const sel = document.querySelector('input[name="ujRedimNov"]:checked');
    const fields = document.getElementById('ujIcazeFields');
    if (!fields) return;
    fields.style.display = sel && parseInt(sel.value) === 1 ? '' : 'none';
}

// ── Helpers ───────────────────────────────────────────────
function ujStatusBadge(status) {
    const map = { 1: ['Gözlənilir', 'gozlenilir'], 2: ['Təsdiqləndi', 'tesdiqlendi'], 3: ['Rədd edildi', 'redd'] };
    const [label, cls] = map[status] ?? ['—', 'gozlenilir'];
    return `<span class="uj-status uj-status--${cls}">${label}</span>`;
}

function ujDate(val) {
    if (!val) return '—';
    const d = new Date(val);
    return d.toLocaleDateString('az-AZ', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function ujToast(msg, type = 'info') {
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

// ── Reytinq Widget ────────────────────────────────────────
async function ujLoadReyting() {
    try {
        const res = await fetch('/User/Jeton/GetReyting');
        const json = await res.json();
        if (!json.success || !json.data) return;
        const d = json.data;
        const AY = ['', 'Yan', 'Fev', 'Mar', 'Apr', 'May', 'İyn',
                    'İyl', 'Avq', 'Sep', 'Okt', 'Noy', 'Dek'];
        document.getElementById('ujReytingXal').textContent = d.cemiXal + ' xal';
        const katEl = document.getElementById('ujReytingKat');
        katEl.textContent = d.kateqoriyaAd ?? '—';
        if (d.kateqoriyaReng) katEl.style.color = d.kateqoriyaReng;
        document.getElementById('ujReytingAy').textContent = `${AY[d.ay] ?? d.ay} ${d.il}`;
        document.getElementById('ujReytingPul').textContent = d.pulAmsali + 'x';
        document.getElementById('ujReytingSaat').textContent = d.saatAmsali + 'x';
        document.getElementById('ujReytingWidget').style.display = 'flex';
    } catch { /* reytinq yoxdursa widget gizli qalır */ }
}

// ── Init ──────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    ujLoadJetonlar();
    ujLoadReyting();
});

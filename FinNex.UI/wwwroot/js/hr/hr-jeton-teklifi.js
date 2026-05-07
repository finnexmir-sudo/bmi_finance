/* hr-jeton-teklifi.js — Motivasiya Paneli */

'use strict';

let jtAllData = [];
let jtCurrentFilter = 0;

const FILTER_MAP = {
    1: 'IsGununuAshdi',
    2: 'EvezediciOldu',
    3: 'TapshiriqTamamlandi',
    4: 'GecikmeCanDoldu'
};

const ICON_MAP = {
    1: 'bi bi-clock-history',
    2: 'bi bi-person-check',
    3: 'bi bi-check2-circle',
    4: 'bi bi-exclamation-triangle'
};

document.addEventListener('DOMContentLoaded', () => {
    jtLoadData();
    jtLoadTeyinatilar();

    document.querySelectorAll('.jt-tab').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.jt-tab').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            jtCurrentFilter = parseInt(btn.dataset.filter);
            jtRender();
        });
    });
});

async function jtLoadData() {
    document.getElementById('jtLoading').style.display = 'block';
    try {
        const res = await fetch('/HR/JetonTeklifi/GetData');
        const json = await res.json();
        if (json.success) {
            jtAllData = json.data || [];
            jtRender();
        } else {
            jtShowError('Məlumatlar yüklənəmədi.');
        }
    } catch (e) {
        jtShowError('Şəbəkə xətası: ' + e.message);
    } finally {
        document.getElementById('jtLoading').style.display = 'none';
    }
}

async function jtLoadTeyinatilar() {
    try {
        const res = await fetch('/HR/JetonTeklifi/GetJetonTeyinatlari');
        const json = await res.json();
        if (json.success) {
            const sel = document.getElementById('jtJetonTeyinatiId');
            // Clear existing options (keep placeholder)
            sel.innerHTML = '<option value="">— Seçin —</option>';
            json.data.forEach(t => {
                const opt = document.createElement('option');
                opt.value = t.id;
                opt.textContent = `${t.ad} (${t.saatDeyeri}s)`;
                opt.style.color = t.rengKodu;
                sel.appendChild(opt);
            });
        }
    } catch (e) {
        console.error('Jeton teyinatları yüklənəmədi:', e);
    }
}

function jtRender() {
    const container = document.getElementById('jtList');
    const empty = document.getElementById('jtEmpty');

    // Filter
    let filtered = jtAllData;
    if (jtCurrentFilter > 0) {
        const novEnum = jtCurrentFilter;
        filtered = jtAllData.filter(d => d.teklifNovu === novEnum);
    }

    // Update count badge
    document.getElementById('jtCount').textContent = jtAllData.length + ' tövsiyə';

    // Remove old cards (keep loading and empty divs)
    container.querySelectorAll('.jt-card').forEach(c => c.remove());

    if (filtered.length === 0) {
        empty.style.display = 'flex';
        empty.style.flexDirection = 'column';
        empty.style.alignItems = 'center';
    } else {
        empty.style.display = 'none';
        filtered.forEach(item => {
            container.appendChild(jtCreateCard(item));
        });
    }
}

function jtCreateCard(item) {
    const card = document.createElement('div');
    card.className = 'jt-card';
    card.dataset.id = item.id;
    card.dataset.nov = item.teklifNovu;

    const iconClass = ICON_MAP[item.teklifNovu] || 'bi bi-info-circle';
    const dateStr = jtFormatDate(item.yaradilmaTarixi);
    const dept = item.departament ? `${item.departament}${item.vezife ? ' / ' + item.vezife : ''}` : (item.vezife || '');

    card.innerHTML = `
        <div class="jt-card-icon" style="background:${item.teklifNovuReng}">
            <i class="${iconClass}"></i>
        </div>
        <div class="jt-card-body">
            <div class="jt-card-top">
                <span class="jt-card-name">${jtEsc(item.isciAd)}</span>
                ${dept ? `<span class="jt-card-meta">${jtEsc(dept)}</span>` : ''}
                <span class="jt-card-badge" style="background:${item.teklifNovuReng}">${jtEsc(item.teklifNovuAd)}</span>
            </div>
            <p class="jt-card-text">${jtEsc(item.metn)}</p>
            <span class="jt-card-date"><i class="bi bi-clock"></i> ${dateStr}</span>
        </div>
        <div class="jt-card-actions">
            <button class="jt-btn jt-btn--success" onclick="jtOpenModal(${item.id}, '${jtEsc(item.isciAd)}', '${jtEsc(item.metn)}')">
                <i class="bi bi-award-fill"></i> Jeton Ver
            </button>
            <button class="jt-btn jt-btn--ghost" onclick="jtReddet(${item.id})">
                <i class="bi bi-x"></i> Keç
            </button>
        </div>
    `;
    return card;
}

function jtOpenModal(teklifId, isciAd, metn) {
    document.getElementById('jtTeklifId').value = teklifId;
    document.getElementById('jtModalInfo').innerHTML =
        `<strong>${jtEsc(isciAd)}</strong><br><span>${jtEsc(metn)}</span>`;
    document.getElementById('jtJetonTeyinatiId').value = '';
    document.getElementById('jtQeyd').value = '';
    document.getElementById('jtOverlay').classList.add('show');
    document.getElementById('jtModal').classList.add('show');
}

function jtCloseModal() {
    document.getElementById('jtOverlay').classList.remove('show');
    document.getElementById('jtModal').classList.remove('show');
}

async function jtSubmit() {
    const teklifId = parseInt(document.getElementById('jtTeklifId').value);
    const jetonTeyinatiId = parseInt(document.getElementById('jtJetonTeyinatiId').value);
    const qeyd = document.getElementById('jtQeyd').value.trim();

    if (!jetonTeyinatiId) {
        alert('Zəhmət olmasa jeton növü seçin.');
        return;
    }

    try {
        const res = await fetch('/HR/JetonTeklifi/JetonVer', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ teklifId, jetonTeyinatiId, qeyd: qeyd || null })
        });
        const json = await res.json();
        if (json.success) {
            jtCloseModal();
            jtAllData = jtAllData.filter(d => d.id !== teklifId);
            jtRender();
            jtToast('Jeton uğurla verildi!', 'success');
        } else {
            alert(json.message || 'Xəta baş verdi.');
        }
    } catch (e) {
        alert('Şəbəkə xətası: ' + e.message);
    }
}

async function jtReddet(teklifId) {
    if (!confirm('Bu tövsiyəni keçmək istədiyinizdən əminsinizmi?')) return;

    try {
        const res = await fetch('/HR/JetonTeklifi/Reddet', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(teklifId)
        });
        const json = await res.json();
        if (json.success) {
            jtAllData = jtAllData.filter(d => d.id !== teklifId);
            jtRender();
            jtToast('Tövsiyə keçildi.', 'info');
        } else {
            alert(json.message || 'Xəta baş verdi.');
        }
    } catch (e) {
        alert('Şəbəkə xətası: ' + e.message);
    }
}

function jtShowError(msg) {
    const container = document.getElementById('jtList');
    container.innerHTML = `<div class="jt-empty" style="color:#ef4444"><i class="bi bi-exclamation-circle"></i><p>${msg}</p></div>`;
}

function jtFormatDate(isoStr) {
    if (!isoStr) return '—';
    const d = new Date(isoStr);
    const pad = n => String(n).padStart(2, '0');
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function jtEsc(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function jtToast(msg, type) {
    const t = document.createElement('div');
    t.style.cssText = `
        position:fixed;bottom:24px;right:24px;z-index:9999;
        background:${type === 'success' ? '#10b981' : '#3b82f6'};
        color:#fff;padding:12px 20px;border-radius:10px;
        font-size:0.9rem;font-weight:600;
        box-shadow:0 4px 16px rgba(0,0,0,0.2);
        animation:jtFadeIn 0.3s ease;
    `;
    t.textContent = msg;
    document.body.appendChild(t);
    setTimeout(() => t.remove(), 3000);
}

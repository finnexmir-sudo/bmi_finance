/* hr-reyting-parametr.js */
'use strict';

document.addEventListener('DOMContentLoaded', () => {
    rpLoadKateqoriyalar();
    rpLoadParametrlər();
});

// ── Kateqoriyalar ────────────────────────────────────────────

async function rpLoadKateqoriyalar() {
    const el = document.getElementById('rpKatList');
    el.innerHTML = '<div class="rp-empty">Yüklənir…</div>';
    const res = await fetch('/HR/ReytingParametr/GetKateqoriyalar');
    const json = await res.json();
    if (!json.data || !json.data.length) {
        el.innerHTML = '<div class="rp-empty">Hələ kateqoriya yoxdur. Yeni əlavə edin.</div>';
        return;
    }
    el.innerHTML = '<div class="rp-kat-list">' + json.data.map(k => `
        <div class="rp-kat-item ${!k.aktivdir ? 'rp-inactive' : ''}">
            <div class="rp-kat-dot" style="background:${k.rengKodu ?? '#9ca3af'}"></div>
            <div class="rp-kat-info">
                <div class="rp-kat-ad">${k.ad}</div>
                <div class="rp-kat-meta">${k.minXal} – ${k.maxXal === 9999 ? '∞' : k.maxXal} xal · Sıra ${k.sira}</div>
            </div>
            <div class="rp-kat-amsallar">
                <span class="rp-amsal-badge">💰 ${k.pulAmsali}x</span>
                <span class="rp-amsal-badge">⏱ ${k.saatAmsali}x</span>
            </div>
            <div class="rp-kat-actions">
                <button class="rp-btn rp-btn--icon" onclick="rpOpenKatModal(${k.id})" title="Düzəlt">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="rp-btn rp-btn--icon rp-btn--danger" onclick="rpDeleteKat(${k.id})" title="Sil">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        </div>`).join('') + '</div>';
}

let rpKatlarCache = [];

async function rpOpenKatModal(id) {
    if (!rpKatlarCache.length) {
        const r = await fetch('/HR/ReytingParametr/GetKateqoriyalar');
        rpKatlarCache = (await r.json()).data || [];
    }
    const kat = rpKatlarCache.find(k => k.id === id);
    document.getElementById('rpKatId').value   = kat?.id ?? 0;
    document.getElementById('rpKatAd').value   = kat?.ad ?? '';
    document.getElementById('rpKatReng').value = kat?.rengKodu ?? '#6366f1';
    document.getElementById('rpKatRengPicker').value = kat?.rengKodu ?? '#6366f1';
    document.getElementById('rpKatMin').value  = kat?.minXal ?? 0;
    document.getElementById('rpKatMax').value  = kat?.maxXal ?? 9999;
    document.getElementById('rpKatPul').value  = kat?.pulAmsali ?? 1.00;
    document.getElementById('rpKatSaat').value = kat?.saatAmsali ?? 1.00;
    document.getElementById('rpKatSira').value = kat?.sira ?? 1;
    document.getElementById('rpKatAktiv').checked = kat?.aktivdir ?? true;
    document.getElementById('rpKatModalTitle').textContent = id ? 'Kateqoriyanı düzəlt' : 'Yeni kateqoriya';
    document.getElementById('rpKatOverlay').classList.add('rp-open');
    document.getElementById('rpKatModal').classList.add('rp-open');
}

function rpCloseKatModal() {
    document.getElementById('rpKatOverlay').classList.remove('rp-open');
    document.getElementById('rpKatModal').classList.remove('rp-open');
}

async function rpSaveKat() {
    const dto = {
        id: parseInt(document.getElementById('rpKatId').value),
        ad: document.getElementById('rpKatAd').value.trim(),
        rengKodu: document.getElementById('rpKatReng').value.trim(),
        minXal: parseInt(document.getElementById('rpKatMin').value),
        maxXal: parseInt(document.getElementById('rpKatMax').value),
        pulAmsali: parseFloat(document.getElementById('rpKatPul').value),
        saatAmsali: parseFloat(document.getElementById('rpKatSaat').value),
        sira: parseInt(document.getElementById('rpKatSira').value),
        aktivdir: document.getElementById('rpKatAktiv').checked
    };
    const res = await fetch('/HR/ReytingParametr/KateqoriyaSaxla', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    });
    const json = await res.json();
    rpToast(json.message, json.success ? 'success' : 'error');
    if (json.success) {
        rpCloseKatModal();
        rpKatlarCache = [];
        rpLoadKateqoriyalar();
    }
}

async function rpDeleteKat(id) {
    if (!confirm('Bu kateqoriyanı silmək istədiyinizə əminsiniz?')) return;
    const res = await fetch('/HR/ReytingParametr/KateqoriyaSil', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(id)
    });
    const json = await res.json();
    rpToast(json.message, json.success ? 'success' : 'error');
    if (json.success) { rpKatlarCache = []; rpLoadKateqoriyalar(); }
}

// ── Parametrlər ──────────────────────────────────────────────

async function rpLoadParametrlər() {
    const el = document.getElementById('rpParamList');
    el.innerHTML = '<div class="rp-empty">Yüklənir…</div>';
    const res = await fetch('/HR/ReytingParametr/GetParametrl%C9%99r');
    const json = await res.json();
    if (!json.data || !json.data.length) {
        el.innerHTML = '<div class="rp-empty">Hələ parametr yoxdur. Yeni əlavə edin.</div>';
        return;
    }
    el.innerHTML = '<div class="rp-param-list">' + json.data.map(p => `
        <div class="rp-param-item ${!p.aktivdir ? 'rp-inactive' : ''}">
            <div class="rp-param-hadise">${p.hadiseAdi}</div>
            <div class="rp-param-ad">${p.ad}</div>
            <div class="rp-param-xal ${p.xalDeyeri >= 0 ? 'rp-param-xal--pos' : 'rp-param-xal--neg'}">
                ${p.xalDeyeri > 0 ? '+' : ''}${p.xalDeyeri}
            </div>
            <button class="rp-btn rp-btn--icon" onclick="rpOpenParamModal(${p.id})" title="Düzəlt">
                <i class="bi bi-pencil"></i>
            </button>
        </div>`).join('') + '</div>';
}

let rpParamlarCache = [];

async function rpOpenParamModal(id) {
    if (!rpParamlarCache.length) {
        const r = await fetch('/HR/ReytingParametr/GetParametrl%C9%99r');
        rpParamlarCache = (await r.json()).data || [];
    }
    const p = rpParamlarCache.find(x => x.id === id);
    document.getElementById('rpParamId').value     = p?.id ?? 0;
    document.getElementById('rpParamHadise').value = p?.hadiseNovu ?? 1;
    document.getElementById('rpParamAd').value     = p?.ad ?? '';
    document.getElementById('rpParamXal').value    = p?.xalDeyeri ?? 0;
    document.getElementById('rpParamAktiv').checked = p?.aktivdir ?? true;
    document.getElementById('rpParamModalTitle').textContent = id ? 'Parametri düzəlt' : 'Yeni parametr';
    document.getElementById('rpParamOverlay').classList.add('rp-open');
    document.getElementById('rpParamModal').classList.add('rp-open');
}

function rpCloseParamModal() {
    document.getElementById('rpParamOverlay').classList.remove('rp-open');
    document.getElementById('rpParamModal').classList.remove('rp-open');
}

async function rpSaveParam() {
    const dto = {
        id: parseInt(document.getElementById('rpParamId').value),
        hadiseNovu: parseInt(document.getElementById('rpParamHadise').value),
        ad: document.getElementById('rpParamAd').value.trim(),
        xalDeyeri: parseInt(document.getElementById('rpParamXal').value),
        aktivdir: document.getElementById('rpParamAktiv').checked
    };
    const res = await fetch('/HR/ReytingParametr/ParametrSaxla', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    });
    const json = await res.json();
    rpToast(json.message, json.success ? 'success' : 'error');
    if (json.success) {
        rpCloseParamModal();
        rpParamlarCache = [];
        rpLoadParametrlər();
    }
}

// ── Toast ────────────────────────────────────────────────────

function rpToast(msg, type = 'info') {
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

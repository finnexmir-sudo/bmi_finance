/* hr-jeton-analitika.js — Jeton Analitika Dashboard */
'use strict';

const JA_COLORS = {
    musbat : 'rgba(99,102,241,.8)',
    menfi  : 'rgba(239,68,68,.75)',
    indigo : '#6366f1',
    green  : '#22c55e',
    amber  : '#f59e0b',
    slate  : '#94a3b8',
    PIE    : ['#6366f1','#22c55e','#f59e0b','#ef4444','#8b5cf6','#06b6d4','#f97316','#84cc16']
};

let jaCharts = {};

// ── Bootstrap ────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', jaRefresh);

async function jaRefresh() {
    jaDestroyCharts();
    document.getElementById('jaKpiGrid').innerHTML =
        Array(6).fill('<div class="ja-kpi-skeleton"></div>').join('');
    document.getElementById('jaAnomaliyaList').innerHTML =
        '<div class="ja-empty">Yüklənir…</div>';

    try {
        const res  = await fetch('/HR/JetonAnalitika/GetData');
        const json = await res.json();
        jaRenderKpi(json.kpi);
        jaRenderTrend(json.trend);
        jaRenderMaliyye(json.kpi);
        jaRenderDept(json.departament);
        jaRenderQara(json.qaraJetonSebebler);
        jaRenderAnom(json.anomaliyalar);
        jaRenderYukseleK(json.kpi);
    } catch (e) {
        document.getElementById('jaKpiGrid').innerHTML =
            '<p style="color:#ef4444;padding:16px">Məlumat yüklənərkən xəta baş verdi.</p>';
    }
}

// ── KPI kartları ──────────────────────────────────────────────
function jaRenderKpi(kpi) {
    const cards = [
        {
            icon: 'bi-award-fill', cls: 'green',
            label: 'Aktiv Müsbət Jeton',
            val: kpi.toplamMusbetJetonSayi,
            sub: `${jaFmt(kpi.toplamSaat)} saat · ${jaFmt(kpi.toplamAzn)} AZN`
        },
        {
            icon: 'bi-slash-circle-fill', cls: 'red',
            label: 'Aktiv Qara Jeton',
            val: kpi.toplamMenfiJetonSayi,
            sub: kpi.toplamMenfiJetonSayi > 0 ? 'İşçilər bloklanıb' : 'Heç kim bloklanmayıb',
            subCls: kpi.toplamMenfiJetonSayi > 0 ? 'danger' : ''
        },
        {
            icon: 'bi-clock-history', cls: 'indigo',
            label: 'Gözləyən Xərcləmə',
            val: kpi.gozleyenXercleme,
            sub: 'HR təsdiqini gözləyir'
        },
        {
            icon: 'bi-shield-exclamation', cls: 'amber',
            label: 'Yüksək Səlahiyyətli',
            val: kpi.yukselekSahiyyetli,
            sub: '>24 saat — prioritet',
            subCls: kpi.yukselekSahiyyetli > 0 ? 'warn' : ''
        },
        {
            icon: 'bi-cash-coin', cls: 'blue',
            label: 'Maliyyə Öhdəliyi',
            val: jaFmt(kpi.toplamAzn) + ' ₼',
            sub: 'Bank üzrə gələcək xərc'
        },
        {
            icon: 'bi-bar-chart-fill', cls: 'slate',
            label: 'Müsbət / Mənfi',
            val: kpi.toplamMenfiJetonSayi === 0
                ? '∞'
                : (kpi.toplamMusbetJetonSayi / kpi.toplamMenfiJetonSayi).toFixed(1) + 'x',
            sub: 'Müsbət jeton nisbəti'
        }
    ];

    document.getElementById('jaKpiGrid').innerHTML = cards.map(c => `
        <div class="ja-kpi">
            <div class="ja-kpi-icon ja-kpi-icon--${c.cls}"><i class="bi ${c.icon}"></i></div>
            <div class="ja-kpi-label">${c.label}</div>
            <div class="ja-kpi-val">${c.val}</div>
            <div class="ja-kpi-sub ${c.subCls ? 'ja-kpi-sub--' + c.subCls : ''}">${c.sub}</div>
        </div>`).join('');
}

// ── Trend qrafiki (Line) ──────────────────────────────────────
function jaRenderTrend(trend) {
    if (!trend || !trend.length) {
        document.getElementById('jaTrendChart').closest('.ja-chart-card').innerHTML +=
            '<p class="ja-empty">Məlumat yoxdur.</p>';
        return;
    }
    const ctx = document.getElementById('jaTrendChart');
    jaCharts.trend = new Chart(ctx, {
        type: 'line',
        data: {
            labels: trend.map(t => t.ay),
            datasets: [
                {
                    label: 'Müsbət Jeton',
                    data: trend.map(t => t.musbat),
                    borderColor: JA_COLORS.indigo, backgroundColor: 'rgba(99,102,241,.1)',
                    borderWidth: 2.5, tension: .4, fill: true, pointRadius: 5,
                    pointBackgroundColor: JA_COLORS.indigo
                },
                {
                    label: 'Qara Jeton',
                    data: trend.map(t => t.menfi),
                    borderColor: JA_COLORS.menfi, backgroundColor: 'rgba(239,68,68,.07)',
                    borderWidth: 2.5, tension: .4, fill: true, pointRadius: 5,
                    pointBackgroundColor: '#ef4444'
                }
            ]
        },
        options: {
            responsive: true, maintainAspectRatio: true,
            plugins: { legend: { position: 'top' } },
            scales: {
                y: { beginAtZero: true, ticks: { stepSize: 1 } }
            }
        }
    });
}

// ── Maliyyə dairəsi (Doughnut) ────────────────────────────────
function jaRenderMaliyye(kpi) {
    const ctx = document.getElementById('jaMaliyyeChart');
    jaCharts.maliyye = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Müsbət Balans (saat)', 'Qara Jeton (sayı)'],
            datasets: [{
                data: [kpi.toplamSaat || 0.001, kpi.toplamMenfiJetonSayi || 0.001],
                backgroundColor: [JA_COLORS.indigo, '#ef4444'],
                borderWidth: 0, hoverOffset: 8
            }]
        },
        options: {
            responsive: true, cutout: '72%',
            plugins: {
                legend: { position: 'bottom', labels: { boxWidth: 12, padding: 14 } }
            }
        }
    });

    document.getElementById('jaMaliyyeInfo').innerHTML = `
        <div class="ja-maliyye-stat">
            <div class="ja-maliyye-stat-val">${jaFmt(kpi.toplamSaat)}</div>
            <div class="ja-maliyye-stat-lab">Cəmi saat</div>
        </div>
        <div class="ja-maliyye-stat">
            <div class="ja-maliyye-stat-val">${jaFmt(kpi.toplamAzn)} ₼</div>
            <div class="ja-maliyye-stat-lab">AZN ekvivalent</div>
        </div>
        <div class="ja-maliyye-stat">
            <div class="ja-maliyye-stat-val">${kpi.toplamMenfiJetonSayi}</div>
            <div class="ja-maliyye-stat-lab">Aktiv Qara</div>
        </div>`;
}

// ── Departament bar qrafiki (Horizontal Bar) ──────────────────
function jaRenderDept(departament) {
    if (!departament || !departament.length) {
        document.getElementById('jaDeptChart').closest('.ja-chart-card').innerHTML +=
            '<p class="ja-empty">Məlumat yoxdur.</p>';
        return;
    }
    const ctx = document.getElementById('jaDeptChart');
    jaCharts.dept = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: departament.map(d => d.ad),
            datasets: [
                {
                    label: 'Müsbət Jeton',
                    data: departament.map(d => d.musbat),
                    backgroundColor: 'rgba(99,102,241,.75)',
                    borderRadius: 5
                },
                {
                    label: 'Qara Jeton',
                    data: departament.map(d => d.menfi),
                    backgroundColor: 'rgba(239,68,68,.7)',
                    borderRadius: 5
                }
            ]
        },
        options: {
            indexAxis: 'y',
            responsive: true, maintainAspectRatio: false,
            plugins: { legend: { position: 'top' } },
            scales: {
                x: { beginAtZero: true, ticks: { stepSize: 1 } },
                y: { ticks: { font: { size: 12 } } }
            }
        }
    });
    ctx.style.minHeight = Math.max(200, departament.length * 44) + 'px';
}

// ── Qara jeton səbəb qrafiki (Doughnut) ──────────────────────
function jaRenderQara(sebebler) {
    if (!sebebler || !sebebler.length) {
        const el = document.getElementById('jaQaraChart');
        el.closest('.ja-chart-card').querySelector('.ja-chart-sub').textContent = '';
        el.closest('.ja-chart-card').insertAdjacentHTML('beforeend',
            '<p class="ja-empty-ok"><i class="bi bi-check-circle-fill"></i> Qara jeton yoxdur</p>');
        el.style.display = 'none';
        return;
    }
    const ctx = document.getElementById('jaQaraChart');
    jaCharts.qara = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: sebebler.map(s => s.sebeb),
            datasets: [{
                data: sebebler.map(s => s.sayi),
                backgroundColor: JA_COLORS.PIE,
                borderWidth: 0, hoverOffset: 8
            }]
        },
        options: {
            responsive: true, cutout: '60%',
            plugins: {
                legend: { position: 'right', labels: { boxWidth: 12, padding: 10, font: { size: 11 } } }
            }
        }
    });
}

// ── Anomaliya cədvəli ─────────────────────────────────────────
function jaRenderAnom(anomaliyalar) {
    const el = document.getElementById('jaAnomaliyaList');
    if (!anomaliyalar || !anomaliyalar.length) {
        el.innerHTML = '<div class="ja-empty-ok"><i class="bi bi-check-circle-fill"></i> Anomaliya aşkarlanmadı</div>';
        return;
    }
    el.innerHTML = `
        <table class="ja-anom-table">
            <thead>
                <tr>
                    <th>İstifadəçi</th>
                    <th>Ay</th>
                    <th>Payladığı jeton</th>
                    <th>Risk Səviyyəsi</th>
                </tr>
            </thead>
            <tbody>
                ${anomaliyalar.map(a => `
                <tr>
                    <td><strong>${a.verenAd}</strong></td>
                    <td>${a.ay}</td>
                    <td><strong>${a.sayi}</strong> jeton</td>
                    <td>
                        <span class="ja-risk-badge ja-risk-badge--${a.risk}">
                            <i class="bi bi-${a.risk === 'critical' ? 'exclamation-circle' : 'exclamation-triangle'}-fill"></i>
                            ${a.risk === 'critical' ? 'Kritik' : 'Yüksək'}
                        </span>
                    </td>
                </tr>`).join('')}
            </tbody>
        </table>`;
}

// ── Yüksək səlahiyyətli xəbərdarlıq ──────────────────────────
function jaRenderYukseleK(kpi) {
    const el = document.getElementById('jaYukselekAlert');
    if (kpi.yukselekSahiyyetli > 0) {
        el.style.display = 'flex';
        document.getElementById('jaYukselekAlertText').textContent =
            `${kpi.yukselekSahiyyetli} ədəd 24 saatdan çox dəyərli xərcləmə sorğusu HR təsdiqini gözləyir!`;
    }
}

// ── Köməkçilər ────────────────────────────────────────────────
function jaFmt(n) {
    if (n == null) return '0';
    return parseFloat(n).toLocaleString('az-AZ', { maximumFractionDigits: 2 });
}

function jaDestroyCharts() {
    Object.values(jaCharts).forEach(c => { try { c.destroy(); } catch {} });
    jaCharts = {};
}

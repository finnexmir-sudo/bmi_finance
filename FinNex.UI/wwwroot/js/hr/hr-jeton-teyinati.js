/* hr-jeton-teyinati.js — Jeton kataloqu idarəetməsi */
'use strict';

let hjtCurrentIkon = '';
let hjtCurrentReng = '';

function hjtOpenEditModal(t) {
    document.getElementById('hjtId').value = t.id;
    document.getElementById('hjtAd').value = t.ad ?? '';
    document.getElementById('hjtSaat').value = t.saatDeyeri ?? 0;
    document.getElementById('hjtTesvir').value = t.tesvir ?? '';
    document.getElementById('hjtAktiv').checked = !!t.aktivdir;

    // İkon və rəng dəyişdirilmir; mövcud dəyərləri saxlayırıq
    hjtCurrentIkon = t.ikon ?? '';
    hjtCurrentReng = t.rengKodu ?? '';

    document.getElementById('hjtOverlay').classList.add('hj-open');
    document.getElementById('hjtModal').classList.add('hj-open');
}

function hjtCloseEditModal() {
    document.getElementById('hjtOverlay').classList.remove('hj-open');
    document.getElementById('hjtModal').classList.remove('hj-open');
}

async function hjtSubmitEdit() {
    const dto = {
        id: parseInt(document.getElementById('hjtId').value),
        ad: document.getElementById('hjtAd').value.trim(),
        saatDeyeri: parseFloat(document.getElementById('hjtSaat').value) || 0,
        tesvir: document.getElementById('hjtTesvir').value.trim(),
        ikon: hjtCurrentIkon,
        rengKodu: hjtCurrentReng,
        aktivdir: document.getElementById('hjtAktiv').checked
    };

    if (!dto.ad) return hjtToast('Ad daxil edin.', 'warn');

    const res = await fetch('/HR/JetonTeyinati/Yenile', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    });
    const json = await res.json();

    if (json.success) {
        hjtToast(json.message || 'Yeniləndi.', 'success');
        hjtCloseEditModal();
        setTimeout(() => location.reload(), 600);
    } else {
        hjtToast(json.message || 'Xəta baş verdi.', 'error');
    }
}

function hjtToast(msg, type = 'info') {
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

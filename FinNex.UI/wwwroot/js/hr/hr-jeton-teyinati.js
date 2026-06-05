/* hr-jeton-teyinati.js — Jeton kataloqu idarəetməsi */
'use strict';

let hjtCurrentIkon = '';
let hjtCurrentReng = '';

// ── Edit modal ────────────────────────────────────────────────────────────────

function hjtOpenEditModal(t) {
    document.getElementById('hjtId').value = t.id;
    document.getElementById('hjtAd').value = t.ad ?? '';
    document.getElementById('hjtTesvir').value = t.tesvir ?? '';
    document.getElementById('hjtAktiv').checked = !!t.aktivdir;
    document.getElementById('hjtBirbasaOdenishli').checked = !!t.birbasaOdenishli;

    // Vahid: 2=Gun, başqası=Saat
    const isGun = t.vahid === 2;
    document.getElementById('hjtVahid').value = isGun ? 'gun' : 'saat';
    document.getElementById('hjtSaat').value = isGun ? (t.saatDeyeri / 8) : (t.saatDeyeri ?? 0);
    hjtSyncEditVahid();

    hjtCurrentIkon = t.ikon ?? '';
    hjtCurrentReng = t.rengKodu ?? '';

    document.getElementById('hjtOverlay').classList.add('hj-open');
    document.getElementById('hjtModal').classList.add('hj-open');
}

function hjtCloseEditModal() {
    document.getElementById('hjtOverlay').classList.remove('hj-open');
    document.getElementById('hjtModal').classList.remove('hj-open');
}

function hjtSyncEditVahid() {
    const vahid = document.getElementById('hjtVahid').value;
    const val = parseFloat(document.getElementById('hjtSaat').value) || 0;
    const hint = document.getElementById('hjtSaatHint');
    if (vahid === 'gun') {
        hint.textContent = `= ${(val * 8).toFixed(2).replace(/\.?0+$/, '')} saat saxlanılacaq`;
    } else {
        const gun = val / 8;
        hint.textContent = gun > 0 && Number.isInteger(gun)
            ? `= ${gun} gün (${val} saat)`
            : '';
    }
}

async function hjtSubmitEdit() {
    const vahid = document.getElementById('hjtVahid').value;
    const rawVal = parseFloat(document.getElementById('hjtSaat').value) || 0;
    const saatDeyeri = vahid === 'gun' ? rawVal * 8 : rawVal;

    const dto = {
        id:               parseInt(document.getElementById('hjtId').value),
        ad:               document.getElementById('hjtAd').value.trim(),
        saatDeyeri:       saatDeyeri,
        vahid:            document.getElementById('hjtVahid').value === 'gun' ? 2 : 1,
        tesvir:           document.getElementById('hjtTesvir').value.trim(),
        ikon:             hjtCurrentIkon,
        rengKodu:         hjtCurrentReng,
        birbasaOdenishli: document.getElementById('hjtBirbasaOdenishli').checked,
        aktivdir:         document.getElementById('hjtAktiv').checked
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

// ── Create modal ─────────────────────────────────────────────────────────────

// Tracks the last valid JetonRengi enum value (1-4) separately from dropdown display
let hjtcLastRengiEnum = 1;

function hjtOpenCreateModal() {
    document.getElementById('hjtcAd').value = '';
    document.getElementById('hjtcNov').value = '1';
    hjtcSetRengiPreset('1', '#cd7f32');
    document.getElementById('hjtcIkon').value = 'bi bi-award-fill';
    document.getElementById('hjtcSaat').value = '1';
    document.getElementById('hjtcVahid').value = 'saat';
    document.getElementById('hjtcBirbasaOdenishli').checked = false;
    document.getElementById('hjtcTesvir').value = '';
    hjtcLastRengiEnum = 1;
    hjtcUpdateHint();
    document.getElementById('hjtCreateOverlay').classList.add('hj-open');
    document.getElementById('hjtCreateModal').classList.add('hj-open');
}

function hjtCloseCreateModal() {
    document.getElementById('hjtCreateOverlay').classList.remove('hj-open');
    document.getElementById('hjtCreateModal').classList.remove('hj-open');
}

// Called when user picks a preset from dropdown
function hjtcSyncRengKodu() {
    const sel = document.getElementById('hjtcRengi');
    const val = sel.value;
    if (!val || val === 'ferdi') return;
    const opt = sel.options[sel.selectedIndex];
    const kod = opt.getAttribute('data-kod') || '#6b7280';
    hjtcLastRengiEnum = parseInt(val);
    document.getElementById('hjtcFerdiOpt').style.display = 'none';
    document.getElementById('hjtcRengKodu').value = kod;
    document.getElementById('hjtcRengPicker').value = kod;
}

// Called when user manually changes the color picker or hex input
function hjtcManualColorChange() {
    const kod = document.getElementById('hjtcRengKodu').value.trim();
    // Check if the new color matches any preset exactly
    const sel = document.getElementById('hjtcRengi');
    let matched = false;
    for (let i = 0; i < sel.options.length; i++) {
        const opt = sel.options[i];
        if (opt.getAttribute('data-kod') && opt.getAttribute('data-kod').toLowerCase() === kod.toLowerCase()) {
            sel.value = opt.value;
            hjtcLastRengiEnum = parseInt(opt.value);
            document.getElementById('hjtcFerdiOpt').style.display = 'none';
            matched = true;
            break;
        }
    }
    if (!matched) {
        const ferdiOpt = document.getElementById('hjtcFerdiOpt');
        ferdiOpt.style.display = '';
        sel.value = 'ferdi';
    }
}

function hjtcSetRengiPreset(val, kod) {
    document.getElementById('hjtcRengi').value = val;
    document.getElementById('hjtcFerdiOpt').style.display = 'none';
    document.getElementById('hjtcRengKodu').value = kod;
    document.getElementById('hjtcRengPicker').value = kod;
}

function hjtcUpdateHint() {
    const vahid = document.getElementById('hjtcVahid').value;
    const val = parseFloat(document.getElementById('hjtcSaat').value) || 0;
    const hint = document.getElementById('hjtcSaatHint');
    if (vahid === 'gun') {
        hint.textContent = `= ${(val * 8).toFixed(2).replace(/\.?0+$/, '')} saat saxlanılacaq`;
    } else {
        const gun = val / 8;
        hint.textContent = Number.isInteger(gun) && gun > 0
            ? `= ${val} saat (${gun} gün)`
            : `= ${val} saat saxlanılacaq`;
    }
}

async function hjtSubmitCreate() {
    const vahid = document.getElementById('hjtcVahid').value;
    const rawVal = parseFloat(document.getElementById('hjtcSaat').value) || 0;
    const saatDeyeri = vahid === 'gun' ? rawVal * 8 : rawVal;

    const dto = {
        ad:               document.getElementById('hjtcAd').value.trim(),
        nov:              parseInt(document.getElementById('hjtcNov').value),
        rengi:            hjtcLastRengiEnum,
        saatDeyeri:       saatDeyeri,
        vahid:            document.getElementById('hjtcVahid').value === 'gun' ? 2 : 1,
        ikon:             document.getElementById('hjtcIkon').value.trim() || 'bi bi-award-fill',
        rengKodu:         document.getElementById('hjtcRengKodu').value.trim() || '#6b7280',
        birbasaOdenishli: document.getElementById('hjtcBirbasaOdenishli').checked,
        tesvir:           document.getElementById('hjtcTesvir').value.trim()
    };

    if (!dto.ad) return hjtToast('Ad daxil edin.', 'warn');

    const res = await fetch('/HR/JetonTeyinati/Yarat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
    });
    const json = await res.json();

    if (json.success) {
        hjtToast(json.message || 'Yaradıldı.', 'success');
        hjtCloseCreateModal();
        setTimeout(() => location.reload(), 600);
    } else {
        hjtToast(json.message || 'Xəta baş verdi.', 'error');
    }
}

// ── Toast ─────────────────────────────────────────────────────────────────────

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

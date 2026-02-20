/* ================================================================
   payment-bank.js  –  Bank / Müştəri axtarışı + Məbləği sözə çevir
================================================================ */
document.addEventListener('DOMContentLoaded', function () {

    /* ── Yardımçı ─────────────────────────────────────────────── */
    function el(id)   { return document.getElementById(id); }
    function setVal(id, v) { const e = el(id); if (e) e.value = v ?? ''; }
    function hide(id) { const e = el(id); if (e) e.style.display = 'none'; }

    function xeta(elId, msg) {
        const e = el(elId);
        if (!e) return;
        e.textContent = msg;
        e.style.display = msg ? 'block' : 'none';
    }

    /* ── Bank axtarışı ────────────────────────────────────────── */
    async function bankAxtar(kodId, errId, f) {
        xeta(errId, '');
        const kod = (el(kodId)?.value ?? '').trim();
        if (!kod) { xeta(errId, 'Bank kodunu daxil edin.'); return; }

        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/BankiKodlaAxtar?kod=' + encodeURIComponent(kod));
            if (!r.ok) { xeta(errId, 'Server xətası: ' + r.status); return; }
            const d = await r.json();

            if (!d.tapildi) {
                xeta(errId, '"' + kod + '" kodu ilə bank tapılmadı.');
                clearBank(f);
                return;
            }
            setVal(f.hiddenId,  d.id);
            setVal(f.ad,        d.ad);
            setVal(f.kod,       d.kod);
            setVal(f.voen,      d.voen);
            setVal(f.muxHesab,  d.muxHesab);
            setVal(f.swift,     d.swiftBic);
        } catch (e) {
            xeta(errId, 'Xəta: ' + e.message);
        }
    }

    function clearBank(f) {
        setVal(f.hiddenId, ''); setVal(f.ad, ''); setVal(f.kod, '');
        setVal(f.voen, ''); setVal(f.muxHesab, ''); setVal(f.swift, '');
    }

    function baglaBank(btnId, kodId, errId, f) {
        el(btnId)?.addEventListener('click', () => bankAxtar(kodId, errId, f));
        el(kodId)?.addEventListener('keydown', e => {
            if (e.key === 'Enter') { e.preventDefault(); bankAxtar(kodId, errId, f); }
        });
    }

    baglaBank('btnOduyenBankAxtar', 'OduyenBankKodSearch', 'OduyenBankXeta', {
        hiddenId: 'OduyenBankIdHidden',
        ad: 'OduyenBankAd', kod: 'OduyenBankKod',
        voen: 'OduyenBankVoen', muxHesab: 'OduyenBankMuxbirHesab', swift: 'OduyenBankSwift'
    });

    baglaBank('btnAlanBankAxtar', 'AlanBankKodSearch', 'AlanBankXeta', {
        hiddenId: 'AlanBankIdHidden',
        ad: 'AlanBankAd', kod: 'AlanBankKod',
        voen: 'AlanBankVoen', muxHesab: 'AlanBankMuxbirHesab', swift: 'AlanBankSwift'
    });

    /* ── Müştəri axtarışı ─────────────────────────────────────── */
    async function musteriAxtar(voenId, errId, f) {
        xeta(errId, '');
        const voen = (el(voenId)?.value ?? '').trim();
        if (!voen) { xeta(errId, 'VOEN daxil edin.'); return; }

        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/MusteriVoenleAxtar?voen=' + encodeURIComponent(voen));
            if (!r.ok) { xeta(errId, 'Server xətası: ' + r.status); return; }
            const d = await r.json();

            if (!d.tapildi) {
                xeta(errId, '"' + voen + '" VOEN-li müştəri tapılmadı.');
                clearMusteri(f);
                return;
            }

            setVal(f.musteriId, d.id);
            setVal(f.ad,        d.ad);
            setVal(f.voen,      d.voen);

            // İlk hesabı avtomatik seç
            if (d.hesablar && d.hesablar.length > 0) {
                setVal(f.hesabId,  d.hesablar[0].id);
                setVal(f.hesabIban, d.hesablar[0].iban);
            } else {
                setVal(f.hesabId, ''); setVal(f.hesabIban, '');
            }
        } catch (e) {
            xeta(errId, 'Xəta: ' + e.message);
        }
    }

    function clearMusteri(f) {
        setVal(f.musteriId, ''); setVal(f.ad, '');
        setVal(f.voen, ''); setVal(f.hesabId, ''); setVal(f.hesabIban, '');
    }

    function baglaMusteri(btnId, voenId, errId, f) {
        el(btnId)?.addEventListener('click', () => musteriAxtar(voenId, errId, f));
        el(voenId)?.addEventListener('keydown', e => {
            if (e.key === 'Enter') { e.preventDefault(); musteriAxtar(voenId, errId, f); }
        });
    }

    baglaMusteri('btnOduyenMusteriAxtar', 'OduyenMusteriVoenSearch', 'OduyenMusteriXeta', {
        musteriId:  'OduyenMusteriIdHidden',
        hesabId:    'OduyenHesabIdHidden',
        ad:         'OduyenMusteriAd',
        voen:       'OduyenMusteriVoen',
        hesabIban:  'OduyenMusteriHesab'
    });

    baglaMusteri('btnAlanMusteriAxtar', 'AlanMusteriVoenSearch', 'AlanMusteriXeta', {
        musteriId:  'AlanMusteriIdHidden',
        hesabId:    'AlanHesabIdHidden',
        ad:         'AlanMusteriAd',
        voen:       'AlanMusteriVoen',
        hesabIban:  'AlanMusteriHesab'
    });

    /* ── Məbləği sözə çevir ───────────────────────────────────── */
    async function meblegSoze() {
        const v = (el('Odenis_Mebleg')?.value ?? '').trim();
        const out = el('Odenis_MeblegYazi');
        if (!out) return;
        if (!v || isNaN(parseFloat(v))) { out.value = ''; return; }
        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/MeblegiSoze?mebleg=' + encodeURIComponent(v));
            if (r.ok) { const d = await r.json(); out.value = d.metn || ''; }
        } catch (_) {}
    }

    el('Odenis_Mebleg')?.addEventListener('change', meblegSoze);
    el('Odenis_Mebleg')?.addEventListener('blur',   meblegSoze);
});

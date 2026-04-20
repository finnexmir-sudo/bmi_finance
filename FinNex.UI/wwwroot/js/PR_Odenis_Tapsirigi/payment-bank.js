/* ================================================================
   payment-bank.js
   Məntiq:
     Axtarış zamanı  → bazada tap, sahələri doldur
     Submit zamanı   → bank/müştəri yoxdursa yarat,
                       hesab yoxdursa əlavə et,
                       sonra payment-word.js-in funksiyasını çağır
================================================================ */
document.addEventListener('DOMContentLoaded', function () {

    function el(id) { return document.getElementById(id); }
    function setVal(id, v) { const e = el(id); if (e) e.value = v ?? ''; }

    function xeta(elId, msg) {
        const e = el(elId);
        if (!e) return;
        e.textContent = msg;
        e.style.display = msg ? '' : 'none';
        if (msg) e.classList.add('aktiv');
        else e.classList.remove('aktiv');
    }

    /* ================================================================
       ÖDÜYƏN BANK
    ================================================================ */
    async function oduyenBankAxtar() {
        xeta('OduyenBankXeta', '');
        const kod = (el('OduyenBankKodSearch')?.value ?? '').trim();
        if (!kod) { xeta('OduyenBankXeta', 'Bank kodunu daxil edin.'); return; }

        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/BankiKodlaAxtar?kod=' + encodeURIComponent(kod));
            if (!r.ok) { xeta('OduyenBankXeta', 'Server xətası: ' + r.status); return; }
            const d = await r.json();

            if (!d.tapildi) {
                xeta('OduyenBankXeta', '"' + kod + '" kodu ilə bank tapılmadı.');
                ['OduyenBankIdHidden', 'OduyenBankAd', 'OduyenBankKod',
                    'OduyenBankVoen', 'OduyenBankMuxbirHesab', 'OduyenBankSwift'].forEach(id => setVal(id, ''));
                return;
            }
            setVal('OduyenBankIdHidden', d.id);
            setVal('OduyenBankAd', d.ad);
            setVal('OduyenBankKod', d.kod);
            setVal('OduyenBankVoen', d.voen);
            setVal('OduyenBankMuxbirHesab', d.muxHesab);
            setVal('OduyenBankSwift', d.swiftBic);
            xeta('OduyenBankXeta', '');
        } catch (e) { xeta('OduyenBankXeta', 'Xəta: ' + e.message); }
        xeta('OduyenBankXeta', '');
    }

    el('btnOduyenBankAxtar')?.addEventListener('click', oduyenBankAxtar);
    el('OduyenBankKodSearch')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); oduyenBankAxtar(); }
    });

    /* ================================================================
       ÖDÜYƏN MÜŞTƏRİ
    ================================================================ */
    async function oduyenMusteriAxtar() {
        xeta('OduyenMusteriXeta', '');
        const voen = (el('OduyenMusteriVoenSearch')?.value ?? '').trim();
        if (!voen) { xeta('OduyenMusteriXeta', 'VOEN daxil edin.'); return; }

        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/MusteriVoenleAxtar?voen=' + encodeURIComponent(voen));
            if (!r.ok) { xeta('OduyenMusteriXeta', 'Server xətası: ' + r.status); return; }
            const d = await r.json();

            if (!d.tapildi) {
                xeta('OduyenMusteriXeta', '"' + voen + '" VOEN-li müştəri tapılmadı.');
                ['OduyenMusteriIdHidden', 'OduyenMusteriAd', 'OduyenMusteriVoen',
                    'OduyenHesabIdHidden', 'OduyenMusteriHesab'].forEach(id => setVal(id, ''));
                return;
            }
            setVal('OduyenMusteriIdHidden', d.id);
            setVal('OduyenMusteriAd', d.ad);
            setVal('OduyenMusteriVoen', d.voen);
            if (d.hesablar?.length > 0) {
                setVal('OduyenHesabIdHidden', d.hesablar[0].id);
                setVal('OduyenMusteriHesab', d.hesablar[0].iban);
            }
        } catch (e) { xeta('OduyenMusteriXeta', 'Xəta: ' + e.message); }
    }

    el('btnOduyenMusteriAxtar')?.addEventListener('click', oduyenMusteriAxtar);
    el('OduyenMusteriVoenSearch')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); oduyenMusteriAxtar(); }
    });

    /* ================================================================
       ALAN BANK — radio/readonly/manual UI
    ================================================================ */
    function renderAlanBankHesab(hesablar) {
        const list = el('AlanBankHesabRadioList');
        const wrapper = el('AlanBankHesabRadioWrapper');
        const manual = el('AlanBankMuxbirHesab');

        if (!list || !wrapper) return;
        list.innerHTML = '';
        wrapper.style.display = 'none';
        if (manual) { manual.style.display = 'none'; manual.readOnly = false; manual.value = ''; }

        if (!hesablar || hesablar.length === 0) {
            if (manual) { manual.style.display = ''; }
            return;
        }
        if (hesablar.length === 1) {
            if (manual) { manual.style.display = ''; manual.value = hesablar[0].iban; manual.readOnly = true; }
            setVal('AlanHesabIdHidden', hesablar[0].id);
            return;
        }

        if (manual) manual.style.display = 'none';
        wrapper.style.display = '';
        hesablar.forEach((h, i) => {
            const opt = document.createElement('div');
            opt.className = 'hesab-radio-option' + (i === 0 ? ' active' : '');
            if (i !== 0) opt.style.display = 'none';
            opt.innerHTML = `
                <div class="hesab-radio-dot"><div class="hesab-radio-dot-inner"></div></div>
                <span class="hesab-radio-iban" title="${h.iban}">${h.iban}</span>
                <span class="hesab-radio-tag ${i === 0 ? 'esass' : 'elave'}">${i === 0 ? 'Əsas' : 'Əlavə'}</span>`;
            opt.addEventListener('click', () => {
                const all = list.querySelectorAll('.hesab-radio-option');
                if (opt.classList.contains('active') && all.length > 1) {
                    all.forEach(o => { o.style.display = ''; });
                } else {
                    all.forEach(o => { o.classList.remove('active'); o.style.display = 'none'; });
                    opt.classList.add('active'); opt.style.display = '';
                    setVal('AlanHesabIdHidden', h.id);
                    setVal('AlanBankMuxbirHesab', h.iban);
                }
            });
            list.appendChild(opt);
        });
        setVal('AlanHesabIdHidden', hesablar[0].id);
        setVal('AlanBankMuxbirHesab', hesablar[0].iban);
    }

    async function alanBankAxtar() {
        
        const kod = (el('AlanBankKodSearch')?.value ?? '').trim();
        if (!kod) { xeta('AlanBankXeta', 'Bank kodunu daxil edin.'); return; }

        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/BankiKodlaAxtar?kod=' + encodeURIComponent(kod));
            if (!r.ok) { xeta('AlanBankXeta', 'Server xətası: ' + r.status); return; }
            const d = await r.json();

            if (!d.tapildi) {
                xeta('AlanBankXeta', '"' + kod + '" tapılmadı. Məlumatları əl ilə daxil edin.');
                setVal('AlanBankIdHidden', '');
                setVal('AlanHesabIdHidden', '');
                ['AlanBankAd', 'AlanBankKod', 'AlanBankVoen', 'AlanBankSwift', 'AlanBankMuxbirHesab']
                    .forEach(id => { const e = el(id); if (e) { e.value = ''; e.style.display = ''; e.readOnly = false; } });
                setVal('AlanBankKod', kod);
                renderAlanBankHesab([]);
                return;
            }

            setVal('AlanBankIdHidden', d.id);
            setVal('AlanBankAd', d.ad);
            setVal('AlanBankKod', d.kod);
            setVal('AlanBankVoen', d.voen);
            setVal('AlanBankSwift', d.swiftBic);
            xeta('AlanBankXeta', ' ');
            ['AlanBankAd', 'AlanBankKod', 'AlanBankVoen', 'AlanBankSwift'].forEach(id => {
                const e = el(id); if (e) { e.style.display = ''; e.readOnly = true; }
            });

            let hesablar = [];
            try {
                const r2 = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/BankHesablariGetir?bankId=' + d.id);
                if (r2.ok) hesablar = await r2.json();
            } catch (_) { }
            renderAlanBankHesab(hesablar);

        } catch (e) { xeta('AlanBankXeta', 'Xəta: ' + e.message); }
        xeta('AlanBankXeta', ' ');
    }

    el('btnAlanBankAxtar')?.addEventListener('click', alanBankAxtar);
    el('AlanBankKodSearch')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); alanBankAxtar(); }
    });

    /* ================================================================
       ALAN MÜŞTƏRİ — radio/readonly/manual UI
    ================================================================ */
    function renderAlanMusteriHesab(hesablar) {
        const list = el('AlanMusteriHesabRadioList');
        const wrapper = el('AlanMusteriHesabRadioWrapper');
        const manual = el('AlanMusteriHesab');

        if (!list || !wrapper) return;
        list.innerHTML = '';
        wrapper.style.display = 'none';
        if (manual) { manual.style.display = 'none'; manual.readOnly = false; manual.value = ''; }

        if (!hesablar || hesablar.length === 0) {
            if (manual) { manual.style.display = ''; }
            return;
        }
        if (hesablar.length === 1) {
            if (manual) { manual.style.display = ''; manual.value = hesablar[0].iban; manual.readOnly = true; }
            setVal('AlanMusteriHesabId', hesablar[0].id);
            return;
        }

        if (manual) manual.style.display = 'none';
        wrapper.style.display = '';
        hesablar.forEach((h, i) => {
            const opt = document.createElement('div');
            opt.className = 'hesab-radio-option' + (i === 0 ? ' active' : '');
            if (i !== 0) opt.style.display = 'none';
            opt.innerHTML = `
                <div class="hesab-radio-dot"><div class="hesab-radio-dot-inner"></div></div>
                <span class="hesab-radio-iban" title="${h.iban}">${h.iban}</span>
                <span class="hesab-radio-tag ${i === 0 ? 'esass' : 'elave'}">${i === 0 ? 'Əsas' : 'Əlavə'}</span>`;
            opt.addEventListener('click', () => {
                const all = list.querySelectorAll('.hesab-radio-option');
                if (opt.classList.contains('active') && all.length > 1) {
                    all.forEach(o => { o.style.display = ''; });
                } else {
                    all.forEach(o => { o.classList.remove('active'); o.style.display = 'none'; });
                    opt.classList.add('active'); opt.style.display = '';
                    setVal('AlanMusteriHesabId', h.id);
                    setVal('AlanMusteriHesab', h.iban);
                }
            });
            list.appendChild(opt);
        });
        setVal('AlanMusteriHesabId', hesablar[0].id);
        setVal('AlanMusteriHesab', hesablar[0].iban);
    }

    async function alanMusteriAxtar() {
       
        const voen = (el('AlanMusteriVoenSearch')?.value ?? '').trim();
        if (!voen) { xeta('AlanMusteriXeta', 'VOEN daxil edin.'); return; }

        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/MusteriVoenleAxtar?voen=' + encodeURIComponent(voen));
            if (!r.ok) { xeta('AlanMusteriXeta', 'Server xətası: ' + r.status); return; }
            const d = await r.json();

            if (!d.tapildi) {
                xeta('AlanMusteriXeta', '"' + voen + '" tapılmadı. Məlumatları əl ilə daxil edin.');
                setVal('AlanMusteriIdHidden', '');
                ['AlanMusteriAd', 'AlanMusteriVoen', 'AlanMusteriHesab']
                    .forEach(id => { const e = el(id); if (e) { e.value = ''; e.style.display = ''; e.readOnly = false; } });
                setVal('AlanMusteriVoen', voen);
                renderAlanMusteriHesab([]);
                return;
            }

            setVal('AlanMusteriIdHidden', d.id);
            setVal('AlanMusteriAd', d.ad);
            setVal('AlanMusteriVoen', d.voen);
            xeta('AlanMusteriXeta', '');
            renderAlanMusteriHesab(d.hesablar ?? []);
            xeta('AlanMusteriXeta', ' ');

        } catch (e) { xeta('AlanMusteriXeta', 'Xəta: ' + e.message); }
        xeta('AlanMusteriXeta', ' ');
    }

    el('btnAlanMusteriAxtar')?.addEventListener('click', alanMusteriAxtar);
    el('AlanMusteriVoenSearch')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); alanMusteriAxtar(); }
    });

    /* ================================================================
       MÜŞTƏRİ YARAT düyməsi
    ================================================================ */
    el('btnMusteriYarat')?.addEventListener('click', function () {
        const ad = document.querySelector('input[name="MusteriCreateDto.Ad"]')?.value ?? '';
        const voen = document.querySelector('input[name="MusteriCreateDto.Voen"]')?.value ?? '';
        const hesab = document.querySelector('input[name="MusteriCreateDto.Hesab"]')?.value ?? '';
        window.location.href = '/PR_Odenis_Tapsirigi/Musteri/Yarat'
            + '?returnUrl=' + encodeURIComponent('/PR_Odenis_Tapsirigi/OdenisTapsirigi/Create')
            + '&ad=' + encodeURIComponent(ad)
            + '&voen=' + encodeURIComponent(voen)
            + '&hesab=' + encodeURIComponent(hesab);
    });

    /* ================================================================
       MƏBLƏĞI SÖZƏ ÇEVİR
    ================================================================ */
    async function meblegSoze() {
        const v = (el('Odenis_Mebleg')?.value ?? '').trim();
        const out = el('Odenis_MeblegYazi');
        if (!out) return;
        if (!v || isNaN(parseFloat(v))) { out.value = ''; return; }
        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/MeblegiSoze?mebleg=' + encodeURIComponent(v));
            if (r.ok) { const d = await r.json(); out.value = d.metn || ''; }
        } catch (_) { }
    }

    el('Odenis_Mebleg')?.addEventListener('change', meblegSoze);
    el('Odenis_Mebleg')?.addEventListener('blur', meblegSoze);

    /* ================================================================
       SUBMIT — "ÖT hazırlanması" düyməsi

       PROBLEM: payment-word.js də btnGenerateWord-u click ilə dinləyir.
       Hər iki listener eyni anda işə düşür — bizim await-lər
       bitməmişdən word endirilir.

       HƏLL: btnGenerateWord-u klonlayırıq → payment-word.js-in
       bütün köhnə listener-ləri silinir. Sonra yeni listener qoşuruq:
         1. bankYoxlaVeYarat()     — await ilə gözləyirik
         2. musteriYoxlaVeYarat()  — await ilə gözləyirik
         3. window.triggerWordGeneration() — payment-word.js-in
            export etdiyi funksiya birbaşa çağırılır
    ================================================================ */
    // Ümumi köməkçi: prefix ilə (Oduyen/Alan) bank və müştərini yoxlayıb,
    // tapılmazsa bazaya əlavə edir və hidden ID-ləri doldurur.
    async function bankYoxlaVeYaratFor(prefix) {
        const bankIdEl = el(prefix + 'BankIdHidden');
        const existingId = (bankIdEl?.value ?? '').trim();
        // Hidden ID artıq dolub və 0-dan böyükdürsə Axtar vasitəsilə təyin olunub — toxunma
        if (existingId && existingId !== '0') return;

        const dto = {
            ad:       (el(prefix + 'BankAd')?.value ?? '').trim(),
            kod:      (el(prefix + 'BankKod')?.value ?? '').trim(),
            voen:     (el(prefix + 'BankVoen')?.value ?? '').trim(),
            swiftBic: (el(prefix + 'BankSwift')?.value ?? '').trim(),
            muxHesab: (el(prefix + 'BankMuxbirHesab')?.value ?? '').trim()
        };
        if (!dto.ad && !dto.kod && !dto.voen && !dto.swiftBic && !dto.muxHesab) return;

        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/BankYoxlaVeYarat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });
            if (!r.ok) { console.error(prefix + ' BankYoxlaVeYarat HTTP', r.status); return; }
            const d = await r.json();
            if (d && d.id) setVal(prefix + 'BankIdHidden', d.id);
        } catch (e) { console.error(prefix + ' BankYoxlaVeYarat xətası:', e); }
    }

    async function musteriYoxlaVeYaratFor(prefix) {
        const musteriIdEl = el(prefix + 'MusteriIdHidden');
        const hesabIdEl   = el(prefix + 'HesabIdHidden');
        const existingMusteri = (musteriIdEl?.value ?? '').trim();
        const existingHesab   = (hesabIdEl?.value ?? '').trim();
        // Hər ikisi doludursa — dəyişdirməyə ehtiyac yoxdur
        if (existingMusteri && existingMusteri !== '0' &&
            existingHesab && existingHesab !== '0') return;

        const dto = {
            id: existingMusteri && existingMusteri !== '0' ? parseInt(existingMusteri) : null,
            ad:    (el(prefix + 'MusteriAd')?.value ?? '').trim(),
            voen:  (el(prefix + 'MusteriVoen')?.value ?? '').trim(),
            hesab: (el(prefix + 'MusteriHesab')?.value ?? '').trim()
        };
        if (!dto.voen && !dto.ad) return;

        try {
            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/MusteriYoxlaVeYarat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });
            if (!r.ok) { console.error(prefix + ' MusteriYoxlaVeYarat HTTP', r.status); return; }
            const d = await r.json();
            if (d && d.id)      setVal(prefix + 'MusteriIdHidden', d.id);
            if (d && d.hesabId) setVal(prefix + 'HesabIdHidden', d.hesabId);
        } catch (e) { console.error(prefix + ' MusteriYoxlaVeYarat xətası:', e); }
    }

    async function bankYoxlaVeYarat() {
        // Ödüyən və Alan — hər ikisi avtomatik yaradıla bilər
        await bankYoxlaVeYaratFor('Oduyen');
        await bankYoxlaVeYaratFor('Alan');
    }
    async function musteriYoxlaVeYarat() {
        await musteriYoxlaVeYaratFor('Oduyen');
        await musteriYoxlaVeYaratFor('Alan');
    }

    // ── btnGenerateWord-u klonla: payment-word.js listener-ini sil ──
    const origBtn = el('btnGenerateWord');
    if (origBtn) {
        // Klonlama bütün addEventListener listener-lərini silir
        const newBtn = origBtn.cloneNode(true);
        origBtn.parentNode.replaceChild(newBtn, origBtn);

        newBtn.addEventListener('click', async function () {
            newBtn.disabled = true;
            try {
                await bankYoxlaVeYarat();
                await musteriYoxlaVeYarat();

                // payment-word.js öz funksiyasını window-a export etməlidi:
                //   window.triggerWordGeneration = async function() { ... }
                // Əgər export yoxdursa — aşağıda alternativ üsul da var
                if (typeof window.triggerWordGeneration === 'function') {
                    await window.triggerWordGeneration();
                } else {
                    // Alternativ: manual click trigger yeni btn-ə listener
                    // qoşulmadığı üçün sonsuz loop olmaz
                    newBtn.dispatchEvent(new MouseEvent('click-word', { bubbles: false }));
                    console.warn('window.triggerWordGeneration tapılmadı. ' +
                        'payment-word.js faylında aşağıdakını əlavə edin:\n' +
                        'window.triggerWordGeneration = async function() { /* mövcud məntiq */ };');
                }
            } finally {
                newBtn.disabled = false;
            }
        });
    }

});
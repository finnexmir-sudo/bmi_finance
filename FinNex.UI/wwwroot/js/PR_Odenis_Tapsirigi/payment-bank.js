document.addEventListener('DOMContentLoaded', function () {

    // ─── Bank axtarisi ────────────────────────────────────────────────────────
    async function bankiAxtar(kodInputId, xetaId, fields) {
        const kod = document.getElementById(kodInputId)?.value?.trim();
        const xetaEl = document.getElementById(xetaId);

        xetaEl.style.display = 'none';
        xetaEl.textContent = '';

        if (!kod) {
            xetaEl.textContent = 'Kod daxil edin.';
            xetaEl.style.display = 'block';
            return;
        }

        try {
            const resp = await fetch(`/OdenisTapsirigi/BankiKodlaAxtar?kod=${encodeURIComponent(kod)}`);
            const data = await resp.json();

            if (!data.tapildi) {
                xetaEl.textContent = `"${kod}" kodu ile bank tapilmadi.`;
                xetaEl.style.display = 'block';
                setBankFields(fields, null);
                return;
            }

            setBankFields(fields, data);
        } catch (e) {
            xetaEl.textContent = 'Server xetasi: ' + e.message;
            xetaEl.style.display = 'block';
        }
    }

    function setBankFields(fields, data) {
        document.getElementById(fields.idHidden).value  = data ? data.id      : '';
        document.getElementById(fields.ad).value        = data ? data.ad      : '';
        document.getElementById(fields.kod).value       = data ? data.kod     : '';
        document.getElementById(fields.voen).value      = data ? data.voen    : '';
        document.getElementById(fields.muxHesab).value  = data ? data.muxHesab : '';
        document.getElementById(fields.swift).value     = data ? data.swiftBic : '';
    }

    // Ödüyən bank (A1)
    const oduyenBankFields = {
        idHidden: 'OduyenBankIdHidden',
        ad:       'OduyenBankAd',
        kod:      'OduyenBankKod',
        voen:     'OduyenBankVoen',
        muxHesab: 'OduyenBankMuxbirHesab',
        swift:    'OduyenBankSwift'
    };

    document.getElementById('btnOduyenBankAxtar')?.addEventListener('click', () =>
        bankiAxtar('OduyenBankKodSearch', 'OduyenBankXeta', oduyenBankFields)
    );
    document.getElementById('OduyenBankKodSearch')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); bankiAxtar('OduyenBankKodSearch', 'OduyenBankXeta', oduyenBankFields); }
    });

    // Alan bank (B1)
    const alanBankFields = {
        idHidden: 'AlanBankIdHidden',
        ad:       'AlanBankAd',
        kod:      'AlanBankKod',
        voen:     'AlanBankVoen',
        muxHesab: 'AlanBankMuxbirHesab',
        swift:    'AlanBankSwift'
    };

    document.getElementById('btnAlanBankAxtar')?.addEventListener('click', () =>
        bankiAxtar('AlanBankKodSearch', 'AlanBankXeta', alanBankFields)
    );
    document.getElementById('AlanBankKodSearch')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); bankiAxtar('AlanBankKodSearch', 'AlanBankXeta', alanBankFields); }
    });

    // ─── Musteri axtarisi ────────────────────────────────────────────────────
    async function musteriAxtar(voenInputId, xetaId, fields) {
        const voen = document.getElementById(voenInputId)?.value?.trim();
        const xetaEl = document.getElementById(xetaId);

        xetaEl.style.display = 'none';
        xetaEl.textContent = '';

        if (!voen) {
            xetaEl.textContent = 'VOEN daxil edin.';
            xetaEl.style.display = 'block';
            return;
        }

        try {
            const resp = await fetch(`/OdenisTapsirigi/MusteriVoenleAxtar?voen=${encodeURIComponent(voen)}`);
            const data = await resp.json();

            if (!data.tapildi) {
                xetaEl.textContent = `"${voen}" VOEN-li musteri tapilmadi.`;
                xetaEl.style.display = 'block';
                setMusteriFields(fields, null, null);
                return;
            }

            document.getElementById(fields.musteriIdHidden).value = data.id;
            document.getElementById(fields.ad).value   = data.ad;
            document.getElementById(fields.voen).value = data.voen;

            // Hesab secimi
            const hesabSelect = document.getElementById(fields.hesabSelect);
            const hesabSecimDiv = document.getElementById(fields.hesabSecimDiv);
            const hesabIban = document.getElementById(fields.hesabIban);

            hesabSelect.innerHTML = '';
            data.hesablar.forEach(h => {
                const opt = document.createElement('option');
                opt.value = h.id;
                opt.textContent = `${h.iban} (${h.valyuta})`;
                opt.dataset.iban = h.iban;
                hesabSelect.appendChild(opt);
            });

            if (data.hesablar.length === 0) {
                hesabSecimDiv.style.display = 'none';
                document.getElementById(fields.hesabIdHidden).value = '';
                hesabIban.value = '';
            } else if (data.hesablar.length === 1) {
                hesabSecimDiv.style.display = 'none';
                document.getElementById(fields.hesabIdHidden).value = data.hesablar[0].id;
                hesabIban.value = data.hesablar[0].iban;
            } else {
                hesabSecimDiv.style.display = 'block';
                // İlk hesabi sec
                document.getElementById(fields.hesabIdHidden).value = data.hesablar[0].id;
                hesabIban.value = data.hesablar[0].iban;
            }
        } catch (e) {
            xetaEl.textContent = 'Server xetasi: ' + e.message;
            xetaEl.style.display = 'block';
        }
    }

    function setMusteriFields(fields, musteri, hesab) {
        document.getElementById(fields.musteriIdHidden).value = musteri ? musteri.id : '';
        document.getElementById(fields.ad).value   = musteri ? musteri.ad   : '';
        document.getElementById(fields.voen).value = musteri ? musteri.voen : '';
        document.getElementById(fields.hesabIdHidden).value   = hesab ? hesab.id   : '';
        document.getElementById(fields.hesabIban).value       = hesab ? hesab.iban : '';
        document.getElementById(fields.hesabSecimDiv).style.display = 'none';
    }

    function baglaHesabSecim(fields) {
        const hesabSelect = document.getElementById(fields.hesabSelect);
        const hesabIdHidden = document.getElementById(fields.hesabIdHidden);
        const hesabIban = document.getElementById(fields.hesabIban);

        hesabSelect.addEventListener('change', function () {
            const opt = this.options[this.selectedIndex];
            hesabIdHidden.value = opt.value;
            hesabIban.value = opt.dataset.iban || '';
        });
    }

    // Ödüyən müştəri (A2)
    const oduyenMusteriFields = {
        musteriIdHidden: 'OduyenMusteriIdHidden',
        hesabIdHidden:   'OduyenHesabIdHidden',
        ad:              'OduyenMusteriAd',
        voen:            'OduyenMusteriVoen',
        hesabSelect:     'OduyenHesabSelect',
        hesabSecimDiv:   'OduyenHesabSecimDiv',
        hesabIban:       'OduyenMusteriHesab'
    };
    baglaHesabSecim(oduyenMusteriFields);

    document.getElementById('btnOduyenMusteriAxtar')?.addEventListener('click', () =>
        musteriAxtar('OduyenMusteriVoenSearch', 'OduyenMusteriXeta', oduyenMusteriFields)
    );
    document.getElementById('OduyenMusteriVoenSearch')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); musteriAxtar('OduyenMusteriVoenSearch', 'OduyenMusteriXeta', oduyenMusteriFields); }
    });

    // Alan müştəri (B2)
    const alanMusteriFields = {
        musteriIdHidden: 'AlanMusteriIdHidden',
        hesabIdHidden:   'AlanHesabIdHidden',
        ad:              'AlanMusteriAd',
        voen:            'AlanMusteriVoen',
        hesabSelect:     'AlanHesabSelect',
        hesabSecimDiv:   'AlanHesabSecimDiv',
        hesabIban:       'AlanMusteriHesab'
    };
    baglaHesabSecim(alanMusteriFields);

    document.getElementById('btnAlanMusteriAxtar')?.addEventListener('click', () =>
        musteriAxtar('AlanMusteriVoenSearch', 'AlanMusteriXeta', alanMusteriFields)
    );
    document.getElementById('AlanMusteriVoenSearch')?.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); musteriAxtar('AlanMusteriVoenSearch', 'AlanMusteriXeta', alanMusteriFields); }
    });

    // ─── Məbləği sözə çevir ──────────────────────────────────────────────────
    const meblegInput = document.getElementById('Odenis_Mebleg');
    const meblegYaziInput = document.getElementById('Odenis_MeblegYazi');

    async function meblegSoze() {
        const val = meblegInput?.value?.trim();
        if (!val || isNaN(parseFloat(val))) {
            if (meblegYaziInput) meblegYaziInput.value = '';
            return;
        }
        try {
            const resp = await fetch(`/OdenisTapsirigi/MeblegiSoze?mebleg=${encodeURIComponent(val)}`);
            const data = await resp.json();
            if (meblegYaziInput) meblegYaziInput.value = data.metn || '';
        } catch (_) { /* sessizce kec */ }
    }

    meblegInput?.addEventListener('change', meblegSoze);
    meblegInput?.addEventListener('blur',   meblegSoze);

});

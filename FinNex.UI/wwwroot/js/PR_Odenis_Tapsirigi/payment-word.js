document.addEventListener('DOMContentLoaded', function () {

    const btn = document.getElementById('btnGenerateWord');
    if (!btn) return;

    const val = id => (document.getElementById(id)?.value ?? '').trim();

    // ─── Validasiya ───────────────────────────────────────────────────────────
    function formYoxla() {
        const xetalar = [];

        if (!val('OduyenBankIdHidden'))
            xetalar.push('Ödüyən bank seçilməyib (A1: bank kodu ilə axtarın).');

        if (!val('AlanBankIdHidden'))
            xetalar.push('Alan bank seçilməyib (B1: bank kodu ilə axtarın).');

        if (!val('OduyenMusteriIdHidden'))
            xetalar.push('Ödüyən müştəri seçilməyib (A2: VOEN ilə axtarın).');

        if (!val('OduyenHesabIdHidden'))
            xetalar.push('Ödüyən müştərinin hesabı seçilməyib (A2).');

        if (!val('AlanMusteriIdHidden'))
            xetalar.push('Alan müştəri seçilməyib (B2: VOEN ilə axtarın).');

        if (!val('AlanHesabIdHidden'))
            xetalar.push('Alan müştərinin hesabı seçilməyib (B2).');

        const mebleg = parseFloat(val('Odenis_Mebleg'));
        if (!val('Odenis_Mebleg') || isNaN(mebleg) || mebleg <= 0)
            xetalar.push('Məbləğ düzgün daxil edilməyib (C2).');

        if (!val('Odenis_Teyinat'))
            xetalar.push('Ödənişin təyinatı daxil edilməyib (D1).');

        return xetalar;
    }

    function xetalarGoster(xetalar) {
        const panel = document.getElementById('formXetalar');
        const list  = document.getElementById('formXetalarList');
        if (!panel || !list) return;

        if (xetalar.length === 0) {
            panel.style.display = 'none';
            return;
        }

        list.innerHTML = xetalar.map(x => `<li>${x}</li>`).join('');
        panel.style.display = 'block';
        panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    // ─── Word generasiyası ───────────────────────────────────────────────────
    btn.addEventListener('click', async function () {

        const xetalar = formYoxla();
        if (xetalar.length > 0) {
            xetalarGoster(xetalar);
            return;
        }
        xetalarGoster([]);

        const dto = {
            nomre: null,
            tarix: null,

            // A1
            oduyenBankAd:          val('OduyenBankAd'),
            oduyenBankKod:         val('OduyenBankKod'),
            oduyenBankVoen:        val('OduyenBankVoen'),
            oduyenBankMuxbirHesab: val('OduyenBankMuxbirHesab'),
            oduyenBankSwift:       val('OduyenBankSwift'),

            // A2
            oduyenMusteriAd:    val('OduyenMusteriAd'),
            oduyenMusteriHesab: val('OduyenMusteriHesab'),
            oduyenMusteriVoen:  val('OduyenMusteriVoen'),

            // B1
            alanBankAd:          val('AlanBankAd'),
            alanBankKod:         val('AlanBankKod'),
            alanBankVoen:        val('AlanBankVoen'),
            alanBankMuxbirHesab: val('AlanBankMuxbirHesab'),
            alanBankSwift:       val('AlanBankSwift'),
            alanBankVbank:       '',

            // B2
            alanMusteriAd:    val('AlanMusteriAd'),
            alanMusteriHesab: val('AlanMusteriHesab'),
            alanMusteriVoen:  val('AlanMusteriVoen'),

            // Məbləğ
            valyuta:    val('Odenis_Valyuta'),
            mebleg:     val('Odenis_Mebleg'),
            meblegYazi: val('Odenis_MeblegYazi'),

            // Təyinat
            teyinat:   val('Odenis_Teyinat'),
            elaveInfo: val('Odenis_ElaveInformasiya'),

            // Büdcə
            budceTesnifatininKodu: val('Odenis_BudceTesnifatininKodu'),
            budceSeviyyesininKodu: val('Odenis_BudceSeviyyesininKodu')
        };

        btn.disabled = true;
        btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Hazırlanır...';

        try {
            const response = await fetch('/OdenisTapsirigi/GenerateWord', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });

            if (!response.ok) {
                throw new Error('Server xətası: ' + response.status);
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);

            const a = document.createElement('a');
            a.href = url;
            a.download = 'OdenisTapsirigi.docx';
            document.body.appendChild(a);
            a.click();

            window.URL.revokeObjectURL(url);
            a.remove();

        } catch (err) {
            alert('Word sənədi hazırlanarkən xəta baş verdi:\n' + err.message);
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-file-earmark-word"></i> Yadda saxla';
        }
    });
});

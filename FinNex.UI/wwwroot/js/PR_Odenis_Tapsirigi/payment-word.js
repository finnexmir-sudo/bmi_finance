document.addEventListener('DOMContentLoaded', function () {

    const btn = document.getElementById('btnGenerateWord');
    if (!btn) return;

    // Helper: read value by id
    const val = id =>
        (document.getElementById(id)?.value ?? '').trim();

    // Helper: selected text from select
    const selText = id => {
        const el = document.getElementById(id);
        if (!el || el.selectedIndex < 0) return '';
        return el.options[el.selectedIndex].text.trim();
    };

    // Helper: value by name
    const nameVal = name => {
        const el = document.querySelector(`[name="${name}"]`);
        return el ? el.value.trim() : '';
    };

    btn.addEventListener('click', async function () {

        const dto = {
            // Server terefde generasiya olunur
            nomre: null,
            tarix: null,

            // A1 – Ödəyən bank
            oduyenBankAd: selText('Odenis_OduyenHesabId'),
            oduyenBankKod: val('OduyenBankKod'),
            oduyenBankVoen: val('OduyenBankVoen'),
            oduyenBankMuxbirHesab: val('OduyenBankMuxbirHesab'),
            oduyenBankSwift: val('OduyenBankSwift'),

            // A2 – Ödəyən müştəri
            oduyenMusteriAd: selText('Odenis_OduyenMusteriId'),
            oduyenMusteriHesab: val('OduyenMusteriHesab'),
            oduyenMusteriVoen: val('OduyenMusteriVoen'),

            // B1 – Alan bank
            alanBankAd: val('AlanBankAd'),
            alanBankKod: val('AlanBankKod'),
            alanBankVoen: val('AlanBankVoen'),
            alanBankMuxbirHesab: val('AlanBankMuxbirHesab'),
            alanBankSwift: val('AlanBankSwift'),
            alanBankVbank: '',

            // B2 – Alan müştəri
            alanMusteriAd: nameVal('ManualAlanMusteriAd') || selText('Odenis_AlanMusteriId'),
            alanMusteriHesab: nameVal('ManualAlanHesab'),
            alanMusteriVoen: nameVal('ManualAlanVoen'),

            // Məbləğ
            valyuta: val('Odenis_Valyuta') || selText('Odenis_Valyuta'),
            mebleg: val('Odenis_Mebleg'),
            meblegYazi: val('Odenis_MeblegYazi'),

            // Təyinat
            teyinat: val('Odenis_Teyinat'),
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

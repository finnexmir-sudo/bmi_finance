document.addEventListener('DOMContentLoaded', function () {

    const btn = document.getElementById('btnGenerateWord');
    if (!btn) return;

    btn.addEventListener('click', async function () {

        // Helper: read value by id (returns empty string if not found)
        const val = id => (document.getElementById(id)?.value ?? '').trim();

        // Helper: get selected option text from a <select>
        const selText = id => {
            const el = document.getElementById(id);
            if (!el || el.selectedIndex < 0) return '';
            return el.options[el.selectedIndex].text.trim();
        };

        // Helper: get value from input by name attribute
        const nameVal = name => {
            const el = document.querySelector(`[name="${name}"]`);
            return el ? el.value.trim() : '';
        };

        // Collect all form data
        const dto = {
            // Tapshiriq nomresi ve tarix
            nomre: '',  // auto-generated on server
            tarix: '',  // auto-generated on server

            // Oduyun bank (A1)
            oduyenBankAd:          selText('Odenis_OduyenHesabId'),
            oduyenBankKod:         val('OduyenBankKod'),
            oduyenBankVoen:        val('OduyenBankVoen'),
            oduyenBankMuxbirHesab: val('OduyenBankMuxbirHesab'),
            oduyenBankSwift:       val('OduyenBankSwift'),

            // Oduyun mushteri (A2)
            oduyenMusteriAd:    selText('Odenis_OduyenMusteriId'),
            oduyenMusteriHesab: val('OduyenMusteriHesab'),
            oduyenMusteriVoen:  val('OduyenMusteriVoen'),

            // Alan bank (B1)
            alanBankAd:          val('AlanBankAd'),
            alanBankKod:         val('AlanBankKod'),
            alanBankVoen:        val('AlanBankVoen'),
            alanBankMuxbirHesab: val('AlanBankMuxbirHesab'),
            alanBankSwift:       val('AlanBankSwift'),
            alanBankVbank:       '',

            // Alan mushteri (B2)
            alanMusteriAd:    nameVal('ManualAlanMusteriAd') || selText('Odenis_AlanMusteriId'),
            alanMusteriHesab: nameVal('ManualAlanHesab'),
            alanMusteriVoen:  nameVal('ManualAlanVoen'),

            // Mebleg
            valyuta:    val('Odenis_Valyuta') || selText('Odenis_Valyuta'),
            mebleg:     val('Odenis_Mebleg'),
            meblegYazi: val('Odenis_MeblegYazi'),

            // Teyinat
            teyinat:   val('Odenis_Teyinat'),
            elaveInfo: val('Odenis_ElaveInformasiya'),

            // Budce
            budceTesnifatininKodu:  val('Odenis_BudceTesnifatininKodu'),
            budceSeviyyesininKodu:  val('Odenis_BudceSeviyyesininKodu')
        };

        btn.disabled = true;
        btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Hazirlanir...';

        try {
            const response = await fetch('/OdenisTapsirigi/GenerateWord', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });

            if (!response.ok) {
                throw new Error('Xeta bash verdi: ' + response.status);
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
            alert('Word senedi hazirlanarken xeta bash verdi:\n' + err.message);
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-file-earmark-word"></i> Yadda saxla';
        }
    });
});

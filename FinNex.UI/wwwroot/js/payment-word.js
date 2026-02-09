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
            // Ödəyən bank (A1)
            oduyenBankAd:          selText('Odenis_OduyenHesabId'),
            oduyenBankKod:         val('OduyenBankKod'),
            oduyenBankVoen:        val('OduyenBankVoen'),
            oduyenBankMuxbirHesab: val('OduyenBankMuxbirHesab'),
            oduyenBankSwift:       val('OduyenBankSwift'),

            // Ödəyən müştəri (A2)
            oduyenMusteriAd:    selText('Odenis_OduyenMusteriId'),
            oduyenMusteriHesab: val('OduyenMusteriHesab'),
            oduyenMusteriVoen:  val('OduyenMusteriVoen'),

            // Alan bank (B1)
            alanBankAd:          '',
            alanBankKod:         val('AlanBankKod'),
            alanBankVoen:        val('AlanBankVoen'),
            alanBankMuxbirHesab: val('AlanBankMuxbirHesab'),
            alanBankSwift:       val('AlanBankSwift'),

            // Alan müştəri (B2) – bazadan və ya əl ilə
            alanMusteriAd:    nameVal('ManualAlanMusteriAd') || selText('Odenis_AlanMusteriId'),
            alanMusteriHesab: nameVal('ManualAlanHesab'),
            alanMusteriVoen:  nameVal('ManualAlanVoen'),

            // Məbləğ
            valyuta:   val('Odenis_Valyuta') || selText('Odenis_Valyuta'),
            mebleg:    val('Odenis_Mebleg'),
            meblegYazi: val('MeblegYazi'),

            // Təyinat
            teyinat:   val('Odenis_Teyinat'),
            elaveInfo: val('ElaveInfo')
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
                throw new Error('Xəta baş verdi: ' + response.status);
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

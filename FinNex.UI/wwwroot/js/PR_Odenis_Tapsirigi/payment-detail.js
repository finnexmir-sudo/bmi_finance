document.addEventListener('DOMContentLoaded', function () {

    /* ── EDIT ── */
    const btnBashla = document.getElementById('btnDuzelisBashla');
    const btnSaxla = document.getElementById('btnSaxla');
    const btnLegv = document.getElementById('btnLegv');

    function editRejimi(aktiv) {
        document.querySelectorAll('.editable-field').forEach(el => {
            el.classList.toggle('d-none', aktiv);
        });
        document.querySelectorAll('.edit-input').forEach(el => {
            el.classList.toggle('d-none', !aktiv);
        });
        btnBashla?.classList.toggle('d-none', aktiv);
        btnSaxla?.classList.toggle('d-none', !aktiv);
        btnLegv?.classList.toggle('d-none', !aktiv);
    }

    btnBashla?.addEventListener('click', () => editRejimi(true));
    btnLegv?.addEventListener('click', () => editRejimi(false));

    btnSaxla?.addEventListener('click', async function () {
        btnSaxla.disabled = true;
        try {
            const dto = {
                id: ODENIS_ID,
                mebleg: parseFloat(document.getElementById('edit_mebleg')?.value) || 0,
                meblegYazi: document.getElementById('edit_meblegYazi')?.value ?? '',
                teyinat: document.getElementById('edit_teyinat')?.value ?? '',
                elaveInformasiya: document.getElementById('edit_elaveInformasiya')?.value ?? '',
                budceTesnifatininKodu: document.getElementById('edit_budceTesnifatininKodu')?.value ?? '',
                budceSeviyyesininKodu: document.getElementById('edit_budceSeviyyesininKodu')?.value ?? ''
            };

            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/EditVeWord', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(dto)
            });

            if (!r.ok) {
                const err = await r.text();
                toast(err || 'Xəta baş verdi', 'error');
                return;
            }

            // Word faylı endir
            const blob = await r.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `OdenisTapsirigi_${ODENIS_ID}.docx`;
            a.click();
            URL.revokeObjectURL(url);

            // View sahələrini yenilə
            document.getElementById('view_mebleg').textContent =
                parseFloat(dto.mebleg).toLocaleString('az-AZ', { minimumFractionDigits: 2 });
            document.getElementById('view_meblegYazi').textContent = dto.meblegYazi;
            document.getElementById('view_teyinat').textContent = dto.teyinat;
            document.getElementById('view_elaveInformasiya').textContent = dto.elaveInformasiya;
            document.getElementById('view_budceTesnifatininKodu').textContent = dto.budceTesnifatininKodu;
            document.getElementById('view_budceSeviyyesininKodu').textContent = dto.budceSeviyyesininKodu;

            editRejimi(false);
            toast('Dəyişikliklər saxlanıldı və Word hazırlandı', 'success');

        } catch (e) {
            toast('Xəta: ' + e.message, 'error');
        } finally {
            btnSaxla.disabled = false;
        }
    });

    /* ── YENİDƏN YAZ ── */
    //const modal = new bootstrap.Modal(document.getElementById('yenidenYazModal'));
    //document.getElementById('btnYenidenYaz')
    //    ?.addEventListener('click', () => modal.show());

    //document.getElementById('btnYenidenYazTesdiq')
    //    ?.addEventListener('click', async function () {
    //        this.disabled = true;
    //        try {
    //            const r = await fetch('/PR_Odenis_Tapsirigi/OdenisTapsirigi/YenidenYaz', {
    //                method: 'POST',
    //                headers: { 'Content-Type': 'application/json' },
    //                body: JSON.stringify(ODENIS_ID)
    //            });
    //            const d = await r.json();
    //            if (d.ugurlu) {
    //                modal.hide();
    //                // Yeni sənədin prefill Create formasına get
    //                window.location.href =
    //                    '/PR_Odenis_Tapsirigi/OdenisTapsirigi/YenidenYazForm/' + d.yeniId;
    //            } else {
    //                toast(d.xeta || 'Xəta baş verdi', 'error');
    //            }
    //        } catch (e) {
    //            toast('Xəta: ' + e.message, 'error');
    //        } finally {
    //            this.disabled = false;
    //        }
    //    });

    /* ── TOAST ── */
    function toast(msg, tip) {
        const el = document.getElementById('detailToast');
        if (!el) return;
        el.textContent = msg;
        el.className = 'detail-toast ' + (tip === 'success' ? 'toast-success' : 'toast-error');
        setTimeout(() => el.classList.add('d-none'), 3000);
    }
});
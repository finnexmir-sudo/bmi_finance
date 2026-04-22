// ── Mezuniyyet Balans JS — illərə görə redaktə ─────────────

(function () {
    'use strict';

    // ── Year selector ────────────────────────────────────
    const yearSelect = document.getElementById('mbYearSelect');
    if (yearSelect) {
        yearSelect.addEventListener('change', function () {
            window.location.href = '/HR/MezuniyyetBalans?il=' + this.value;
        });
    }

    // ── İşçi axtarışı (ad, FİN, departament, vəzifə) ──────
    const searchInput = document.getElementById('mbSearch');
    const visibleCountEl = document.getElementById('mbVisibleCount');
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const t = this.value.trim().toLowerCase();
            const rows = document.querySelectorAll('#mbTable tbody tr');
            let gorunen = 0;
            rows.forEach(function (r) {
                const match = !t || r.textContent.toLowerCase().indexOf(t) >= 0;
                r.style.display = match ? '' : 'none';
                if (match) gorunen++;
            });
            if (visibleCountEl) visibleCountEl.textContent = gorunen;
        });
    }

    // ── Toast ────────────────────────────────────────────
    function showToast(message, type) {
        let toast = document.getElementById('mbToast');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'mbToast';
            toast.className = 'mb-toast';
            document.body.appendChild(toast);
        }
        toast.textContent = message;
        toast.className = 'mb-toast mb-toast--' + type;
        void toast.offsetWidth;
        toast.classList.add('mb-toast--visible');
        setTimeout(function () { toast.classList.remove('mb-toast--visible'); }, 3000);
    }

    // ── Modal refs ───────────────────────────────────────
    const overlay = document.getElementById('mbModalOverlay');
    const illikTbody = document.getElementById('mbIllikTbody');
    const digerTbody = document.getElementById('mbDigerTbody');
    const loadingInd = document.getElementById('mbLoadingIndicator');
    const cariIlMetn = document.getElementById('mbCariIlMetn');
    const yeniIlInput = document.getElementById('mbYeniIl');
    const yeniToplamInput = document.getElementById('mbYeniToplam');
    const yeniIlBtn = document.getElementById('mbYeniIlElaveEt');

    let currentIsciId = null;
    let currentIl = null;

    const novAdlari = {
        1: 'İllik məzuniyyət',
        2: 'Xəstəlik məzuniyyəti',
        3: 'Ezamiyyət'
    };

    // ── Modal aç / doldurma ──────────────────────────────
    async function openModal(isciId, isciAd, il) {
        if (!overlay) return;

        currentIsciId = parseInt(isciId);
        currentIl = parseInt(il);
        document.getElementById('mbModalIsciAd').textContent = isciAd;
        cariIlMetn.textContent = currentIl;

        overlay.classList.add('mb-modal-overlay--active');
        await loadBalanslari();
    }

    function closeModal() { if (overlay) overlay.classList.remove('mb-modal-overlay--active'); }

    async function loadBalanslari() {
        illikTbody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:#94a3b8;padding:20px">— yüklənir —</td></tr>';
        digerTbody.innerHTML = '';
        if (loadingInd) loadingInd.style.display = 'inline';

        try {
            const resp = await fetch('/HR/MezuniyyetBalans/IsciBalanslari?isciId=' + currentIsciId);
            const data = await resp.json();
            if (!data.success) {
                showToast(data.message || 'Məlumat yüklənmədi.', 'error');
                return;
            }

            const illik = data.balanslar.filter(b => b.nov === 1).sort((a, b) => b.il - a.il);
            renderIllik(illik);

            const diger = data.balanslar.filter(b => b.nov !== 1 && b.il === currentIl);
            renderDiger(diger);
        } catch (err) {
            showToast('Server xətası baş verdi.', 'error');
        } finally {
            if (loadingInd) loadingInd.style.display = 'none';
        }
    }

    // ── İllik cədvəli renderi ────────────────────────────
    function renderIllik(balanslar) {
        if (!balanslar.length) {
            illikTbody.innerHTML = '<tr><td colspan="5" style="text-align:center;color:#94a3b8;padding:20px">Heç bir il üçün İllik balansı yoxdur</td></tr>';
            return;
        }

        illikTbody.innerHTML = '';
        balanslar.forEach(function (b) {
            const tr = document.createElement('tr');
            const isActiveYear = b.il === currentIl;
            tr.innerHTML =
                '<td style="font-weight:700' + (isActiveYear ? ';color:#1e40af' : '') + '">' +
                    b.il + (isActiveYear ? ' <span style="font-size:10px;background:#dbeafe;color:#1e40af;padding:1px 6px;border-radius:8px;margin-left:4px">cari</span>' : '') +
                '</td>' +
                '<td><input type="number" class="mb-edit-input mb-illik-toplam" min="' + b.istifade + '" value="' + b.toplamGun + '" data-balans-id="' + b.id + '" data-istifade="' + b.istifade + '" style="width:80px"></td>' +
                '<td style="text-align:center">' + b.istifade + '</td>' +
                '<td class="mb-edit-qaliq" style="text-align:center;font-weight:700">' + b.qaliq + '</td>' +
                '<td><button type="button" class="mb-btn-save mb-illik-save" data-balans-id="' + b.id + '" style="padding:4px 10px;font-size:11px">Saxla</button></td>';
            illikTbody.appendChild(tr);

            const input = tr.querySelector('.mb-illik-toplam');
            const qaliqTd = tr.querySelector('.mb-edit-qaliq');
            input.addEventListener('input', function () {
                const newT = parseInt(this.value) || 0;
                qaliqTd.textContent = newT - b.istifade;
            });

            tr.querySelector('.mb-illik-save').addEventListener('click', async function () {
                const btn = this;
                const newT = parseInt(input.value) || 0;
                if (newT < b.istifade) {
                    showToast('Toplam istifadədən az ola bilməz (' + b.istifade + ' gün istifadə olunub).', 'error');
                    return;
                }
                btn.disabled = true; btn.textContent = '...';
                try {
                    const resp = await fetch('/HR/MezuniyyetBalans/Update', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                        body: 'id=' + b.id + '&toplamGun=' + newT
                    });
                    const r = await resp.json();
                    if (r.success) {
                        showToast(b.il + '-ci il balansı yeniləndi.', 'success');
                        b.toplamGun = newT;
                        b.qaliq = r.qaliqGun;
                    } else {
                        showToast(r.message, 'error');
                    }
                } catch (e) {
                    showToast('Server xətası.', 'error');
                }
                btn.disabled = false; btn.textContent = 'Saxla';
            });
        });
    }

    // ── Xəstəlik / Ezamiyyət cədvəli (cari il) ───────────
    function renderDiger(balanslar) {
        digerTbody.innerHTML = '';
        [2, 3].forEach(function (nov) {
            const b = balanslar.find(x => x.nov === nov);
            const tr = document.createElement('tr');

            if (!b) {
                // Yeni yarat — CreateOrUpdate istifadə edə bilmək üçün mövcud illik-i saxlayırıq
                tr.innerHTML =
                    '<td><strong>' + novAdlari[nov] + '</strong></td>' +
                    '<td><input type="number" class="mb-edit-input mb-diger-toplam" min="0" value="0" data-nov="' + nov + '" style="width:80px"></td>' +
                    '<td style="text-align:center">0</td>' +
                    '<td class="mb-edit-qaliq" style="text-align:center">0</td>' +
                    '<td><button type="button" class="mb-btn-save mb-diger-save" data-nov="' + nov + '" style="padding:4px 10px;font-size:11px">Yarat</button></td>';
            } else {
                tr.innerHTML =
                    '<td><strong>' + novAdlari[nov] + '</strong></td>' +
                    '<td><input type="number" class="mb-edit-input mb-diger-toplam" min="' + b.istifade + '" value="' + b.toplamGun + '" data-balans-id="' + b.id + '" data-nov="' + nov + '" data-istifade="' + b.istifade + '" style="width:80px"></td>' +
                    '<td style="text-align:center">' + b.istifade + '</td>' +
                    '<td class="mb-edit-qaliq" style="text-align:center;font-weight:700">' + b.qaliq + '</td>' +
                    '<td><button type="button" class="mb-btn-save mb-diger-save" data-balans-id="' + b.id + '" data-nov="' + nov + '" style="padding:4px 10px;font-size:11px">Saxla</button></td>';
            }
            digerTbody.appendChild(tr);

            const input = tr.querySelector('.mb-diger-toplam');
            const qaliqTd = tr.querySelector('.mb-edit-qaliq');
            const istifadeVal = b ? b.istifade : 0;
            input.addEventListener('input', function () {
                const newT = parseInt(this.value) || 0;
                qaliqTd.textContent = newT - istifadeVal;
            });

            tr.querySelector('.mb-diger-save').addEventListener('click', async function () {
                const btn = this;
                const newT = parseInt(input.value) || 0;
                if (b && newT < b.istifade) {
                    showToast('Toplam istifadədən az ola bilməz.', 'error');
                    return;
                }
                btn.disabled = true; btn.textContent = '...';
                try {
                    // b varsa — Update, yoxdursa — CreateOrUpdate ilə yarat
                    if (b) {
                        const resp = await fetch('/HR/MezuniyyetBalans/Update', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                            body: 'id=' + b.id + '&toplamGun=' + newT
                        });
                        const r = await resp.json();
                        if (r.success) {
                            showToast(novAdlari[nov] + ' yeniləndi.', 'success');
                            b.toplamGun = newT;
                            b.qaliq = r.qaliqGun;
                        } else { showToast(r.message, 'error'); }
                    } else {
                        // CreateOrUpdate yalnız tam set göndərir — current il üçün İllik digər növü saxlaya bilmirik.
                        // Sadə yol: backend-də yeni CreateSingle əvvəlcədən yazıldı.
                        // Amma hələ CreateOrUpdate istifadə edirik — yalnız həmin novu 0 olmayan kimi göndəririk.
                        // Digərləri 0 gəlsə də, onlar artıq yaradılıb. -1 göndərsək skip olur — 0-dan az olsa. Əlavə olaraq növü yaratmaq üçün sadə yol: AddIllikBalans nov 1 üçündür. Hazırda nov 2/3 yaratmaq üçün manual POST:
                        const resp = await fetch('/HR/MezuniyyetBalans/CreateOrUpdate', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                            body: 'isciId=' + currentIsciId + '&il=' + currentIl +
                                  '&illikGun=-1' +
                                  '&xestelikGun=' + (nov === 2 ? newT : -1) +
                                  '&ezamiyyetGun=' + (nov === 3 ? newT : -1)
                        });
                        const r = await resp.json();
                        if (r.success) {
                            showToast(novAdlari[nov] + ' yaradıldı.', 'success');
                            await loadBalanslari();
                        } else { showToast(r.message, 'error'); }
                    }
                } catch (e) {
                    showToast('Server xətası.', 'error');
                }
                btn.disabled = false;
                btn.textContent = b ? 'Saxla' : 'Yarat';
            });
        });
    }

    // ── Yeni il əlavə et ─────────────────────────────────
    if (yeniIlBtn) {
        yeniIlBtn.addEventListener('click', async function () {
            const il = parseInt(yeniIlInput.value) || 0;
            const gun = parseInt(yeniToplamInput.value) || 0;
            if (il < 2000 || il > 2030) {
                showToast('İl düzgün daxil edilməlidir (2000–2030).', 'error'); return;
            }
            if (gun <= 0) {
                showToast('Gün 0-dan böyük olmalıdır.', 'error'); return;
            }
            yeniIlBtn.disabled = true; yeniIlBtn.textContent = '...';
            try {
                const resp = await fetch('/HR/MezuniyyetBalans/AddIllikBalans', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: 'isciId=' + currentIsciId + '&il=' + il + '&toplamGun=' + gun
                });
                const r = await resp.json();
                if (r.success) {
                    showToast(r.message, 'success');
                    yeniIlInput.value = '';
                    yeniToplamInput.value = '';
                    await loadBalanslari();
                } else { showToast(r.message, 'error'); }
            } catch (e) { showToast('Server xətası.', 'error'); }
            yeniIlBtn.disabled = false; yeniIlBtn.textContent = 'Əlavə et';
        });
    }

    // ── Edit button click delegation ─────────────────────
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.mb-btn-edit');
        if (!btn) return;
        openModal(btn.dataset.isciId, btn.dataset.isciAd, btn.dataset.il || (yearSelect && yearSelect.value) || new Date().getFullYear());
    });

    // ── Close ────────────────────────────────────────────
    if (overlay) {
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) closeModalAndReload();
        });
    }
    const closeBtn = document.getElementById('mbModalClose');
    if (closeBtn) closeBtn.addEventListener('click', closeModalAndReload);
    const cancelBtn = document.getElementById('mbModalCancel');
    if (cancelBtn) cancelBtn.addEventListener('click', closeModalAndReload);

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && overlay && overlay.classList.contains('mb-modal-overlay--active')) {
            closeModalAndReload();
        }
    });

    // Modal bağlananda — əgər redaktə olunub reload et ki cədvəl yenilənsin
    let modalDirty = false;
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('mb-illik-save') ||
            e.target.classList.contains('mb-diger-save') ||
            e.target.id === 'mbYeniIlElaveEt') {
            modalDirty = true;
        }
    });

    function closeModalAndReload() {
        closeModal();
        if (modalDirty) {
            setTimeout(function () { location.reload(); }, 300);
        }
    }

})();

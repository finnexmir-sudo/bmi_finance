document.addEventListener("DOMContentLoaded", function () {

    const sobeSelect = document.getElementById('sobeSelect');
    const dropZone = document.getElementById('dropZone');
    const fileInput = document.getElementById('fileInput');
    const novModalEl = document.getElementById('novModal');
    const saveNovBtn = document.getElementById('saveBtn');
    const openNovBtn = document.getElementById('openNovBtn');

    const SobeId = document.getElementById('SobeId');

    // =============================
    // Fayl Drag & Drop
    // =============================
    if (dropZone && fileInput) {

        dropZone.addEventListener('click', () => fileInput.click());

        ['dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, e => {
                e.preventDefault();
                e.stopPropagation();
            });
        });

        dropZone.addEventListener('drop', (e) => {
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                fileInput.files = files;
                fileInput.dispatchEvent(new Event('change'));
            }
        });

        fileInput.addEventListener('change', function () {
            if (!this.files.length) return;

            const file = this.files[0];
            const maxSize = 25 * 1024 * 1024;

            if (file.size > maxSize) {
                alert('Fayl ölçüsü 25MB-dan böyük ola bilməz.');
                this.value = '';
            }
        });
    }

    // =============================
    // Şöbə dəyişəndə növləri yüklə
    // =============================
    if (sobeSelect) {

        sobeSelect.addEventListener('change', function () {

            const sobeId = this.value;
            const senedNovuSelect = document.getElementById('senedNovuSelect');

            if (!senedNovuSelect) return;

            if (!sobeId) {
                senedNovuSelect.innerHTML = '<option value="">Əvvəlcə şöbə seçin...</option>';
                return;
            }

            senedNovuSelect.innerHTML = '<option value="">Yüklənir...</option>';

            fetch('/SenedDovriyyesi/Sened/SenedNovleriByShobe?sobeId=' + sobeId, {
                method: 'GET',
                credentials: 'include'
            })





                .then(response => {
                    if (!response.ok) throw new Error(response.status);
                    return response.json();
                })
                .then(data => {

                    senedNovuSelect.innerHTML = '<option value="">Sənəd növü seçin...</option>';

                    if (!data || data.length === 0) {
                        senedNovuSelect.innerHTML = '<option value="">Bu şöbədə növ yoxdur</option>';
                        return;
                    }

                    data.forEach(x => {
                        const opt = document.createElement("option");
                        opt.value = x.id;
                        opt.textContent = x.ad;
                        senedNovuSelect.appendChild(opt);
                    });
                })
                .catch(err => {
                    console.error("Fetch error:", err);
                    senedNovuSelect.innerHTML = '<option value="">Xəta baş verdi</option>';
                });

        });
    }

    // =============================
    // Modal Açılması (Şöbə yoxdursa açılmasın)
    // =============================
    if (openNovBtn && novModalEl) {

        const novModal = new bootstrap.Modal(novModalEl);

        openNovBtn.addEventListener('click', function () {

            const sobeId = sobeSelect.value;

            if (!sobeId) {
                alert("Əvvəlcə şöbə seçilməlidir.");
                return;
            }

            novModal.show();
        });
    }

    // =============================
    // Yeni növ yarat
    // =============================
    if (saveNovBtn && novModalEl) {

        saveNovBtn.addEventListener('click', function () {

            const kod = document.getElementById('novKod').value.trim();
            const ad = document.getElementById('novAd').value.trim();
            const errorDiv = document.getElementById('novError');
            const sobeId = sobeSelect.value;

            errorDiv.classList.add('d-none');

            if (!sobeId) {
                errorDiv.textContent = "Əvvəlcə şöbə seçilməlidir.";
                errorDiv.classList.remove('d-none');
                return;
            }

            if (!kod || !ad) {
                errorDiv.textContent = "Kod və Ad boş ola bilməz.";
                errorDiv.classList.remove('d-none');
                return;
            }

            fetch('/SenedDovriyyesi/Sened/YeniSenedNovu', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    departmentId: parseInt(sobeId),
                    kod: kod.toUpperCase(),
                    ad: ad
                })
            })
                .then(res => res.json())
                .then(data => {

                    if (!data.success) {
                        errorDiv.textContent = data.message;
                        errorDiv.classList.remove('d-none');
                        return;
                    }

                    const senedNovuSelect = document.getElementById('senedNovuSelect');
                    const option = new Option(ad, data.id, true, true);
                    senedNovuSelect.add(option);

                    bootstrap.Modal.getInstance(novModalEl).hide();
                })
                .catch(() => {
                    errorDiv.textContent = "Server xətası baş verdi.";
                    errorDiv.classList.remove('d-none');
                });

        });
    }

});

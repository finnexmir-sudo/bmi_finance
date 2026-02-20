document.addEventListener("DOMContentLoaded", function () {

    // 1. Əsas elementləri əvvəlcədən tapırıq
    const fileInput = document.getElementById('fileInput');
    const dropZone = document.getElementById('dropZone');
    const sobeSelect = document.getElementById('sobeSelect');
    const senedNovuSelect = document.getElementById('senedNovuSelect');
    const novModal = document.getElementById('novModal');
    const saveNovBtn = document.getElementById('saveBtn');


    // =====================================================
    // Fayl Yükləmə (Klik və Drag & Drop)
    // =====================================================

    if (dropZone && fileInput) {
        // Drop zone-a klik edəndə gizli file input-u tetiklə
        dropZone.addEventListener('click', function () {
            fileInput.click();
        });

        // Sürüşdürmə hadisələrini idarə et
        ['dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, e => {
                e.preventDefault();
                e.stopPropagation();
            });
        });

        // Faylı drop zone-a buraxanda
        dropZone.addEventListener('drop', (e) => {
            const dt = e.dataTransfer;
            const files = dt.files;

            if (files.length > 0) {
                fileInput.files = files; // Faylları input-a ötür
                // Change hadisəsini əllə tetiklə ki, ölçü kontrolu işləsin
                fileInput.dispatchEvent(new Event('change'));
            }
        });
    }

    // Fayl ölçü kontrolu
    if (fileInput) {
        fileInput.addEventListener('change', function () {
            if (this.files.length === 0) return;

            const file = this.files[0];
            const maxSize = 25 * 1024 * 1024; // 25MB

            if (file.size > maxSize) {
                alert('Fayl ölçüsü 25MB-dan böyük ola bilməz.');
                this.value = '';
            }
        });
    }

    // =====================================================
    // Şöbə dəyişəndə sənəd növlərini yüklə
    // =====================================================

    if (sobeSelect) {
        sobeSelect.addEventListener('change', function () {
            const sobeId = this.value;

            if (!sobeId) {
                senedNovuSelect.innerHTML = '<option value="">Əvvəlcə şöbə seçin...</option>';
                return;
            }

            senedNovuSelect.innerHTML = '<option value="">Yüklənir...</option>';

            fetch(`/SenedDovriyyesi/Sened/SenedNovleriByShobe?sobeId=${sobeId}`)
                .then(r => r.json())
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
                .catch(() => {
                    senedNovuSelect.innerHTML = '<option value="">Xəta baş verdi</option>';
                });
        });
    }

    // =====================================================
    // Modal açılarkən inputları təmizlə
    // =====================================================

    if (novModal) {
        novModal.addEventListener('show.bs.modal', function () {
            document.getElementById('novKod').value = '';
            document.getElementById('novAd').value = '';

            const errorDiv = document.getElementById('novError');
            if (errorDiv) errorDiv.classList.add('d-none');
        });
    }

    // =====================================================
    // Yeni sənəd növünü yarat (Modal Save)
    // =====================================================

    if (saveNovBtn) {

        saveNovBtn.addEventListener('click', function () {

            const kodElement = document.getElementById('novKod');
            const adElement = document.getElementById('novAd');
            const errorDiv = document.getElementById('novError');

            const sobeId = sobeSelect.value;
            const kod = kodElement.value.trim();
            const ad = adElement.value.trim();

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
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    departmentId: parseInt(sobeId),   // BURANI DÜZƏLTDİM
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

                    const option = new Option(ad, data.id, true, true);
                    senedNovuSelect.add(option);

                    const modalInstance = bootstrap.Modal.getInstance(novModal);
                    modalInstance.hide();

                })
                .catch(() => {
                    errorDiv.textContent = "Server xətası baş verdi.";
                    errorDiv.classList.remove('d-none');
                });

        });
    }
});
document.addEventListener("DOMContentLoaded", function () {

    const sobeSelect = document.getElementById('sobeSelect');
    const openNovBtn = document.getElementById('openNovBtn');
    const novModal = new bootstrap.Modal(document.getElementById('novModal'));

    if (openNovBtn) {

        openNovBtn.addEventListener('click', function () {

            const sobeId = sobeSelect.value;

            if (!sobeId) {
                alert("Əvvəlcə şöbə seçilməlidir.");
                return;
            }

            novModal.show();
        });

    }

});

    

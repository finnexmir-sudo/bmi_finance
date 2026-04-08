document.addEventListener("DOMContentLoaded", function () {

    // 1. Əsas elementləri əvvəlcədən tapırıq
    const fileInput = document.getElementById('fileInput');
    const dropZone = document.getElementById('dropZone');
    const sobeSelect = document.getElementById('sobeSelect');
    const senedNovuSelect = document.getElementById('senedNovuSelect');
    const AxtarSilinmisler = document.getElementById('AxtarSilinmisler');
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

    // =====================================================
    // Şöbə dəyişəndə sənəd növlərini yükləyən funksiya
    // =====================================================
    function loadSenedNovleri(sobeId, selectedNovId = null) {
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
                    // Əgər axtarışdan sonra səhifə yenilənibsə, əvvəlki seçimi bərpa et
                    if (selectedNovId && x.id == selectedNovId) {
                        opt.selected = true;
                    }
                    senedNovuSelect.appendChild(opt);
                });
            })
            .catch(() => {
                senedNovuSelect.innerHTML = '<option value="">Xəta baş verdi</option>';
            });
    }

    // Event Listener hissəsi
    if (sobeSelect) {
        // 1. İstifadəçi şöbəni əllə dəyişəndə
        sobeSelect.addEventListener('change', function () {
            loadSenedNovleri(this.value);
        });

        // 2. Axtarışdan sonra səhifə yüklənəndə (əgər şöbə artıq seçilibsə)
        if (sobeSelect.value) {
            // Model-dən gələn mövcud SenedNovuId-ni JS-ə ötürürük
            const currentSenedNovuId = "@Model.SenedNovuId";
            loadSenedNovleri(sobeSelect.value, currentSenedNovuId);
        }
    }


    // =====================================================
    // Modal açılarkən inputları təmizlə
    // =====================================================

    if (novModal) {
        novModal.addEventListener('show.bs.modal', function () {
            document.getElementById('novKod').value = '';
            document.getElementById('novAd').value = '';

            // Şöbəni ana formadan avtomatik seç
            var novSobe = document.getElementById('novSobe');
            if (novSobe && sobeSelect && sobeSelect.value) {
                novSobe.value = sobeSelect.value;
            }

            var errorDiv = document.getElementById('novError');
            if (errorDiv) { errorDiv.hidden = true; errorDiv.classList.add('d-none'); }
            var successDiv = document.getElementById('novSuccess');
            if (successDiv) successDiv.hidden = true;
        });
    }

    // =====================================================
    // Yeni sənəd növünü yarat (Modal Save)
    // =====================================================

    //if (saveNovBtn) {

    //    saveNovBtn.addEventListener('click', function () {

    //        const kodElement = document.getElementById('novKod');
    //        const adElement = document.getElementById('novAd');
    //        const errorDiv = document.getElementById('novError');

    //        const sobeId = sobeSelect.value;
    //        const kod = kodElement.value.trim();
    //        const ad = adElement.value.trim();

    //        errorDiv.classList.add('d-none');

    //        if (!sobeId) {
    //            errorDiv.textContent = "Əvvəlcə şöbə seçilməlidir.";
    //            errorDiv.classList.remove('d-none');
    //            return;
    //        }

    //        if (!kod || !ad) {
    //            errorDiv.textContent = "Kod və Ad boş ola bilməz.";
    //            errorDiv.classList.remove('d-none');
    //            return;
    //        }

    //        fetch('/SenedDovriyyesi/Sened/YeniSenedNovu', {
    //            method: 'POST',
    //            headers: {
    //                'Content-Type': 'application/json'
    //            },
    //            body: JSON.stringify({
    //                departmentId: parseInt(sobeId),   // BURANI DÜZƏLTDİM
    //                kod: kod.toUpperCase(),
    //                ad: ad
    //            })
    //        })
    //            .then(res => res.json())
    //            .then(data => {

    //                if (!data.success) {
    //                    errorDiv.textContent = data.message;
    //                    errorDiv.classList.remove('d-none');
    //                    return;
    //                }

    //                const option = new Option(ad, data.id, true, true);
    //                senedNovuSelect.add(option);

    //                const modalInstance = bootstrap.Modal.getInstance(novModal);
    //                modalInstance.hide();

    //            })
    //            .catch(() => {
    //                errorDiv.textContent = "Server xətası baş verdi.";
    //                errorDiv.classList.remove('d-none');
    //            });

    //    });
    //}
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

document.addEventListener("DOMContentLoaded", function () {

    const openNovBtn = document.getElementById("openNovBtn");

    if (openNovBtn) {
        openNovBtn.addEventListener("click", function () {

            const modalElement = document.getElementById("novModal");
            const modal = new bootstrap.Modal(modalElement);

            modal.show();
        });
    }

});
document.addEventListener("DOMContentLoaded", function () {

    const saveBtn = document.getElementById("saveBtn");

    if (!saveBtn) return;

    saveBtn.addEventListener("click", function () {

        const sobeSelect = document.getElementById("sobeSelect");
        const kodInput = document.getElementById("novKod");
        const adInput = document.getElementById("novAd");
        const errorDiv = document.getElementById("novError");

        errorDiv.classList.add("d-none");

        const sobeId = parseInt(sobeSelect.value);
        const kod = kodInput.value.trim();
        const ad = adInput.value.trim();

        if (!sobeId || isNaN(sobeId)) {
            errorDiv.textContent = "Əvvəlcə şöbə seçilməlidir.";
            errorDiv.classList.remove("d-none");
            return;
        }

        if (!kod || !ad) {
            errorDiv.textContent = "Kod və Ad boş ola bilməz.";
            errorDiv.classList.remove("d-none");
            return;
        }

        fetch("/SenedDovriyyesi/Sened/YeniSenedNovu", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                departmentId: sobeId,
                kod: kod.toUpperCase(),
                ad: ad
            })
        })
            .then(res => res.json())
            .then(data => {

                if (!data.success) {
                    errorDiv.textContent = data.message;
                    errorDiv.classList.remove("d-none");
                    return;
                }

                location.reload(); // sadə və təmiz həll

            })
            .catch(() => {
                errorDiv.textContent = "Server xətası baş verdi.";
                errorDiv.classList.remove("d-none");
            });

    });

});

// YENİ
const silModalEl = document.getElementById('silModal');
if (silModalEl) {
    silModalEl.addEventListener('show.bs.modal', function (e) {
        var id = e.relatedTarget.getAttribute('data-id');
        document.getElementById('silId').value = id;
    });
}

function openFilePreview(btn) {
    console.log('openFilePreview çağırıldı', btn); // debug üçün

    const id = btn.dataset.id;
    const name = btn.dataset.name;
    const size = btn.dataset.size;
    const date = btn.dataset.date;
    const version = btn.dataset.version;
    const isActive = btn.dataset.active === 'true';
    const ext = (name.split('.').pop() || '').toLowerCase();

    const icons = {
        pdf: 'bi-file-earmark-pdf-fill text-danger',
        doc: 'bi-file-earmark-word-fill text-primary',
        docx: 'bi-file-earmark-word-fill text-primary',
        xls: 'bi-file-earmark-excel-fill text-success',
        xlsx: 'bi-file-earmark-excel-fill text-success',
        jpg: 'bi-file-earmark-image-fill text-warning',
        jpeg: 'bi-file-earmark-image-fill text-warning',
        png: 'bi-file-earmark-image-fill text-warning',
        gif: 'bi-file-earmark-image-fill text-warning',
        zip: 'bi-file-earmark-zip-fill text-secondary',
        rar: 'bi-file-earmark-zip-fill text-secondary'
    };

    const modalEl = document.getElementById('filePreviewModal');
    if (!modalEl) {
        console.error('filePreviewModal tapılmadı!');
        return;
    }

    document.getElementById('pvModalIcon').innerHTML =
        `<i class="bi ${icons[ext] || 'bi-file-earmark text-muted'}" style="font-size:1.5rem"></i>`;
    document.getElementById('pvModalTitle').textContent = name;
    document.getElementById('pvModalSub').textContent = size + ' · ' + date;
    document.getElementById('pvModalVersion').innerHTML =
        version + (isActive ? ' <span class="sd-badge sd-badge-success ms-1">Aktiv versiya</span>' : '');

    const downloadUrl = `/SenedDovriyyesi/Sened/Download/${id}`;
    const previewUrl = `/SenedDovriyyesi/Sened/Preview/${id}`;

    document.getElementById('pvDownloadBtn').href = downloadUrl;

    // ... funksiyanın əvvəli eyni qalır ...

    const body = document.getElementById('pvModalBody');
    const imgTypes = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp'];

    // ... funksiyanın daxilindəki PDF hissəsini bu şəkildə dəyişin:

    if (ext === 'pdf') {
        // Iframe-i konteynerin bütün sahəsini tutacaq şəkildə yerləşdiririk
        body.innerHTML = `<iframe src="${previewUrl}" allow="fullscreen"></iframe>`;
    } else if (imgTypes.includes(ext)) {
        body.innerHTML = `
        <div class="d-flex align-items-center justify-content-center h-100 w-100 p-2">
            <img src="${previewUrl}" class="img-fluid" style="max-height: 100%; object-fit: contain;" />
        </div>`;
    } else {
        // Digər fayllar üçün placeholder
        body.innerHTML = `
        <div class="d-flex flex-column align-items-center justify-content-center h-100 text-muted bg-light">
            <i class="bi ${icons[ext] || 'bi-file-earmark'} mb-3" style="font-size:4rem; opacity:.3"></i>
            <p class="fw-bold mb-1">${name}</p>
            <p class="small text-center px-4">${fileLabels[ext] || 'Bu fayl növü brauzerdə birbaşa göstərilə bilmir.'}</p>
            <a href="${downloadUrl}" class="btn btn-outline-primary mt-2">
                <i class="bi bi-download"></i> Faylı yüklə və bax
            </a>
        </div>`;
    }

    new bootstrap.Modal(modalEl).show();
}
// Preview düyməsi üçün event delegation (CSP-ni keçir)
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.btn-preview');
    if (!btn) return;
    openFilePreview(btn);
});
document.addEventListener("DOMContentLoaded", function () {

    const sobeSelect    = document.getElementById('sobeSelect');
    const dropZone      = document.getElementById('dropZone');
    const fileInput     = document.getElementById('fileInput');
    const novModalEl    = document.getElementById('novModal');
    const saveNovBtn    = document.getElementById('saveBtn');
    const openNovBtn    = document.getElementById('openNovBtn');

    // =============================
    // Fayl Drag & Drop
    // =============================
    if (dropZone && fileInput) {

        // Click on zone → open file picker (but not on remove button)
        dropZone.addEventListener('click', function (e) {
            if (e.target.closest('.sd-file-remove')) return;
            fileInput.click();
        });

        // Drag styling
        ['dragover', 'dragenter'].forEach(ev => {
            dropZone.addEventListener(ev, e => {
                e.preventDefault();
                e.stopPropagation();
                dropZone.classList.add('sd-file-drop-active');
            });
        });

        ['dragleave', 'dragend'].forEach(ev => {
            dropZone.addEventListener(ev, e => {
                e.preventDefault();
                e.stopPropagation();
                dropZone.classList.remove('sd-file-drop-active');
            });
        });

        dropZone.addEventListener('drop', e => {
            e.preventDefault();
            e.stopPropagation();
            dropZone.classList.remove('sd-file-drop-active');
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                fileInput.files = files;
                handleFileSelected(files[0]);
            }
        });

        fileInput.addEventListener('change', function () {
            if (!this.files.length) return;
            handleFileSelected(this.files[0]);
        });
    }

    function handleFileSelected(file) {
        const maxSize       = 25 * 1024 * 1024;
        const dropContent   = document.getElementById('dropContent');
        const filePreviewBox = document.getElementById('filePreviewBox');
        const fileNameEl    = document.getElementById('fileName');
        const fileMetaEl    = document.getElementById('fileMeta');
        const fileIconEl    = document.getElementById('fileIcon');

        if (file.size > maxSize) {
            showFieldError('Fayl ölçüsü 25MB-dan böyük ola bilməz.');
            if (fileInput) fileInput.value = '';
            return;
        }

        if (fileNameEl) fileNameEl.textContent = file.name;
        if (fileMetaEl) fileMetaEl.textContent = formatFileSize(file.size) + ' · ' + (file.type || 'Fayl');
        if (fileIconEl) {
            const ext = file.name.split('.').pop().toLowerCase();
            fileIconEl.innerHTML = '<i class="bi ' + getFileIconClass(ext) + '"></i>';
        }

        if (dropContent)    dropContent.classList.add('d-none');
        if (filePreviewBox) filePreviewBox.classList.remove('d-none');
    }

    function formatFileSize(bytes) {
        if (bytes < 1024)            return bytes + ' B';
        if (bytes < 1024 * 1024)     return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }

    function getFileIconClass(ext) {
        const map = {
            pdf:  'bi-file-earmark-pdf-fill text-danger',
            doc:  'bi-file-earmark-word-fill text-primary',
            docx: 'bi-file-earmark-word-fill text-primary',
            xls:  'bi-file-earmark-excel-fill text-success',
            xlsx: 'bi-file-earmark-excel-fill text-success',
            jpg:  'bi-file-earmark-image-fill text-warning',
            jpeg: 'bi-file-earmark-image-fill text-warning',
            png:  'bi-file-earmark-image-fill text-warning',
            gif:  'bi-file-earmark-image-fill text-warning',
            zip:  'bi-file-earmark-zip-fill text-secondary',
            rar:  'bi-file-earmark-zip-fill text-secondary',
        };
        return map[ext] || 'bi-file-earmark text-muted';
    }

    function showFieldError(msg) {
        const existing = document.getElementById('_fileError');
        if (existing) existing.remove();
        if (!dropZone) return;
        const el = document.createElement('div');
        el.id = '_fileError';
        el.className = 'text-danger small mt-1';
        el.textContent = msg;
        dropZone.insertAdjacentElement('afterend', el);
        setTimeout(() => el.remove(), 4000);
    }

    // =============================
    // Şöbə seçiləndə sənəd növlərini yüklə
    // =============================
    if (sobeSelect) {

        sobeSelect.addEventListener('change', function () {

            const sobeId          = this.value;
            const senedNovuSelect = document.getElementById('senedNovuSelect');
            const novWrapper      = document.getElementById('senedNovuWrapper');

            if (!senedNovuSelect) return;

            if (!sobeId) {
                senedNovuSelect.innerHTML = '<option value="">Əvvəlcə şöbə seçin...</option>';
                senedNovuSelect.disabled  = false;
                if (novWrapper) novWrapper.classList.remove('sd-select-loading');
                return;
            }

            // Loading state
            senedNovuSelect.innerHTML = '<option value="">Yüklənir...</option>';
            senedNovuSelect.disabled  = true;
            if (novWrapper) novWrapper.classList.add('sd-select-loading');

            const baseUrl = (typeof senedNovUrl !== 'undefined')
                ? senedNovUrl
                : '/SenedDovriyyesi/Sened/SenedNovleriByShobe';

            fetch(baseUrl + '?sobeId=' + encodeURIComponent(sobeId), {
                method: 'GET',
                credentials: 'include',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(response => {
                    if (!response.ok) throw new Error('HTTP ' + response.status);
                    return response.json();
                })
                .then(data => {
                    senedNovuSelect.disabled = false;
                    if (novWrapper) novWrapper.classList.remove('sd-select-loading');

                    if (!data || data.length === 0) {
                        senedNovuSelect.innerHTML = '<option value="">Bu şöbədə aktiv növ yoxdur</option>';
                        return;
                    }

                    senedNovuSelect.innerHTML = '<option value="">Sənəd növü seçin...</option>';
                    data.forEach(x => {
                        const opt       = document.createElement('option');
                        opt.value       = x.id;
                        opt.textContent = x.ad;
                        senedNovuSelect.appendChild(opt);
                    });
                })
                .catch(err => {
                    console.error('SenedNovu fetch error:', err);
                    senedNovuSelect.disabled = false;
                    if (novWrapper) novWrapper.classList.remove('sd-select-loading');
                    senedNovuSelect.innerHTML = '<option value="">Yükləmə xətası – yenidən cəhd edin</option>';
                });
        });
    }

    // =============================
    // Yeni Sənəd Növü modalı aç
    // =============================
    if (openNovBtn && novModalEl) {

        const novModal = new bootstrap.Modal(novModalEl);

        openNovBtn.addEventListener('click', function () {
            if (!sobeSelect || !sobeSelect.value) {
                sobeSelect.classList.add('is-invalid');
                sobeSelect.focus();
                sobeSelect.addEventListener('change', () => sobeSelect.classList.remove('is-invalid'), { once: true });
                return;
            }

            // Reset modal inputs
            const kodInput = document.getElementById('novKod');
            const adInput  = document.getElementById('novAd');
            const errDiv   = document.getElementById('novError');
            if (kodInput) kodInput.value = '';
            if (adInput)  adInput.value  = '';
            if (errDiv)   errDiv.classList.add('d-none');

            novModal.show();
            if (kodInput) setTimeout(() => kodInput.focus(), 300);
        });
    }

    // =============================
    // Yeni növ saxla
    // =============================
    if (saveNovBtn && novModalEl) {

        saveNovBtn.addEventListener('click', function () {

            const kod      = (document.getElementById('novKod')?.value ?? '').trim();
            const ad       = (document.getElementById('novAd')?.value ?? '').trim();
            const errorDiv = document.getElementById('novError');
            const sobeId   = sobeSelect?.value ?? '';

            if (errorDiv) errorDiv.classList.add('d-none');

            if (!sobeId) {
                if (errorDiv) { errorDiv.textContent = 'Əvvəlcə şöbə seçilməlidir.'; errorDiv.classList.remove('d-none'); }
                return;
            }
            if (!kod || !ad) {
                if (errorDiv) { errorDiv.textContent = 'Kod və Ad boş ola bilməz.'; errorDiv.classList.remove('d-none'); }
                return;
            }

            // Disable button + spinner
            saveNovBtn.disabled    = true;
            saveNovBtn.innerHTML   = '<span class="spinner-border spinner-border-sm me-1" role="status"></span>Saxlanır...';

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
                    saveNovBtn.disabled  = false;
                    saveNovBtn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Saxla';

                    if (!data.success) {
                        if (errorDiv) { errorDiv.textContent = data.message; errorDiv.classList.remove('d-none'); }
                        return;
                    }

                    const senedNovuSelect = document.getElementById('senedNovuSelect');
                    if (senedNovuSelect) {
                        const option = new Option(ad, data.id, true, true);
                        senedNovuSelect.add(option);
                    }

                    bootstrap.Modal.getInstance(novModalEl)?.hide();
                })
                .catch(() => {
                    saveNovBtn.disabled  = false;
                    saveNovBtn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Saxla';
                    if (errorDiv) { errorDiv.textContent = 'Server xətası baş verdi.'; errorDiv.classList.remove('d-none'); }
                });
        });

        // Enter key in modal inputs
        ['novKod', 'novAd'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); saveNovBtn.click(); } });
        });
    }

    // =============================
    // Detail page: yeni versiya upload preview
    // =============================
    const detailFileInput = document.getElementById('detailFileInput');
    if (detailFileInput) {
        detailFileInput.addEventListener('change', function () {
            const preview    = document.getElementById('detailFilePreview');
            const nameEl     = document.getElementById('detailFileName');
            const sizeEl     = document.getElementById('detailFileSize');
            const uploadBtn  = document.getElementById('uploadBtn');

            if (!this.files.length) {
                if (preview)   preview.style.display = 'none';
                if (uploadBtn) uploadBtn.disabled = true;
                return;
            }

            const file = this.files[0];
            if (nameEl)    nameEl.textContent = file.name;
            if (sizeEl)    sizeEl.textContent = '(' + formatFileSize(file.size) + ')';
            if (preview)   preview.style.display = '';
            if (uploadBtn) uploadBtn.disabled = false;
        });
    }

});

// Global: remove selected file (called from button onclick in template)
function removeFile() {
    const fileInput      = document.getElementById('fileInput');
    const dropContent    = document.getElementById('dropContent');
    const filePreviewBox = document.getElementById('filePreviewBox');
    if (fileInput)      fileInput.value = '';
    if (dropContent)    dropContent.classList.remove('d-none');
    if (filePreviewBox) filePreviewBox.classList.add('d-none');
}

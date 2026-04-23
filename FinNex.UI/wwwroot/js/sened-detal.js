// wwwroot/js/sened-detal.js
// Fayl preview funksionalligi - Detal sehifesi ucun

(function () {
    'use strict';

    var imgTypes = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp'];
    var pdfTypes = ['pdf'];

    function getExtension(filename) {
        return (filename.split('.').pop() || '').toLowerCase();
    }

    function openPreview(btn) {
        var id = btn.dataset.id;
        var name = btn.dataset.name;
        var size = btn.dataset.size;
        var date = btn.dataset.date;
        var version = btn.dataset.version;
        var isActive = btn.dataset.active === 'true';
        var ext = getExtension(name);

        var modalEl = document.getElementById('filePreviewModal');
        if (!modalEl) return;

        var previewUrl = '/SenedDovriyyesi/Sened/Preview/' + id;
        var downloadUrl = '/SenedDovriyyesi/Sened/Download/' + id;

        // Basliq
        var titleEl = document.getElementById('pvModalTitle');
        if (titleEl) titleEl.textContent = name;

        // Versiya info
        var versionEl = document.getElementById('pvModalVersion');
        if (versionEl) {
            versionEl.innerHTML = version + ' &middot; ' + size + ' &middot; ' + date;
            if (isActive) {
                versionEl.innerHTML += ' <span style="display:inline-block;padding:2px 8px;border-radius:6px;background:#dcfce7;color:#16a34a;font-size:10px;font-weight:600;margin-left:6px;">Aktiv</span>';
            }
        }

        // Download link
        var downloadBtn = document.getElementById('pvDownloadBtn');
        if (downloadBtn) downloadBtn.href = downloadUrl;

        // Body content
        var body = document.getElementById('pvModalBody');
        if (!body) return;

        if (pdfTypes.indexOf(ext) !== -1) {
            // PDF - iframe ile goster
            body.innerHTML = '<iframe src="' + previewUrl + '" style="width:100%;height:100%;border:none;" allow="fullscreen"></iframe>';
        } else if (imgTypes.indexOf(ext) !== -1) {
            // Sekil - img tag ile goster
            body.innerHTML = '<div style="display:flex;align-items:center;justify-content:center;width:100%;height:100%;padding:16px;background:#1a1a2e;">' +
                '<img src="' + previewUrl + '" style="max-width:100%;max-height:100%;object-fit:contain;border-radius:4px;" alt="' + name + '" />' +
                '</div>';
        } else {
            // Diger fayllar - preview movcud deyil
            body.innerHTML = '<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;width:100%;height:100%;background:#f8f9fc;color:#5a647a;">' +
                '<svg width="48" height="48" viewBox="0 0 48 48" fill="none" style="margin-bottom:16px;opacity:0.4;">' +
                '<path d="M12 6h18l10 10v26H12V6z" stroke="#8a93a8" stroke-width="2" stroke-linejoin="round"/>' +
                '<path d="M30 6v10h10" stroke="#8a93a8" stroke-width="2" stroke-linejoin="round"/>' +
                '</svg>' +
                '<div style="font-size:14px;font-weight:600;margin-bottom:6px;">Preview movcud deyil</div>' +
                '<div style="font-size:12px;color:#8a93a8;margin-bottom:16px;">' + name + '</div>' +
                '<a href="' + downloadUrl + '" class="fn-btn fn-btn--primary" style="font-size:12px;height:36px;padding:0 18px;text-decoration:none;">' +
                '<svg width="12" height="12" viewBox="0 0 12 12" fill="none" style="margin-right:6px;">' +
                '<path d="M6 2v6M3.5 5.5L6 8l2.5-2.5" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/>' +
                '<path d="M1 10h10" stroke="currentColor" stroke-width="1.3" stroke-linecap="round"/>' +
                '</svg>' +
                'Preview movcud deyil, endirin' +
                '</a>' +
                '</div>';
        }

        // Modali ac — həm class-ı sil həm də display-ı təyin et
        // (struktur.css overlay-ı display:none yapır, sened css-i flex; hər
        // iki halda garantili açılır deyə inline display:flex veririk)
        modalEl.classList.remove('str-modal-hidden');
        modalEl.style.display = 'flex';
    }

    function closePreview() {
        var modalEl = document.getElementById('filePreviewModal');
        if (!modalEl) return;
        modalEl.classList.add('str-modal-hidden');
        modalEl.style.display = 'none';

        // Iframe-in davam edib səs çıxartmaması üçün boşalt
        var body = document.getElementById('pvModalBody');
        if (body) body.innerHTML = '';
    }

    // Event delegation - btn-preview kliklerini tut
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.btn-preview');
        if (btn) {
            e.preventDefault();
            openPreview(btn);
            return;
        }

        // Bağlama düyməsi
        var closeBtn = e.target.closest('[data-close-modal="filePreviewModal"]');
        if (closeBtn) {
            e.preventDefault();
            closePreview();
            return;
        }

        // Overlay-ə klik (modaldan kənar boşluq)
        if (e.target && e.target.id === 'filePreviewModal') {
            closePreview();
        }
    });

    // ESC ilə bağla
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closePreview();
    });
})();

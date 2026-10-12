// Qoşma önizləmə modalı (12.10.2026) — Gələn/Göndərilən Mail, Tapşırılan Maillər
// paylaşır. PDF/şəkil üçün faylı birbaşa modalda göstərir (Titan-vari),
// digər fayl növlərində çıxarılmış mətni (varsa) göstərir, yoxdursa
// "önizləmə yoxdur" + Yüklə düyməsi ilə məlumatlandırır.
(function () {
    'use strict';

    var IMG_EXT = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.svg'];

    var overlay = null;

    function ensureOverlay() {
        if (overlay) return overlay;

        overlay = document.createElement('div');
        overlay.className = 'gm-preview-overlay';
        overlay.style.display = 'none';
        overlay.innerHTML =
            '<div class="gm-preview-dialog" role="dialog" aria-modal="true">' +
                '<div class="gm-preview-header">' +
                    '<i class="bi gm-preview-icon"></i>' +
                    '<span class="gm-preview-filename"></span>' +
                    '<div class="gm-preview-actions">' +
                        '<a class="gm-btn gm-btn-sm gm-btn-outline gm-preview-download" target="_blank" title="Yüklə">' +
                            '<i class="bi bi-download"></i>' +
                        '</a>' +
                        '<button type="button" class="gm-btn gm-btn-sm gm-btn-outline gm-preview-print" title="Çap et" style="display:none">' +
                            '<i class="bi bi-printer"></i>' +
                        '</button>' +
                        '<button type="button" class="gm-preview-close" title="Bağla">' +
                            '<i class="bi bi-x-lg"></i>' +
                        '</button>' +
                    '</div>' +
                '</div>' +
                '<div class="gm-preview-body"></div>' +
            '</div>';

        document.body.appendChild(overlay);

        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) close();
        });
        overlay.querySelector('.gm-preview-close').addEventListener('click', close);
        overlay.querySelector('.gm-preview-print').addEventListener('click', function () {
            var frame = overlay.querySelector('.gm-preview-iframe');
            if (frame && frame.contentWindow) {
                try { frame.contentWindow.focus(); frame.contentWindow.print(); } catch (err) { /* cross-origin guard */ }
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && overlay.style.display !== 'none') close();
        });

        return overlay;
    }

    function iconForExt(ext) {
        if (ext === '.pdf') return 'bi-file-earmark-pdf-fill text-danger';
        if (ext === '.docx' || ext === '.doc') return 'bi-file-earmark-word-fill text-primary';
        if (ext === '.xlsx' || ext === '.xls') return 'bi-file-earmark-excel-fill text-success';
        if (IMG_EXT.indexOf(ext) !== -1) return 'bi-file-earmark-image-fill text-info';
        return 'bi-file-earmark-fill text-secondary';
    }

    function close() {
        if (!overlay) return;
        overlay.style.display = 'none';
        overlay.querySelector('.gm-preview-body').innerHTML = '';
        document.body.style.overflow = '';
    }

    function open(opts) {
        var ov = ensureOverlay();
        var body = ov.querySelector('.gm-preview-body');
        var ext = (opts.ext || '').toLowerCase();

        ov.querySelector('.gm-preview-filename').textContent = opts.name || '';
        ov.querySelector('.gm-preview-icon').className = 'bi gm-preview-icon ' + iconForExt(ext);
        ov.querySelector('.gm-preview-download').setAttribute('href', opts.downloadUrl || '#');
        ov.querySelector('.gm-preview-print').style.display = ext === '.pdf' ? '' : 'none';

        body.innerHTML = '';

        if (ext === '.pdf') {
            var iframe = document.createElement('iframe');
            iframe.className = 'gm-preview-iframe';
            iframe.src = opts.viewUrl;
            body.appendChild(iframe);
        } else if (IMG_EXT.indexOf(ext) !== -1) {
            var wrap = document.createElement('div');
            wrap.className = 'gm-preview-imgwrap';
            var img = document.createElement('img');
            img.src = opts.viewUrl;
            img.alt = opts.name || '';
            wrap.appendChild(img);
            body.appendChild(wrap);
        } else if (opts.text && opts.text.trim().length > 0) {
            var pre = document.createElement('pre');
            pre.className = 'gm-preview-text';
            pre.textContent = opts.text;
            body.appendChild(pre);
        } else {
            var empty = document.createElement('div');
            empty.className = 'gm-preview-empty';
            empty.innerHTML = '<i class="bi bi-file-earmark-x"></i>';
            var p = document.createElement('p');
            p.textContent = 'Bu fayl növü üçün ekranda önizləmə dəstəklənmir. Baxmaq üçün yükləyin.';
            empty.appendChild(p);
            body.appendChild(empty);
        }

        ov.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }

    // Event delegation — düymələr AJAX ilə sonradan əlavə olunsa belə işləsin.
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.gm-preview-btn');
        if (!btn) return;
        e.preventDefault();

        var textEl = btn.dataset.textid ? document.getElementById(btn.dataset.textid) : null;

        open({
            name: btn.dataset.name || '',
            ext: btn.dataset.ext || '',
            viewUrl: btn.dataset.view || '',
            downloadUrl: btn.dataset.download || '',
            text: textEl ? textEl.textContent : ''
        });
    });

    window.gmOpenAttachmentPreview = open;
})();

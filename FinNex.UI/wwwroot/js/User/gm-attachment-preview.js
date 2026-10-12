// Qoşma önizləmə modalı (12.10.2026) — Gələn/Göndərilən Mail, Tapşırılan Maillər
// paylaşır. PDF/şəkil üçün faylı birbaşa modalda göstərir, .docx-i mammoth.js
// ilə formatlı HTML-ə, .xlsx/.xls-i isə SheetJS ilə cədvələ çevirib göstərir
// (Titan-vari təmiz baxış — istifadəçi: "bizde bele gosterir, ama titanda gor
// nece seliqelidir... bizde bele bir sey ede bilerik?"). Heç biri mümkün
// olmayanda (varsa) çıxarılmış mətni, yoxdursa "önizləmə yoxdur" + Yüklə
// mesajını göstərir.
(function () {
    'use strict';

    var IMG_EXT  = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.svg'];
    var WORD_EXT = ['.docx'];                 // mammoth köhnə binar .doc-u açmır
    var EXCEL_EXT = ['.xlsx', '.xls'];

    var overlay = null;
    var libPromises = {};                      // CDN kitabxanalarının keşi — hər önizləmədə YENİDƏN yüklənməsin

    function loadScript(url) {
        if (libPromises[url]) return libPromises[url];
        libPromises[url] = new Promise(function (resolve, reject) {
            var s = document.createElement('script');
            s.src = url;
            s.onload = function () { resolve(); };
            s.onerror = function () { delete libPromises[url]; reject(new Error('Kitabxana yüklənmədi: ' + url)); };
            document.head.appendChild(s);
        });
        return libPromises[url];
    }

    function ensureMammoth() {
        if (window.mammoth) return Promise.resolve();
        return loadScript('https://cdn.jsdelivr.net/npm/mammoth@1.8.0/mammoth.browser.min.js');
    }

    function ensureXlsx() {
        if (window.XLSX) return Promise.resolve();
        return loadScript('https://cdn.jsdelivr.net/npm/xlsx@0.18.5/dist/xlsx.full.min.js');
    }

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
            } else {
                try { window.print(); } catch (err) { /* ignore */ }
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
        overlay.querySelector('.gm-preview-print').style.display = 'none';
        document.body.style.overflow = '';
    }

    function showLoading(body) {
        body.innerHTML = '';
        var wrap = document.createElement('div');
        wrap.className = 'gm-preview-loading';
        wrap.innerHTML = '<div class="gm-preview-spinner"></div><span>Önizləmə hazırlanır...</span>';
        body.appendChild(wrap);
    }

    function showErrorOrFallback(body, name, downloadUrl, text, errorNote) {
        body.innerHTML = '';
        if (text && text.trim().length > 0) {
            var pre = document.createElement('pre');
            pre.className = 'gm-preview-text';
            pre.textContent = text;
            body.appendChild(pre);
            return;
        }
        var empty = document.createElement('div');
        empty.className = 'gm-preview-empty';
        empty.innerHTML = '<i class="bi bi-file-earmark-x"></i>';
        var p = document.createElement('p');
        p.textContent = errorNote || 'Bu fayl növü üçün ekranda önizləmə dəstəklənmir.';
        empty.appendChild(p);
        var a = document.createElement('a');
        a.className = 'gm-btn gm-btn-sm gm-btn-primary';
        a.href = downloadUrl || '#';
        a.target = '_blank';
        a.innerHTML = '<i class="bi bi-download"></i> Yüklə';
        empty.appendChild(a);
        body.appendChild(empty);
    }

    function renderDocx(body, viewUrl, downloadUrl, name, text) {
        showLoading(body);
        ensureMammoth()
            .then(function () { return fetch(viewUrl); })
            .then(function (r) {
                if (!r.ok) throw new Error('Fayl yüklənmədi (' + r.status + ')');
                return r.arrayBuffer();
            })
            .then(function (buf) { return window.mammoth.convertToHtml({ arrayBuffer: buf }); })
            .then(function (result) {
                var wrap = document.createElement('div');
                wrap.className = 'gm-preview-docx';
                wrap.innerHTML = result.value || '<p><em>Sənəd boşdur.</em></p>';
                body.innerHTML = '';
                body.appendChild(wrap);
            })
            .catch(function () {
                // Köhnə binar .doc (OLE2) mammoth ilə açılmır — sükutla mətnə/yükləməyə düş
                showErrorOrFallback(body, name, downloadUrl, text,
                    'Bu Word faylı (köhnə .doc formatı ola bilər) ekranda birbaşa açıla bilmədi.');
            });
    }

    function renderXlsx(body, viewUrl, downloadUrl, name, text) {
        showLoading(body);
        ensureXlsx()
            .then(function () { return fetch(viewUrl); })
            .then(function (r) {
                if (!r.ok) throw new Error('Fayl yüklənmədi (' + r.status + ')');
                return r.arrayBuffer();
            })
            .then(function (buf) {
                var wb = window.XLSX.read(buf, { type: 'array' });
                var sheetNames = wb.SheetNames || [];
                if (sheetNames.length === 0) throw new Error('Vərəq tapılmadı');

                var wrap = document.createElement('div');
                wrap.className = 'gm-preview-xlsx';

                if (sheetNames.length > 1) {
                    var tabs = document.createElement('div');
                    tabs.className = 'gm-preview-xlsx-tabs';
                    sheetNames.forEach(function (sn, i) {
                        var tab = document.createElement('button');
                        tab.type = 'button';
                        tab.className = 'gm-preview-xlsx-tab' + (i === 0 ? ' gm-preview-xlsx-tab--active' : '');
                        tab.textContent = sn;
                        tab.addEventListener('click', function () {
                            tabs.querySelectorAll('.gm-preview-xlsx-tab').forEach(function (t) { t.classList.remove('gm-preview-xlsx-tab--active'); });
                            tab.classList.add('gm-preview-xlsx-tab--active');
                            wrap.querySelectorAll('.gm-preview-xlsx-sheet').forEach(function (s) { s.style.display = 'none'; });
                            var target = wrap.querySelector('[data-sheet-index="' + i + '"]');
                            if (target) target.style.display = '';
                        });
                        tabs.appendChild(tab);
                    });
                    wrap.appendChild(tabs);
                }

                sheetNames.forEach(function (sn, i) {
                    var sheetDiv = document.createElement('div');
                    sheetDiv.className = 'gm-preview-xlsx-sheet';
                    sheetDiv.setAttribute('data-sheet-index', i);
                    if (i !== 0) sheetDiv.style.display = 'none';
                    sheetDiv.innerHTML = window.XLSX.utils.sheet_to_html(wb.Sheets[sn], { id: 'gm-xlsx-' + i });
                    wrap.appendChild(sheetDiv);
                });

                body.innerHTML = '';
                body.appendChild(wrap);
            })
            .catch(function () {
                showErrorOrFallback(body, name, downloadUrl, text,
                    'Bu Excel faylı ekranda birbaşa açıla bilmədi.');
            });
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
        } else if (WORD_EXT.indexOf(ext) !== -1) {
            renderDocx(body, opts.viewUrl, opts.downloadUrl, opts.name, opts.text);
        } else if (EXCEL_EXT.indexOf(ext) !== -1) {
            renderXlsx(body, opts.viewUrl, opts.downloadUrl, opts.name, opts.text);
        } else {
            showErrorOrFallback(body, opts.name, opts.downloadUrl, opts.text, null);
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

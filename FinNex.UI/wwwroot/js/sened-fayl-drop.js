// wwwroot/js/sened-fayl-drop.js
// Bir və ya bir neçə fayl seçməyi dəstəkləyir.
// Drag&drop hər dəfə YENİ faylları siyahıya ƏLAVƏ edir (əvəz etmir).

document.addEventListener('DOMContentLoaded', function () {
    var zone = document.getElementById('faylZone');
    var input = document.getElementById('Fayl');
    var listEl = document.getElementById('faylList');
    var countEl = document.getElementById('faylCount');
    var infoEl = document.getElementById('faylInfo');
    var defEl = document.getElementById('faylDefault');

    if (!zone || !input) return;

    // File input change (click → file dialog) — mövcud faylları əvəz edir
    input.addEventListener('change', function () {
        render(this.files);
    });

    // Drag over
    zone.addEventListener('dragover', function (e) {
        e.preventDefault();
        zone.classList.add('sd-file-zone--active');
    });

    zone.addEventListener('dragleave', function () {
        zone.classList.remove('sd-file-zone--active');
    });

    // Drop — yenilərini mövcudların üstünə ƏLAVƏ edir
    zone.addEventListener('drop', function (e) {
        e.preventDefault();
        zone.classList.remove('sd-file-zone--active');

        var newFiles = e.dataTransfer.files;
        if (!newFiles || newFiles.length === 0) return;

        var dt = new DataTransfer();

        // 1) Əvvəlki fayllar saxlansın
        if (input.files) {
            for (var i = 0; i < input.files.length; i++) dt.items.add(input.files[i]);
        }

        // 2) Yenilərini əlavə et — dublikatları atla (ad + ölçüsü görə)
        for (var j = 0; j < newFiles.length; j++) {
            var nf = newFiles[j];
            var exists = false;
            for (var k = 0; k < dt.items.length; k++) {
                var ex = dt.files[k];
                if (ex.name === nf.name && ex.size === nf.size) { exists = true; break; }
            }
            if (!exists) dt.items.add(nf);
        }

        input.files = dt.files;
        render(input.files);
    });

    // Zone-a klik edəndə input açılsın (silmə düyməsi istisna)
    zone.addEventListener('click', function (e) {
        if (e.target === input) return;
        if (e.target && e.target.classList && e.target.classList.contains('fayl-remove')) return;
        input.click();
    });

    function formatSize(bytes) {
        var kb = (bytes / 1024).toFixed(1);
        var mb = (bytes / 1024 / 1024).toFixed(2);
        return bytes > 1024 * 1024 ? mb + ' MB' : kb + ' KB';
    }

    function removeAt(index) {
        var dt = new DataTransfer();
        for (var i = 0; i < input.files.length; i++) {
            if (i !== index) dt.items.add(input.files[i]);
        }
        input.files = dt.files;
        render(input.files);
    }

    function render(files) {
        if (!listEl) return;
        listEl.innerHTML = '';

        if (!files || files.length === 0) {
            if (infoEl) infoEl.setAttribute('hidden', '');
            if (defEl) defEl.removeAttribute('hidden');
            if (countEl) countEl.textContent = '';
            return;
        }

        for (var i = 0; i < files.length; i++) {
            (function (f, idx) {
                var li = document.createElement('li');
                li.style.cssText = 'display:flex;align-items:center;gap:8px;padding:6px 10px;background:rgba(99,102,241,0.06);border:1px solid rgba(99,102,241,0.15);border-radius:6px;';
                li.innerHTML =
                    '<svg width="14" height="14" viewBox="0 0 14 14" fill="none"><path d="M3 2h6l2 2v8H3V2z" stroke="#6366f1" stroke-width="1.1" stroke-linejoin="round"/></svg>' +
                    '<span style="flex:1;font-size:12px;font-weight:600;color:#1a2332;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">' + f.name + '</span>' +
                    '<span style="font-size:11px;color:#8a93a8;">' + formatSize(f.size) + '</span>' +
                    '<button type="button" class="fayl-remove" data-idx="' + idx + '" title="Sil" ' +
                    'style="border:none;background:transparent;color:#ef4444;font-size:15px;line-height:1;cursor:pointer;padding:0 4px;">×</button>';
                listEl.appendChild(li);
            })(files[i], i);
        }

        // Silmə düyməsi click handleri
        Array.prototype.forEach.call(listEl.querySelectorAll('.fayl-remove'), function (btn) {
            btn.addEventListener('click', function (ev) {
                ev.stopPropagation();
                removeAt(parseInt(btn.getAttribute('data-idx'), 10));
            });
        });

        if (countEl) countEl.textContent = files.length + ' fayl seçildi';
        if (infoEl) infoEl.removeAttribute('hidden');
        if (defEl) defEl.setAttribute('hidden', '');
    }
});

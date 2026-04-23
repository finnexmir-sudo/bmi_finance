// wwwroot/js/sened-fayl-drop.js
// Bir və ya bir neçə fayl seçməyi dəstəkləyir.

document.addEventListener('DOMContentLoaded', function () {
    var zone = document.getElementById('faylZone');
    var input = document.getElementById('Fayl');
    var listEl = document.getElementById('faylList');
    var countEl = document.getElementById('faylCount');
    var infoEl = document.getElementById('faylInfo');
    var defEl = document.getElementById('faylDefault');

    if (!zone || !input) return;

    // Fayl seçiləndə göstər
    input.addEventListener('change', function () {
        showFiles(this.files);
    });

    // Drag over
    zone.addEventListener('dragover', function (e) {
        e.preventDefault();
        zone.classList.add('sd-file-zone--active');
    });

    zone.addEventListener('dragleave', function () {
        zone.classList.remove('sd-file-zone--active');
    });

    // Drop — çoxlu fayl da qəbul edir
    zone.addEventListener('drop', function (e) {
        e.preventDefault();
        zone.classList.remove('sd-file-zone--active');

        var files = e.dataTransfer.files;
        if (!files || files.length === 0) return;

        // input-a bütün faylları təyin et
        var dt = new DataTransfer();
        for (var i = 0; i < files.length; i++) dt.items.add(files[i]);
        input.files = dt.files;

        showFiles(dt.files);
    });

    // Zone-a klik edəndə input açılsın
    zone.addEventListener('click', function (e) {
        if (e.target === input) return;
        input.click();
    });

    function formatSize(bytes) {
        var kb = (bytes / 1024).toFixed(1);
        var mb = (bytes / 1024 / 1024).toFixed(2);
        return bytes > 1024 * 1024 ? mb + ' MB' : kb + ' KB';
    }

    function showFiles(files) {
        if (!files || files.length === 0) return;

        if (listEl) {
            listEl.innerHTML = '';
            for (var i = 0; i < files.length; i++) {
                var f = files[i];
                var li = document.createElement('li');
                li.style.cssText = 'display:flex;align-items:center;gap:8px;padding:6px 10px;background:rgba(99,102,241,0.06);border:1px solid rgba(99,102,241,0.15);border-radius:6px;';
                li.innerHTML =
                    '<svg width="14" height="14" viewBox="0 0 14 14" fill="none"><path d="M3 2h6l2 2v8H3V2z" stroke="#6366f1" stroke-width="1.1" stroke-linejoin="round"/></svg>' +
                    '<span style="flex:1;font-size:12px;font-weight:600;color:#1a2332;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">' + f.name + '</span>' +
                    '<span style="font-size:11px;color:#8a93a8;">' + formatSize(f.size) + '</span>';
                listEl.appendChild(li);
            }
        }
        if (countEl) countEl.textContent = files.length + ' fayl seçildi';
        if (infoEl) infoEl.removeAttribute('hidden');
        if (defEl) defEl.setAttribute('hidden', '');
    }
});

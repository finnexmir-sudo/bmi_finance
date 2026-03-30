// wwwroot/js/sened-fayl-drop.js

document.addEventListener('DOMContentLoaded', function () {
    var zone = document.getElementById('faylZone');
    var input = document.getElementById('Fayl');
    var nameEl = document.getElementById('faylAd');
    var sizeEl = document.getElementById('faylOlcu');
    var infoEl = document.getElementById('faylInfo');
    var defEl = document.getElementById('faylDefault');

    if (!zone || !input) return;

    // Fayl seçiləndə göstər
    input.addEventListener('change', function () {
        showFile(this.files[0]);
    });

    // Drag over
    zone.addEventListener('dragover', function (e) {
        e.preventDefault();
        zone.classList.add('sd-file-zone--active');
    });

    zone.addEventListener('dragleave', function () {
        zone.classList.remove('sd-file-zone--active');
    });

    // Drop
    zone.addEventListener('drop', function (e) {
        e.preventDefault();
        zone.classList.remove('sd-file-zone--active');

        var file = e.dataTransfer.files[0];
        if (!file) return;

        // input-a faylı təyin et
        var dt = new DataTransfer();
        dt.items.add(file);
        input.files = dt.files;

        showFile(file);
    });

    // Zone-a klik edəndə input açılsın
    zone.addEventListener('click', function (e) {
        if (e.target === input) return;
        input.click();
    });

    function showFile(file) {
        if (!file) return;
        var kb = (file.size / 1024).toFixed(1);
        var mb = (file.size / 1024 / 1024).toFixed(2);
        var size = file.size > 1024 * 1024 ? mb + ' MB' : kb + ' KB';

        if (nameEl) nameEl.textContent = file.name;
        if (sizeEl) sizeEl.textContent = size;
        if (infoEl) infoEl.removeAttribute('hidden');
        if (defEl) defEl.setAttribute('hidden', '');
    }
});
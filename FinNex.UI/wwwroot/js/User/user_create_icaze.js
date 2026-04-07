// ── Yalnız rəqəm və ":" icazə ver, avtomatik ":" əlavə et ──
function formatSaat(e) {
    var input = e.target;
    var val = input.value.replace(/[^\d]/g, '');
    if (val.length >= 3) {
        val = val.substring(0, 2) + ':' + val.substring(2, 4);
    }
    input.value = val.substring(0, 5);
    hesabla();
}

function parseTime(str) {
    var m = /^(\d{1,2}):(\d{2})$/.exec((str || '').trim());
    if (!m) return null;
    var h = parseInt(m[1], 10);
    var min = parseInt(m[2], 10);
    if (h > 23 || min > 59) return null;
    return h * 60 + min;
}

function hesabla() {
    var bas = parseTime(document.getElementById('baslamaSaati').value);
    var bitis = parseTime(document.getElementById('bitisSaati').value);
    var box = document.getElementById('durationBox');
    var txt = document.getElementById('durationText');
    var errBox = document.getElementById('timeErrorBox');

    if (bas === null || bitis === null) {
        box.style.display = 'none';
        if (errBox) errBox.style.display = 'none';
        return;
    }

    var diff = bitis - bas;

    if (diff <= 0) {
        box.style.display = 'none';
        if (errBox) {
            errBox.style.display = 'flex';
            errBox.textContent = 'Bitmə saatı başlama saatından sonra olmalıdır.';
        }
        return;
    }

    if (errBox) errBox.style.display = 'none';

    var saat = Math.floor(diff / 60);
    var deq = diff % 60;
    var metn = saat > 0 ? saat + ' saat' : '';
    if (deq > 0) metn += (metn ? ' ' : '') + deq + ' dəqiqə';

    txt.textContent = metn;
    box.style.display = 'flex';
}

var basEl = document.getElementById('baslamaSaati');
var bitisEl = document.getElementById('bitisSaati');

basEl.addEventListener('input', formatSaat);
bitisEl.addEventListener('input', formatSaat);
basEl.addEventListener('change', hesabla);
bitisEl.addEventListener('change', hesabla);
hesabla();

// ── Saat mask: istifadəçi yalnız rəqəm yazır, ":" avtomatik qoyulur ──
function maskSaat(e) {
    var input = e.target;
    var raw = input.value.replace(/[^\d]/g, '');

    // Maksimum 4 rəqəm (HHmm)
    if (raw.length > 4) raw = raw.substring(0, 4);

    // Avtomatik ":" əlavə et (3+ rəqəm olduqda)
    if (raw.length >= 3) {
        input.value = raw.substring(0, 2) + ':' + raw.substring(2);
    } else {
        input.value = raw;
    }

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

    naharEfektivGoster(diff);
}

// ── Nahar seçiləndə sayğaca yazılacaq (effektiv) müddəti göstər ──
// İşçi "nahara çıxmıram" seçəndə pəncərə uzun ola bilir, amma sayğacdan sabit
// nahar fasiləsi qədər AZ yazılır. Bu qarşılığı formada dərhal göstəririk ki,
// işçi nə aldığını və nə yazıldığını eyni anda görsün.
function deqMetn(t) {
    var s = Math.floor(t / 60), m = t % 60;
    var x = s > 0 ? s + ' saat' : '';
    if (m > 0) x += (x ? ' ' : '') + m + ' dəqiqə';
    return x || '0 dəqiqə';
}

function naharEfektivGoster(diff) {
    var cb = document.querySelector('input[name="NaharNezereAlinmasin"]');
    var kutu = document.getElementById('naharEfektivBox');
    var mtn = document.getElementById('naharEfektivText');
    if (!cb || !kutu || !mtn) return;

    if (!cb.checked || !diff || diff <= 0) {
        kutu.style.display = 'none';
        return;
    }

    var naharDeq = parseInt(cb.getAttribute('data-nahar-deq'), 10);
    if (isNaN(naharDeq)) naharDeq = 45;

    var sayilan = Math.max(0, diff - Math.min(naharDeq, diff));
    mtn.textContent = deqMetn(sayilan) + ' (pəncərə ' + deqMetn(diff) + ', −' + Math.min(naharDeq, diff) + ' dəq nahar)';
    kutu.style.display = 'block';
}

var basEl = document.getElementById('baslamaSaati');
var bitisEl = document.getElementById('bitisSaati');

basEl.addEventListener('input', maskSaat);
bitisEl.addEventListener('input', maskSaat);

var naharCb = document.querySelector('input[name="NaharNezereAlinmasin"]');
if (naharCb) naharCb.addEventListener('change', hesabla);

hesabla();

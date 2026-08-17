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
        naharEfektivGoster(0);
        jetonTutulmaGoster(0);
        return;
    }

    var diff = bitis - bas;

    if (diff <= 0) {
        box.style.display = 'none';
        naharEfektivGoster(0);
        jetonTutulmaGoster(0);
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
    jetonTutulmaGoster(diff);
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

// Serverdəki qayda ilə eyni olmalıdır: IcazeService.EffektivDeq / MecburiJetonSaat.
// Bu yalnız GÖSTƏRMƏ qatıdır — yekun qərarı server verir.
var ADI_MAX_DEQ = 180;

function naharDeqiqesi() {
    var cb = document.querySelector('input[name="NaharNezereAlinmasin"]');
    if (!cb) return 45;
    var n = parseInt(cb.getAttribute('data-nahar-deq'), 10);
    return isNaN(n) ? 45 : n;
}

// Pəncərədən sabit nahar fasiləsi çıxıldıqdan sonra qalan dəqiqə.
function effektivDeq(diff) {
    var cb = document.querySelector('input[name="NaharNezereAlinmasin"]');
    var cixilan = (cb && cb.checked) ? Math.min(naharDeqiqesi(), diff) : 0;
    return Math.max(0, diff - cixilan);
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

    var naharDeq = naharDeqiqesi();
    var sayilan = effektivDeq(diff);
    mtn.textContent = deqMetn(sayilan) + ' (pəncərə ' + deqMetn(diff) + ', −' + Math.min(naharDeq, diff) + ' dəq nahar)';
    kutu.style.display = 'block';
}

// ── Jetonla uzatma: tutulacaq miqdarı göstər ──
// Miqdar formada YAZILMIR — pəncərədən hesablanır (serverdə MecburiJetonSaat).
// İşçi nə qazandığını (uzun pəncərə) və nəyin qarşılığında (jeton) eyni anda görsün.
function jetonTutulmaGoster(diff) {
    var uzatCb = document.getElementById('jetonlaUzat');
    var kutu = document.getElementById('jetonTutulmaBox');
    var mtn = document.getElementById('jetonTutulmaText');
    if (!uzatCb || !kutu || !mtn) return;

    var balans = parseFloat(uzatCb.getAttribute('data-jeton-balans'));
    if (isNaN(balans)) balans = 0;

    if (!uzatCb.checked || !diff || diff <= 0) {
        kutu.style.display = 'none';
        return;
    }

    var artiqDeq = effektivDeq(diff) - ADI_MAX_DEQ;
    if (artiqDeq <= 0) {
        mtn.textContent = 'Bu icazə 3 saatlıq limitə sığır — jeton tutulmayacaq.';
        kutu.style.background = '#ecfdf5';
        kutu.style.color = '#065f46';
        kutu.style.display = 'block';
        return;
    }

    var tutulacaq = Math.ceil(artiqDeq / 60 * 100) / 100;
    if (tutulacaq > balans) {
        mtn.textContent = 'Bu pəncərə üçün ' + tutulacaq.toFixed(2) + ' saat jeton lazımdır, '
            + 'balansınız isə ' + balans + ' saatdır — icazəni qısaldın.';
        kutu.style.background = '#fee2e2';
        kutu.style.color = '#991b1b';
    } else {
        mtn.textContent = 'Jetonunuzdan tutulacaq: ' + tutulacaq.toFixed(2) + ' saat '
            + '(qalıq ' + (balans - tutulacaq).toFixed(2) + ' saat). '
            + 'İllik icazə sayğacınıza 3 saat yazılacaq.';
        kutu.style.background = '#fff8e8';
        kutu.style.color = '#8a6a18';
    }
    kutu.style.display = 'block';
}

var basEl = document.getElementById('baslamaSaati');
var bitisEl = document.getElementById('bitisSaati');

basEl.addEventListener('input', maskSaat);
bitisEl.addEventListener('input', maskSaat);

var naharCb = document.querySelector('input[name="NaharNezereAlinmasin"]');
if (naharCb) naharCb.addEventListener('change', hesabla);

var uzatCbEl = document.getElementById('jetonlaUzat');
if (uzatCbEl) uzatCbEl.addEventListener('change', hesabla);

hesabla();

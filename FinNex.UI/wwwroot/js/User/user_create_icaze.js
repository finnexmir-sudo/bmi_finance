function hesabla() {
    var bas = document.getElementById('baslamaSaati').value;
    var bitis = document.getElementById('bitisSaati').value;
    var box = document.getElementById('durationBox');
    var txt = document.getElementById('durationText');
    var errBox = document.getElementById('timeErrorBox');

    if (!bas || !bitis) {
        box.style.display = 'none';
        if (errBox) errBox.style.display = 'none';
        return;
    }

    var bP = bas.split(':').map(Number);
    var eP = bitis.split(':').map(Number);
    var diff = (eP[0] * 60 + (eP[1] || 0)) - (bP[0] * 60 + (bP[1] || 0));

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

document.getElementById('baslamaSaati').addEventListener('change', hesabla);
document.getElementById('bitisSaati').addEventListener('change', hesabla);
hesabla();

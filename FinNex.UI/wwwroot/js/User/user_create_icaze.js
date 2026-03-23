function hesabla() {
    var bas = document.getElementById('baslamaSaati').value;
    var bitis = document.getElementById('bitisSaati').value;
    var box = document.getElementById('durationBox');
    var txt = document.getElementById('durationText');

    if (!bas || !bitis) { box.style.display = 'none'; return; }

    var bParts = bas.split(':').map(Number);
    var eParts = bitis.split(':').map(Number);
    var diff = (eParts[0] * 60 + eParts[1]) - (bParts[0] * 60 + bParts[1]);

    if (diff <= 0) { box.style.display = 'none'; return; }

    var saat = Math.floor(diff / 60);
    var deqiqe = diff % 60;
    var metn = saat > 0 ? saat + ' saat' : '';
    if (deqiqe > 0) metn += (metn ? ' ' : '') + deqiqe + ' dəqiqə';

    txt.textContent = metn;
    box.style.display = 'flex';
}

document.getElementById('baslamaSaati').addEventListener('change', hesabla);
document.getElementById('bitisSaati').addEventListener('change', hesabla);
hesabla();
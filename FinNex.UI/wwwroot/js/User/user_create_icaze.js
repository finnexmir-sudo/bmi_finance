function hesabla() {
    var basVal = document.getElementById('baslamaSaati').value;
    var bitisVal = document.getElementById('bitisSaati').value;
    var box = document.getElementById('durationBox');
    var txt = document.getElementById('durationText');

    if (!basVal || !bitisVal) { box.style.display = 'none'; return; }

    var bP = basVal.split(':');
    var eP = bitisVal.split(':');
    var diff = ((+eP[0]) * 60 + (+eP[1])) - ((+bP[0]) * 60 + (+bP[1]));

    if (diff <= 0) { box.style.display = 'none'; return; }

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

function hesabla() {
    var basEl = document.getElementById('baslamaSaati');
    var bitisEl = document.getElementById('bitisSaati');
    var box = document.getElementById('durationBox');
    var txt = document.getElementById('durationText');

    var bSaat = parseInt(basEl.value, 10) || 0;
    var eSaat = parseInt(bitisEl.value, 10) || 0;
    var diff = eSaat - bSaat;

    if (diff <= 0) { box.style.display = 'none'; return; }

    txt.textContent = diff + ' saat';
    box.style.display = 'flex';
}

document.getElementById('baslamaSaati').addEventListener('change', hesabla);
document.getElementById('bitisSaati').addEventListener('change', hesabla);
hesabla();

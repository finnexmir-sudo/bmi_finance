// Dəqiqələri sıfırla — tam saata yuvarla
function tamSaat(input) {
    var val = input.value;
    if (!val) return;
    var parts = val.split(':');
    var saat = parseInt(parts[0], 10);
    if (isNaN(saat)) return;
    input.value = (saat < 10 ? '0' : '') + saat + ':00';
}

function hesabla() {
    var basEl = document.getElementById('baslamaSaati');
    var bitisEl = document.getElementById('bitisSaati');
    var box = document.getElementById('durationBox');
    var txt = document.getElementById('durationText');

    // Tam saata yuvarla
    tamSaat(basEl);
    tamSaat(bitisEl);

    var bas = basEl.value;
    var bitis = bitisEl.value;

    if (!bas || !bitis) { box.style.display = 'none'; return; }

    var bParts = bas.split(':');
    var eParts = bitis.split(':');
    var bSaat = parseInt(bParts[0], 10) || 0;
    var eSaat = parseInt(eParts[0], 10) || 0;
    var diff = eSaat - bSaat;

    if (diff <= 0) { box.style.display = 'none'; return; }

    txt.textContent = diff + ' saat';
    box.style.display = 'flex';
}

document.getElementById('baslamaSaati').addEventListener('change', hesabla);
document.getElementById('bitisSaati').addEventListener('change', hesabla);
hesabla();

// Nov seçimi
document.querySelectorAll('.fn-nov-radio').forEach(function (radio) {
    radio.addEventListener('change', function () {
        document.querySelectorAll('.fn-nov-card').forEach(c => c.classList.remove('fn-nov-card--active'));
        this.closest('.fn-nov-card').classList.add('fn-nov-card--active');
    });
});

// Müddət hesabla
function hesabla() {
    var bas = document.getElementById('baslamaTarixi').value;
    var bitis = document.getElementById('bitmeTarixi').value;
    var box = document.getElementById('durationBox');
    var txt = document.getElementById('durationText');

    if (!bas || !bitis) { box.style.display = 'none'; return; }

    var d1 = new Date(bas);
    var d2 = new Date(bitis);
    var diff = Math.round((d2 - d1) / (1000 * 60 * 60 * 24)) + 1;

    if (diff <= 0) { box.style.display = 'none'; return; }

    txt.textContent = diff + ' təqvim günü (~' + Math.max(1, Math.round(diff * 5 / 7)) + ' iş günü)';
    box.style.display = 'flex';
}

document.getElementById('baslamaTarixi').addEventListener('change', hesabla);
document.getElementById('bitmeTarixi').addEventListener('change', hesabla);
hesabla();
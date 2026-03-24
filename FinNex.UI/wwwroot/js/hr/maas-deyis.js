// Maaş Dəyişikliyi - real-vaxt fərq göstəricisi
document.addEventListener('DOMContentLoaded', function () {
    var input = document.getElementById('yeniMaasInput');
    var preview = document.getElementById('diffPreview');
    var diffTo = document.getElementById('diffTo');
    var diffAmount = document.getElementById('diffAmount');

    if (!input || !preview) return;

    function fmt(n) {
        return n.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ' ') + ' ₼';
    }

    input.addEventListener('input', function () {
        var val = parseFloat(this.value);

        if (!val || val <= 0) {
            preview.style.display = 'none';
            return;
        }

        preview.style.display = 'flex';
        diffTo.textContent = fmt(val);

        var ferg = val - kohneMaas;
        var abs = Math.abs(ferg);

        if (ferg >= 0) {
            diffAmount.textContent = '▲ +' + fmt(abs);
            diffAmount.style.background = 'rgba(16,185,129,.12)';
            diffAmount.style.color = '#059669';
            diffTo.style.color = '#059669';
        } else {
            diffAmount.textContent = '▼ -' + fmt(abs);
            diffAmount.style.background = 'rgba(239,68,68,.1)';
            diffAmount.style.color = '#dc2626';
            diffTo.style.color = '#dc2626';
        }
    });
});

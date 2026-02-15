document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('isciForm');

    if (!form) return;

    // FIN kod formatlaması
    const finInput = form.querySelector('input[name="FIN"]');
    if (finInput) {
        finInput.addEventListener('input', function (e) {
            this.value = this.value.toUpperCase();
        });
    }

    // Telefon formatlaması
    const telefonInput = form.querySelector('input[name="Telefon"]');
    if (telefonInput) {
        telefonInput.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, '');

            if (value.startsWith('994')) {
                value = '+' + value;
            } else if (value.startsWith('0')) {
                value = '+994' + value.substring(1);
            } else if (value.length > 0 && !value.startsWith('+')) {
                value = '+994' + value;
            }

            e.target.value = value;
        });
    }

    // Form validasiyası
    form.addEventListener('submit', function (e) {
        if (!form.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();

            const firstError = form.querySelector(':invalid');
            if (firstError) {
                firstError.focus();
            }
        }
    });
});
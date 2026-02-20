document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('isciForm');

    if (!form) return;

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

            // Format: +994 XX XXX XX XX
            if (value.length > 4) {
                value = value.slice(0, 4) + ' ' + value.slice(4);
            }
            if (value.length > 7) {
                value = value.slice(0, 7) + ' ' + value.slice(7);
            }
            if (value.length > 11) {
                value = value.slice(0, 11) + ' ' + value.slice(11);
            }
            if (value.length > 14) {
                value = value.slice(0, 14) + ' ' + value.slice(14, 16);
            }

            e.target.value = value.slice(0, 17);
        });
    }

    // Form animasiyası
    const inputs = form.querySelectorAll('.form-control, .form-select');
    inputs.forEach(input => {
        input.addEventListener('focus', function () {
            this.closest('.form-group').classList.add('focused');
        });

        input.addEventListener('blur', function () {
            this.closest('.form-group').classList.remove('focused');
        });
    });

    // Form validasiyası
    form.addEventListener('submit', function (e) {
        if (!form.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();

            // İlk xətalı sahəyə scroll
            const firstError = form.querySelector('.input-validation-error');
            if (firstError) {
                firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                firstError.focus();
            }
        }

        form.classList.add('was-validated');
    });

    // Loading state
    form.addEventListener('submit', function () {
        const submitBtn = form.querySelector('button[type="submit"]');
        if (submitBtn && form.checkValidity()) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Saxlanılır...';
        }
    });
});
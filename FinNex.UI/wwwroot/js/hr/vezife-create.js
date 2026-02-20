document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('vezifeForm');

    if (!form) return;

    // Maaş input formatlaması
    const maasInput = form.querySelector('input[name="Maas"]');
    if (maasInput) {
        maasInput.addEventListener('blur', function () {
            if (this.value) {
                const value = parseFloat(this.value);
                if (!isNaN(value)) {
                    this.value = value.toFixed(2);
                }
            }
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

    // Checkbox animasiyası
    const checkbox = form.querySelector('.form-checkbox');
    if (checkbox) {
        checkbox.addEventListener('change', function () {
            this.parentElement.style.transform = 'scale(1.05)';
            setTimeout(() => {
                this.parentElement.style.transform = 'scale(1)';
            }, 200);
        });
    }
});
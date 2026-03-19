document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('isciForm');
    if (!form) return;

    const depSelect = form.querySelector('select[name="DepartamentId"]');
    const vezSelect = form.querySelector('select[name="VezifeId"]');

    // 1. FIN kod formatlaması
    const finInput = form.querySelector('input[name="FIN"]');
    if (finInput) {
        finInput.addEventListener('input', function () {
            this.value = this.value.toUpperCase();
        });
    }

    // 2. Telefon formatlaması
    const telefonInput = form.querySelector('input[name="Telefon"]');
    if (telefonInput) {
        telefonInput.addEventListener('input', function (e) {
            let value = e.target.value.replace(/\D/g, '');
            if (value.startsWith('994')) value = '+' + value;
            else if (value.startsWith('0')) value = '+994' + value.substring(1);
            else if (value.length > 0) value = '+994' + value;
            e.target.value = value;
        });
    }

    // 3. Departamentləri yüklə (Əgər dropdown boşdursa)
    if (depSelect && vezSelect) {
        depSelect.addEventListener('change', function () {
            const depId = this.value;

            vezSelect.innerHTML = '<option value="">Vəzifə seçin</option>';
            vezSelect.disabled = true;

            if (depId) {
                fetch(`/HR/Isci/GetVezifeler?departamentId=${depId}`)
                    .then(res => res.json())
                    .then(data => {
                        data.forEach(v => {
                            // Həm 'id', həm 'Id' ehtimalını yoxlayırıq
                            const optionValue = v.id || v.Id || v.ID;
                            const optionText = v.ad || v.Ad;

                            if (optionValue !== undefined) {
                                const option = new Option(optionText, optionValue);
                                vezSelect.add(option);
                            }
                        });
                        vezSelect.disabled = false;
                    })
                    .catch(err => console.error("Vəzifələr yüklənmədi:", err));
            }
        });
    }

    // 5. Form validasiyası
    form.addEventListener('submit', function (e) {
        if (!form.checkValidity()) {
            e.preventDefault();
            const firstError = form.querySelector(':invalid');
            if (firstError) firstError.focus();
        }
    });
});
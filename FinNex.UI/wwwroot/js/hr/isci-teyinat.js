// Teyinat Deyişikliyi - Departament seçildikdə vəzifələri yüklə
document.addEventListener('DOMContentLoaded', function () {
    var deptSelect = document.getElementById('departamentSec');
    var vezifeSelect = document.getElementById('vezifeSec');

    if (!deptSelect || !vezifeSelect) return;

    function loadVezifeler(deptId, selectedId) {
        vezifeSelect.innerHTML = '<option value="">Yüklənir...</option>';
        vezifeSelect.disabled = true;

        if (!deptId) {
            vezifeSelect.innerHTML = '<option value="">Əvvəl departament seçin</option>';
            vezifeSelect.disabled = false;
            return;
        }

        fetch(getVezifelerUrl + '?departamentId=' + deptId)
            .then(function (r) {
                if (!r.ok) throw new Error('Server xətası: ' + r.status);
                return r.json();
            })
            .then(function (data) {
                vezifeSelect.innerHTML = '<option value="">Vəzifə seçin</option>';
                if (data && data.length > 0) {
                    data.forEach(function (v) {
                        var opt = document.createElement('option');
                        opt.value = v.id;
                        opt.textContent = v.ad;
                        if (selectedId && v.id == selectedId) opt.selected = true;
                        vezifeSelect.appendChild(opt);
                    });
                }
                vezifeSelect.disabled = false;
            })
            .catch(function (err) {
                console.error('Vəzifə yüklənmə xətası:', err);
                vezifeSelect.innerHTML = '<option value="">Xəta baş verdi</option>';
                vezifeSelect.disabled = false;
            });
    }

    deptSelect.addEventListener('change', function () {
        loadVezifeler(this.value, null);
    });

    // İlkin yüklənmə
    if (typeof initialDeptId !== 'undefined' && initialDeptId > 0) {
        loadVezifeler(initialDeptId, null);
    }
});

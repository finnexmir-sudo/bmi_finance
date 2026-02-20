document.addEventListener('DOMContentLoaded', function () {
    const monthFilter = document.getElementById('monthFilter');
    const yearFilter = document.getElementById('yearFilter');

    // Filter eventləri
    if (monthFilter) {
        monthFilter.addEventListener('change', filterTable);
    }

    if (yearFilter) {
        yearFilter.addEventListener('change', filterTable);
    }

    function filterTable() {
        const month = monthFilter.value;
        const year = yearFilter.value;
        const rows = document.querySelectorAll('.data-table tbody tr');

        rows.forEach(row => {
            if (row.querySelector('.empty-state')) return;

            const periodBadge = row.querySelector('.badge-period');
            if (!periodBadge) return;

            const periodText = periodBadge.textContent.trim();
            let show = true;

            if (month) {
                const monthNames = ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'İyun',
                    'İyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'];
                const monthName = monthNames[parseInt(month) - 1];
                show = show && periodText.includes(monthName);
            }

            if (year) {
                show = show && periodText.includes(year);
            }

            row.style.display = show ? '' : 'none';
        });
    }
});
document.addEventListener("click", function (e) {
    const btn = e.target.closest(".btn-delete");
    if (!btn) return;

    const id = btn.dataset.id;
    deleteMaas(id);
});


function deleteMaas(id) {
    if (confirm('Bu maaş qeydini silmək istədiyinizdən əminsiniz?')) {
        fetch(`/HR/Maas/Delete/${id}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    location.reload();
                } else {
                    alert(data.message || 'Xəta baş verdi!');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Xəta baş verdi!');
            });
    }
}
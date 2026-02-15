document.addEventListener('DOMContentLoaded', function () {
    const dateFilter = document.getElementById('dateFilter');
    const statusFilter = document.getElementById('statusFilter');

    // Filter eventləri
    if (dateFilter) {
        dateFilter.addEventListener('change', applyFilters);
    }

    if (statusFilter) {
        statusFilter.addEventListener('change', applyFilters);
    }

    function applyFilters() {
        const selectedDate = dateFilter.value;
        const selectedStatus = statusFilter.value;

        const url = new URL(window.location);
        if (selectedDate) url.searchParams.set('date', selectedDate);
        if (selectedStatus) url.searchParams.set('status', selectedStatus);

        window.location = url.toString();
    }
});

function deleteDavamiyyet(id) {
    if (confirm('Bu davamiyyət qeydini silmək istədiyinizdən əminsiniz?')) {
        fetch(`/HR/Davamiyyet/Delete/${id}`, {
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
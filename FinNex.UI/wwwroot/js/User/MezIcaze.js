const ctx = document.getElementById('statusChart');
if (ctx) {
    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Gözləyən', 'Təsdiqlənib', 'İmtina'],
            datasets: [{
                data: [@Model.Gozleyen, @Model.Tesdiqlenib, @Model.ImtinaEdildi],
                backgroundColor: ['#f59e0b', '#10b981', '#ef4444'],
                borderWidth: 0,
                hoverOffset: 4
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { position: 'right' }
            }
        }
    });
}
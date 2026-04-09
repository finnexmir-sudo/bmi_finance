/* =============================================
   FinNex — HR Analytics JS
   ============================================= */

(function () {
    'use strict';

    if (typeof analyticsDataUrl === 'undefined') return;

    var loadingEl = document.getElementById('anlLoading');
    var kpiGrid = document.getElementById('anlKpiGrid');
    var chartsEl = document.getElementById('anlCharts');
    var chartsFullEl = document.getElementById('anlChartsFull');

    fetch(analyticsDataUrl)
        .then(function (r) { return r.json(); })
        .then(function (data) {
            loadingEl.style.display = 'none';
            renderKPIs(data);
            renderCharts(data);
            kpiGrid.style.display = '';
            chartsEl.style.display = '';
            chartsFullEl.style.display = '';
        })
        .catch(function () {
            loadingEl.innerHTML = '<div style="color:#dc2626;">Məlumat yüklənə bilmədi</div>';
        });

    function renderKPIs(data) {
        document.getElementById('kpiAktiv').textContent = data.aktivIsciSayi;
        document.getElementById('kpiPassiv').textContent = data.passivIsciSayi + ' passiv';

        document.getElementById('kpiGender').textContent = data.kisiSayi + ' / ' + data.qadinSayi;
        var total = data.kisiSayi + data.qadinSayi;
        var pct = total > 0 ? Math.round(data.qadinSayi / total * 100) : 0;
        document.getElementById('kpiGenderPct').textContent = pct + '% qad\u0131n';

        document.getElementById('kpiOrtaMuddet').textContent = data.ortaIsMuddeti;

        document.getElementById('kpiDonusum').textContent = data.donusumFaizi + '%';
        document.getElementById('kpiCixanlar').textContent =
            'Son 12 ayda ' + data.sonOnIkiAydaCixanlar + ' \u00e7\u0131x\u0131\u015f';
    }

    function renderCharts(data) {
        // Departament chart
        var deptLabels = data.departamentStat.map(function (d) { return d.ad; });
        var deptValues = data.departamentStat.map(function (d) { return d.sayi; });

        var deptColors = [
            '#2d6cdf', '#7c3aed', '#1e9b6b', '#d88a30', '#dc2626',
            '#0891b2', '#4f46e5', '#059669', '#d97706', '#e11d48',
            '#6366f1', '#14b8a6'
        ];

        new Chart(document.getElementById('chartDepartament'), {
            type: 'bar',
            data: {
                labels: deptLabels,
                datasets: [{
                    label: '\u0130\u015f\u00e7i say\u0131',
                    data: deptValues,
                    backgroundColor: deptColors.slice(0, deptLabels.length),
                    borderRadius: 6,
                    barThickness: 32
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1,
                            font: { size: 11 },
                            color: '#8a93a8'
                        },
                        grid: { color: '#f0f2f7' }
                    },
                    x: {
                        ticks: {
                            font: { size: 10 },
                            color: '#6b7385'
                        },
                        grid: { display: false }
                    }
                }
            }
        });

        // Gender chart
        new Chart(document.getElementById('chartGender'), {
            type: 'doughnut',
            data: {
                labels: ['Ki\u015fi', 'Qad\u0131n'],
                datasets: [{
                    data: [data.kisiSayi, data.qadinSayi],
                    backgroundColor: ['#2d6cdf', '#e8b84b'],
                    borderWidth: 0,
                    hoverOffset: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '65%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 16,
                            usePointStyle: true,
                            pointStyleWidth: 12,
                            font: { size: 12, weight: '600' },
                            color: '#3d4557'
                        }
                    }
                }
            }
        });

        // Trend chart
        var trendLabels = data.aylikTrend.map(function (t) { return t.ay; });
        var qebulValues = data.aylikTrend.map(function (t) { return t.qebul; });
        var cixisValues = data.aylikTrend.map(function (t) { return t.cixis; });

        new Chart(document.getElementById('chartTrend'), {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: '\u0130\u015f\u0259 q\u0259bul',
                        data: qebulValues,
                        borderColor: '#1e9b6b',
                        backgroundColor: 'rgba(30, 155, 107, 0.1)',
                        fill: true,
                        tension: 0.4,
                        borderWidth: 2,
                        pointRadius: 4,
                        pointBackgroundColor: '#1e9b6b'
                    },
                    {
                        label: '\u0130\u015fd\u0259n \u00e7\u0131x\u0131\u015f',
                        data: cixisValues,
                        borderColor: '#dc2626',
                        backgroundColor: 'rgba(220, 38, 38, 0.1)',
                        fill: true,
                        tension: 0.4,
                        borderWidth: 2,
                        pointRadius: 4,
                        pointBackgroundColor: '#dc2626'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            padding: 16,
                            usePointStyle: true,
                            pointStyleWidth: 12,
                            font: { size: 11, weight: '600' },
                            color: '#3d4557'
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1,
                            font: { size: 11 },
                            color: '#8a93a8'
                        },
                        grid: { color: '#f0f2f7' }
                    },
                    x: {
                        ticks: {
                            font: { size: 10 },
                            color: '#6b7385',
                            maxRotation: 45
                        },
                        grid: { display: false }
                    }
                }
            }
        });
    }

})();

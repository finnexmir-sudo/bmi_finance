// ── Əmək haqqı tarixçəsi qrafiki ────────────────────────

(function () {
    'use strict';

    var chartWrap = document.getElementById('chartWrap');
    var canvas = document.getElementById('maasChart');
    var summaryCard = document.getElementById('summaryCard');

    function formatMoney(val) {
        return Number(val).toLocaleString('az-AZ', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    fetch('/User/Maas/GetTarixceData')
        .then(function (r) { return r.json(); })
        .then(function (result) {
            if (!result.success || !result.data || result.data.length === 0) {
                chartWrap.innerHTML =
                    '<div class="mt-no-data"><i class="bi bi-bar-chart-line"></i>' +
                    '<p>Maaş məlumatı tapılmadı.</p></div>';
                return;
            }

            var labels = result.data.map(function (d) { return d.etiket; });
            var brutData = result.data.map(function (d) { return d.brut; });
            var netData = result.data.map(function (d) { return d.net; });

            // Tək ay data varsa — qrafik əvəzinə kart göstər
            if (result.data.length === 1) {
                var d = result.data[0];
                chartWrap.innerHTML =
                    '<div class="mt-single">' +
                        '<div class="mt-single-label">' + d.etiket + '</div>' +
                        '<div class="mt-single-grid">' +
                            '<div class="mt-single-item">' +
                                '<div class="mt-single-name">Gross Maaş</div>' +
                                '<div class="mt-single-val mt-single-val--blue">' + formatMoney(d.brut) + ' ₼</div>' +
                            '</div>' +
                            '<div class="mt-single-item">' +
                                '<div class="mt-single-name">Net Maaş (əldə edilən)</div>' +
                                '<div class="mt-single-val mt-single-val--green">' + formatMoney(d.net) + ' ₼</div>' +
                            '</div>' +
                            '<div class="mt-single-item">' +
                                '<div class="mt-single-name">Tutulmalar</div>' +
                                '<div class="mt-single-val mt-single-val--red">' + formatMoney(d.brut - d.net) + ' ₼</div>' +
                            '</div>' +
                        '</div>' +
                        '<div class="mt-single-hint">' +
                            '<i class="bi bi-info-circle"></i> ' +
                            'Növbəti aylarda daha çox məlumat toplandıqca burada dinamika qrafiki görünəcək.' +
                        '</div>' +
                    '</div>';
            } else {
                // 2+ ay → normal qrafik
                chartWrap.style.display = 'none';
                canvas.style.display = 'block';

                var ctx = canvas.getContext('2d');
                new Chart(ctx, {
                    type: 'bar',
                    data: {
                        labels: labels,
                        datasets: [
                            {
                                label: 'Gross Maaş (AZN)',
                                data: brutData,
                                backgroundColor: 'rgba(102, 126, 234, 0.6)',
                                borderColor: '#667eea',
                                borderWidth: 1,
                                borderRadius: 4,
                                order: 2
                            },
                            {
                                label: 'Net Maaş (AZN)',
                                data: netData,
                                type: 'line',
                                borderColor: '#16a34a',
                                backgroundColor: 'rgba(22, 163, 106, 0.1)',
                                borderWidth: 2,
                                pointBackgroundColor: '#16a34a',
                                pointRadius: 4,
                                pointHoverRadius: 6,
                                fill: true,
                                tension: 0.3,
                                order: 1
                            }
                        ]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        interaction: { mode: 'index', intersect: false },
                        plugins: {
                            legend: {
                                position: 'top',
                                labels: {
                                    font: { family: "'Plus Jakarta Sans', sans-serif", size: 12 },
                                    usePointStyle: true,
                                    padding: 20
                                }
                            },
                            tooltip: {
                                callbacks: {
                                    label: function (context) {
                                        return context.dataset.label + ': ' +
                                            formatMoney(context.raw) + ' AZN';
                                    }
                                }
                            }
                        },
                        scales: {
                            y: {
                                beginAtZero: true,
                                ticks: {
                                    callback: function (value) {
                                        return value.toLocaleString('az-AZ') + ' ₼';
                                    },
                                    font: { size: 11 }
                                },
                                grid: { color: 'rgba(0,0,0,0.05)' }
                            },
                            x: {
                                ticks: { font: { size: 11 } },
                                grid: { display: false }
                            }
                        }
                    }
                });
            }

            // İcmal statistika (həmişə göstər — 1 ay olsa belə cari rəqəmlər)
            var netArr = netData.filter(function (v) { return v > 0; });
            var brutArr = brutData.filter(function (v) { return v > 0; });

            if (netArr.length > 0) {
                var ortaBrut = brutArr.reduce(function (a, b) { return a + b; }, 0) / brutArr.length;
                var ortaNet = netArr.reduce(function (a, b) { return a + b; }, 0) / netArr.length;
                var enYuksek = Math.max.apply(null, netArr);
                var enAsagi = Math.min.apply(null, netArr);

                document.getElementById('ortaBrut').textContent = formatMoney(ortaBrut) + ' ₼';
                document.getElementById('ortaNet').textContent = formatMoney(ortaNet) + ' ₼';
                document.getElementById('enYuksekNet').textContent = formatMoney(enYuksek) + ' ₼';
                document.getElementById('enAsagiNet').textContent = formatMoney(enAsagi) + ' ₼';
                summaryCard.style.display = 'block';
            }
        })
        .catch(function () {
            chartWrap.innerHTML =
                '<div class="mt-no-data"><i class="bi bi-exclamation-triangle"></i>' +
                '<p>Məlumat yüklənərkən xəta baş verdi.</p></div>';
        });

})();

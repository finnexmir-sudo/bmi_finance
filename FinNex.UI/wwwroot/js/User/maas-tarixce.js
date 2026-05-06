// ── Əmək haqqı tarixçəsi qrafiki ────────────────────────

(function () {
    'use strict';

    var chartWrap = document.getElementById('chartWrap');
    var canvas = document.getElementById('maasChart');
    var summaryCard = document.getElementById('summaryCard');
    var detayPanel = document.getElementById('detayPanel');

    function formatMoney(val) {
        return Number(val).toLocaleString('az-AZ', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    // ── Detay panelini aç ─────────────────────────────────
    function showDetay(il, ay, etiket) {
        detayPanel.innerHTML =
            '<div class="mt-detay-loading"><i class="bi bi-arrow-repeat"></i> Yüklənir...</div>';
        detayPanel.style.display = 'block';
        detayPanel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

        fetch('/User/Maas/GetDetay?il=' + il + '&ay=' + ay)
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (!res.success) {
                    detayPanel.innerHTML = '<div class="mt-detay-err"><i class="bi bi-exclamation-triangle"></i> Məlumat tapılmadı.</div>';
                    return;
                }

                var gelirlər = (res.addimlar || []).filter(function (x) { return x.tip === 'gelir'; });
                var kesintilər = (res.addimlar || []).filter(function (x) { return x.tip === 'kesinti' || x.tip === 'vergi'; });
                var sirket = (res.addimlar || []).filter(function (x) { return x.tip === 'sirket'; });
                var melumat = (res.addimlar || []).filter(function (x) { return x.tip === 'melumati'; });

                function rows(arr, sign, colorClass) {
                    return arr.map(function (x) {
                        return '<div class="mt-detay-row">' +
                            '<div class="mt-detay-row-left">' +
                                '<span class="mt-detay-row-name">' + x.addim + '</span>' +
                                (x.izah ? '<span class="mt-detay-row-izah">' + x.izah + '</span>' : '') +
                            '</div>' +
                            '<span class="mt-detay-row-val ' + colorClass + '">' + sign + formatMoney(x.mebleg) + ' ₼</span>' +
                        '</div>';
                    }).join('');
                }

                function section(title, icon, arr, sign, colorClass, bgClass) {
                    if (arr.length === 0) return '';
                    var total = arr.reduce(function (s, x) { return s + x.mebleg; }, 0);
                    return '<div class="mt-detay-section ' + bgClass + '">' +
                        '<div class="mt-detay-section-head">' +
                            '<span><i class="bi ' + icon + '"></i> ' + title + '</span>' +
                            '<span class="mt-detay-section-total ' + colorClass + '">' + sign + formatMoney(total) + ' ₼</span>' +
                        '</div>' +
                        rows(arr, sign, colorClass) +
                    '</div>';
                }

                var tutulma = res.brut - res.net;
                var html =
                    '<div class="mt-detay-header">' +
                        '<div class="mt-detay-title">' +
                            '<i class="bi bi-receipt"></i> ' + etiket + ' — Hesablama detalı' +
                        '</div>' +
                        '<button class="mt-detay-close" id="detayClose"><i class="bi bi-x-lg"></i></button>' +
                    '</div>' +

                    section('Gəlirlər', 'bi-plus-circle-fill', gelirlər, '+', 'mt-val-green', 'mt-section-green') +
                    section('Tutulmalar (işçidən)', 'bi-dash-circle-fill', kesintilər, '−', 'mt-val-red', 'mt-section-red') +
                    section('İşəgötürən xərcləri', 'bi-building', sirket, '', 'mt-val-gray', 'mt-section-gray') +
                    section('Məlumat üçün', 'bi-info-circle', melumat, '', 'mt-val-blue', 'mt-section-blue') +

                    '<div class="mt-detay-summary">' +
                        '<div class="mt-detay-sum-row">' +
                            '<span>Gross (Brüt) maaş</span>' +
                            '<span class="mt-val-blue">' + formatMoney(res.brut) + ' ₼</span>' +
                        '</div>' +
                        '<div class="mt-detay-sum-row">' +
                            '<span>Tutulmalar</span>' +
                            '<span class="mt-val-red">− ' + formatMoney(tutulma) + ' ₼</span>' +
                        '</div>' +
                        '<div class="mt-detay-sum-row mt-detay-sum-row--net">' +
                            '<span>Net (ələ keçən)</span>' +
                            '<span class="mt-val-green">' + formatMoney(res.net) + ' ₼</span>' +
                        '</div>' +
                    '</div>';

                detayPanel.innerHTML = html;
                document.getElementById('detayClose').addEventListener('click', function () {
                    detayPanel.style.display = 'none';
                });
            })
            .catch(function () {
                detayPanel.innerHTML = '<div class="mt-detay-err"><i class="bi bi-exclamation-triangle"></i> Xəta baş verdi.</div>';
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
                        '<button class="mt-detay-btn" data-il="' + d.il + '" data-ay="' + d.ay + '" data-etiket="' + d.etiket + '">' +
                            '<i class="bi bi-receipt"></i> Hesablama detalını gör' +
                        '</button>' +
                        '<div class="mt-single-hint">' +
                            '<i class="bi bi-info-circle"></i> ' +
                            'Növbəti aylarda daha çox məlumat toplandıqca burada dinamika qrafiki görünəcək.' +
                        '</div>' +
                    '</div>';

                document.querySelector('.mt-detay-btn').addEventListener('click', function (e) {
                    var btn = e.currentTarget;
                    showDetay(parseInt(btn.dataset.il), parseInt(btn.dataset.ay), btn.dataset.etiket);
                });

            } else {
                // 2+ ay → normal qrafik
                chartWrap.style.display = 'none';
                canvas.style.display = 'block';

                var ctx = canvas.getContext('2d');
                var chart = new Chart(ctx, {
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
                        onClick: function (evt, elements) {
                            if (!elements || elements.length === 0) return;
                            var idx = elements[0].index;
                            var d = result.data[idx];
                            showDetay(d.il, d.ay, d.etiket);
                        },
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
                                    },
                                    footer: function () {
                                        return 'Detalı görmək üçün klikləyin';
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
                canvas.style.cursor = 'pointer';
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

// wwwroot/js/user/takvim.js
// Read-only qeyri iş günləri təqvimi — bütün işçilər üçün.
// /HR/BayramGunu JS-nin yüngülləşdirilmiş, toggle-siz variantı.

(function () {
    'use strict';

    const MONTHS_AZ = [
        'Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'İyun',
        'İyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'
    ];
    const WEEK_HEADERS_AZ = ['B.e', 'Ç.a', 'Ç', 'C.a', 'C', 'Ş', 'B'];

    let currentYear = new Date().getFullYear();
    let yearData = null;

    document.addEventListener('DOMContentLoaded', function () {
        document.getElementById('tvYearPrev').addEventListener('click', () => {
            currentYear--;
            refresh();
        });
        document.getElementById('tvYearNext').addEventListener('click', () => {
            currentYear++;
            refresh();
        });
        document.getElementById('tvYearToday').addEventListener('click', () => {
            currentYear = new Date().getFullYear();
            refresh();
        });

        refresh();
    });

    function refresh() {
        document.getElementById('tvCalendarYear').textContent = currentYear;
        const grid = document.getElementById('tvCalendarGrid');
        grid.innerHTML = '<div class="bg-calendar-loading">Yüklənir...</div>';

        fetch(`/User/Takvim/Year?year=${currentYear}`)
            .then(r => r.json())
            .then(data => {
                if (!data || !data.success) {
                    grid.innerHTML = '<div class="bg-calendar-loading">Xəta baş verdi.</div>';
                    return;
                }
                yearData = data;
                render();
            })
            .catch(() => {
                grid.innerHTML = '<div class="bg-calendar-loading">Xəta baş verdi.</div>';
            });
    }

    function render() {
        const grid = document.getElementById('tvCalendarGrid');
        grid.innerHTML = '';

        const recordsByDate = {};
        yearData.records.forEach(r => { recordsByDate[r.tarix] = r; });

        const today = yearData.today;

        let totalWorkDays = 0;
        let totalHolidays = 0;
        let upcomingHolidays = 0;

        for (let m = 0; m < 12; m++) {
            const monthEl = document.createElement('div');
            monthEl.className = 'bg-month';

            const title = document.createElement('div');
            title.className = 'bg-month-title';
            title.textContent = MONTHS_AZ[m];
            monthEl.appendChild(title);

            const header = document.createElement('div');
            header.className = 'bg-month-header';
            WEEK_HEADERS_AZ.forEach((wd, i) => {
                const el = document.createElement('div');
                el.textContent = wd;
                if (i >= 5) el.classList.add('bg-weekend-hdr');
                header.appendChild(el);
            });
            monthEl.appendChild(header);

            const days = document.createElement('div');
            days.className = 'bg-month-days';

            const firstDay = new Date(currentYear, m, 1);
            let startOffset = firstDay.getDay() - 1;
            if (startOffset < 0) startOffset = 6;

            const daysInMonth = new Date(currentYear, m + 1, 0).getDate();

            for (let i = 0; i < startOffset; i++) {
                const cell = document.createElement('div');
                cell.className = 'bg-day bg-day--other';
                days.appendChild(cell);
            }

            let monthWorkDays = 0;

            for (let d = 1; d <= daysInMonth; d++) {
                const cell = document.createElement('div');
                cell.className = 'bg-day bg-day--readonly';
                cell.textContent = d;

                const dateStr = `${currentYear}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;

                const dateObj = new Date(currentYear, m, d);
                const dow = dateObj.getDay();
                const isWeekend = (dow === 0 || dow === 6);

                const record = recordsByDate[dateStr];
                let isWorkDay = !isWeekend;

                if (record) {
                    if (record.tip === 2) {
                        cell.classList.add('bg-day--work-override');
                        cell.title = `${record.ad} (İş günü)`;
                        isWorkDay = true;
                    } else {
                        cell.classList.add('bg-day--holiday');
                        cell.title = `${record.ad} (Qeyri iş günü)`;
                        isWorkDay = false;
                        totalHolidays++;
                        if (dateStr >= today) upcomingHolidays++;
                    }
                } else if (isWeekend) {
                    cell.classList.add('bg-day--weekend');
                }

                if (isWorkDay) monthWorkDays++;

                if (dateStr < today) {
                    cell.classList.add('bg-day--past');
                } else if (dateStr === today) {
                    cell.classList.add('bg-day--today');
                }

                days.appendChild(cell);
            }

            monthEl.appendChild(days);

            const footer = document.createElement('div');
            footer.className = 'bg-month-footer';
            footer.innerHTML = `<i class="bi bi-briefcase"></i> <strong>${monthWorkDays}</strong> iş günü`;
            monthEl.appendChild(footer);

            totalWorkDays += monthWorkDays;
            grid.appendChild(monthEl);
        }

        document.getElementById('statIsGunu').textContent = totalWorkDays;
        document.getElementById('statCemi').textContent = totalHolidays;
        document.getElementById('statGelecek').textContent = upcomingHolidays;
    }
})();

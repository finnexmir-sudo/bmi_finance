// ── HR Budce JS ──────────────────────────────────────────

(function () {
    'use strict';

    const ayAdlari = ['Yan', 'Fev', 'Mar', 'Apr', 'May', 'Iyn', 'Iyl', 'Avq', 'Sen', 'Okt', 'Noy', 'Dek'];
    let cachedData = null;
    let currentIl = null;

    // ── Helpers ──────────────────────────────────────────
    function fmt(val) {
        return Number(val).toLocaleString('az-AZ', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    // ── Load Data ───────────────────────────────────────
    function loadData() {
        const il = document.getElementById('budceIl').value;
        currentIl = il;
        const body = document.getElementById('budceBody');
        body.innerHTML = '<tr><td colspan="27" class="budce-empty">Yuklenir...</td></tr>';

        fetch('/HR/Budce/GetData?il=' + il)
            .then(r => r.json())
            .then(data => {
                cachedData = data;
                renderTable(data);
                renderSummary(data);
            })
            .catch(() => {
                body.innerHTML = '<tr><td colspan="27" class="budce-empty">Xeta bash verdi</td></tr>';
            });
    }

    // ── Render Table ────────────────────────────────────
    function renderTable(data) {
        const body = document.getElementById('budceBody');

        if (!data.departamentlar || data.departamentlar.length === 0) {
            body.innerHTML = '<tr><td colspan="27" class="budce-empty">Melumat tapilmadi</td></tr>';
            return;
        }

        let html = '';

        data.departamentlar.forEach(dept => {
            html += '<tr>';
            html += '<td>' + dept.departamentAd + '</td>';

            dept.aylar.forEach(a => {
                var planCls = a.plan === 0 ? 'budce-cell--zero' : 'budce-cell--plan';
                var faktCls = a.faktiki === 0 ? 'budce-cell--zero' :
                    (a.faktiki > a.plan && a.plan > 0) ? 'budce-cell--over' : 'budce-cell--fakt';

                html += '<td class="' + planCls + '" onclick="openEdit(' + dept.departamentId + ',\'' + dept.departamentAd + '\',' + a.ay + ',' + a.plan + ',' + a.faktiki + ')">' + fmt(a.plan) + '</td>';
                html += '<td class="' + faktCls + '" onclick="openEdit(' + dept.departamentId + ',\'' + dept.departamentAd + '\',' + a.ay + ',' + a.plan + ',' + a.faktiki + ')">' + fmt(a.faktiki) + '</td>';
            });

            var topCls = dept.toplamFaktiki > dept.toplamPlan && dept.toplamPlan > 0 ? 'budce-cell--over' : '';
            html += '<td class="budce-cell--total">' + fmt(dept.toplamPlan) + '</td>';
            html += '<td class="budce-cell--total ' + topCls + '">' + fmt(dept.toplamFaktiki) + '</td>';
            html += '</tr>';
        });

        // Summary row
        html += '<tr class="budce-summary-row">';
        html += '<td>CEMI</td>';
        for (var i = 0; i < 24; i++) html += '<td></td>';
        html += '<td>' + fmt(data.umumiPlan) + '</td>';
        html += '<td>' + fmt(data.umumiFaktiki) + '</td>';
        html += '</tr>';

        body.innerHTML = html;
    }

    // ── Render Summary Cards ────────────────────────────
    function renderSummary(data) {
        var cards = document.getElementById('summaryCards');
        cards.style.display = 'grid';

        document.getElementById('umumiPlan').textContent = fmt(data.umumiPlan) + ' AZN';
        document.getElementById('umumiFaktiki').textContent = fmt(data.umumiFaktiki) + ' AZN';

        var ferq = data.umumiPlan - data.umumiFaktiki;
        var ferqEl = document.getElementById('umumiFerq');
        ferqEl.textContent = fmt(ferq) + ' AZN';
        ferqEl.style.color = ferq >= 0 ? '#16a34a' : '#dc2626';
    }

    // ── Edit Modal ──────────────────────────────────────
    window.openEdit = function (deptId, deptAd, ay, plan, faktiki) {
        document.getElementById('editDeptId').value = deptId;
        document.getElementById('editAy').value = ay;
        document.getElementById('editDeptAd').value = deptAd;
        document.getElementById('editAyAd').value = ayAdlari[ay - 1];
        document.getElementById('editPlan').value = plan;
        document.getElementById('editFaktiki').value = faktiki;
        document.getElementById('editQeyd').value = '';
        document.getElementById('editModal').style.display = 'flex';
    };

    function closeModal() {
        document.getElementById('editModal').style.display = 'none';
    }

    function saveModal() {
        var payload = {
            departamentId: parseInt(document.getElementById('editDeptId').value),
            il: parseInt(currentIl),
            ay: parseInt(document.getElementById('editAy').value),
            planMebleg: parseFloat(document.getElementById('editPlan').value) || 0,
            faktikiMebleg: parseFloat(document.getElementById('editFaktiki').value) || 0,
            qeyd: document.getElementById('editQeyd').value
        };

        fetch('/HR/Budce/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
            .then(r => {
                if (!r.ok) throw new Error('Xeta');
                return r.json();
            })
            .then(() => {
                closeModal();
                loadData();
            })
            .catch(() => {
                alert('Yadda saxlama zamani xeta bash verdi.');
            });
    }

    // ── Excel Export ────────────────────────────────────
    function exportExcel() {
        var il = document.getElementById('budceIl').value;
        window.location.href = '/HR/Budce/ExportExcel?il=' + il;
    }

    // ── Event Listeners ─────────────────────────────────
    document.getElementById('btnYukle').addEventListener('click', loadData);
    document.getElementById('btnExcel').addEventListener('click', exportExcel);
    document.getElementById('btnModalClose').addEventListener('click', closeModal);
    document.getElementById('btnModalCancel').addEventListener('click', closeModal);
    document.getElementById('btnModalSave').addEventListener('click', saveModal);

    document.getElementById('editModal').addEventListener('click', function (e) {
        if (e.target === this) closeModal();
    });

    // Initial load
    loadData();

})();

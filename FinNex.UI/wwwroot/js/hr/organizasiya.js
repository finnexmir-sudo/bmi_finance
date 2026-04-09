/* =============================================
   FinNex — Organizasiya Sxemi JS
   ============================================= */

(function () {
    'use strict';

    if (typeof orgDataUrl === 'undefined') return;

    var loadingEl = document.getElementById('orgLoading');
    var rehberlerEl = document.getElementById('orgRehberler');
    var chartEl = document.getElementById('orgChart');

    fetch(orgDataUrl)
        .then(function (r) { return r.json(); })
        .then(function (data) {
            loadingEl.style.display = 'none';
            renderRehberler(data.rehberler);
            renderDepartamentler(data.departamentler);
            rehberlerEl.style.display = '';
            chartEl.style.display = '';
        })
        .catch(function () {
            loadingEl.innerHTML = '<div style="color:#dc2626;">Məlumat yüklənə bilmədi</div>';
        });

    function renderRehberler(rehberler) {
        if (!rehberler || rehberler.length === 0) {
            rehberlerEl.style.display = 'none';
            return;
        }

        var html = '';
        rehberler.forEach(function (r) {
            var initials = getInitials(r.ad);
            html += '<div class="org-rehber-card">';
            html += '  <div class="org-rehber-avatar">' + initials + '</div>';
            html += '  <div class="org-rehber-info">';
            html += '    <div class="org-rehber-name">' + escHtml(r.ad) + '</div>';
            html += '    <div class="org-rehber-role">R\u0259hb\u0259r</div>';
            html += '  </div>';
            html += '</div>';
        });
        rehberlerEl.innerHTML = html;
    }

    function renderDepartamentler(departamentler) {
        if (!departamentler || departamentler.length === 0) {
            chartEl.innerHTML = '<div style="padding:40px;text-align:center;color:#aab0bf;">Departament tapılmadı</div>';
            return;
        }

        var html = '';
        departamentler.forEach(function (dept) {
            html += '<div class="org-dept-card" data-dept-id="' + dept.id + '">';

            // Header
            html += '<div class="org-dept-header" onclick="toggleDept(this)">';
            html += '  <div class="org-dept-header-left">';
            html += '    <div class="org-dept-icon">';
            html += '      <svg width="16" height="16" viewBox="0 0 16 16" fill="none"><rect x="2" y="3" width="12" height="10" rx="2" stroke="currentColor" stroke-width="1.2"/><path d="M2 6h12" stroke="currentColor" stroke-width="1.1"/></svg>';
            html += '    </div>';
            html += '    <div>';
            html += '      <div class="org-dept-name">' + escHtml(dept.ad) + '</div>';
            html += '      <div class="org-dept-count">' + dept.isciSayi + ' i\u015f\u00e7i</div>';
            html += '    </div>';
            html += '  </div>';
            html += '  <div class="org-dept-toggle">';
            html += '    <svg width="10" height="10" viewBox="0 0 10 10" fill="none"><polyline points="3,2 7,5 3,8" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>';
            html += '  </div>';
            html += '</div>';

            // Body
            html += '<div class="org-dept-body">';

            // Sobe Reisi
            if (dept.sobeReisi) {
                var reisiInitials = getInitials(dept.sobeReisi);
                html += '<div class="org-reisi">';
                html += '  <div class="org-reisi-avatar">' + reisiInitials + '</div>';
                html += '  <div class="org-reisi-info">';
                html += '    <div class="org-reisi-name">' + escHtml(dept.sobeReisi) + '</div>';
                html += '    <div class="org-reisi-role">\u015e\u00f6b\u0259 R\u0259isi</div>';
                html += '  </div>';
                html += '</div>';
            }

            // Employees
            if (dept.isciler && dept.isciler.length > 0) {
                html += '<div class="org-employees">';
                dept.isciler.forEach(function (isci) {
                    if (isci.isSobeReisi) return; // Already shown above
                    var empInitials = getInitials(isci.ad);
                    html += '<div class="org-emp">';
                    html += '  <div class="org-emp-avatar">' + empInitials + '</div>';
                    html += '  <div>';
                    html += '    <div class="org-emp-name">' + escHtml(isci.ad) + '</div>';
                    html += '    <div class="org-emp-vezife">' + escHtml(isci.vezife) + '</div>';
                    html += '  </div>';
                    html += '</div>';
                });
                html += '</div>';
            } else if (!dept.sobeReisi) {
                html += '<div class="org-dept-empty">\u0130\u015f\u00e7i yoxdur</div>';
            }

            html += '</div>'; // body
            html += '</div>'; // card
        });

        chartEl.innerHTML = html;
    }

    function getInitials(name) {
        if (!name) return '?';
        var parts = name.trim().split(' ');
        if (parts.length >= 2) return parts[0][0] + parts[1][0];
        return name[0];
    }

    function escHtml(str) {
        if (!str) return '\u2014';
        var d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

    // Global toggle function
    window.toggleDept = function (header) {
        var card = header.closest('.org-dept-card');
        card.classList.toggle('collapsed');
    };

    // Collapse / Expand All
    var collapseBtn = document.getElementById('orgCollapseAll');
    var expandBtn = document.getElementById('orgExpandAll');

    if (collapseBtn) {
        collapseBtn.addEventListener('click', function () {
            document.querySelectorAll('.org-dept-card').forEach(function (c) {
                c.classList.add('collapsed');
            });
        });
    }

    if (expandBtn) {
        expandBtn.addEventListener('click', function () {
            document.querySelectorAll('.org-dept-card').forEach(function (c) {
                c.classList.remove('collapsed');
            });
        });
    }

})();

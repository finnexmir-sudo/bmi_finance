// İşçi Detal - Tab sistemi
document.addEventListener('DOMContentLoaded', function () {
    const tabs = document.querySelectorAll('.detail-tab');
    const panels = document.querySelectorAll('.tab-panel');

    tabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            const target = this.dataset.tab;

            tabs.forEach(function (t) { t.classList.remove('active'); });
            panels.forEach(function (p) { p.classList.remove('active'); });

            this.classList.add('active');
            const panel = document.getElementById('tab-' + target);
            if (panel) panel.classList.add('active');
        });
    });

    // Toast avtomatik gizlət
    var toast = document.getElementById('toastSuccess');
    if (toast) {
        setTimeout(function () {
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(-12px)';
            toast.style.transition = 'opacity .4s, transform .4s';
            setTimeout(function () { toast.remove(); }, 400);
        }, 3500);
    }
});

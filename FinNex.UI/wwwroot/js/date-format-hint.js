// Brauzerin tarix formatını detect edib göstərir
document.addEventListener('DOMContentLoaded', function () {
    var hints = document.querySelectorAll('.date-format-hint');
    if (!hints.length) return;

    // 2026-01-25 tarixini brauzerin locale-ına çevirib formatı tapırıq
    var testDate = new Date(2026, 0, 25); // 25 Yanvar 2026
    var formatted = testDate.toLocaleDateString();

    // Formatı müəyyən et
    var format;
    if (formatted.indexOf('25') < formatted.indexOf('1') || formatted.indexOf('25') < formatted.indexOf('01')) {
        format = 'Gün/Ay/İl (DD/MM/YYYY)';
    } else {
        format = 'Ay/Gün/İl (MM/DD/YYYY)';
    }

    hints.forEach(function (el) {
        el.textContent = 'Format: ' + format;
    });
});

/*  Kredit müqaviləsi formaları — BOŞ SAHƏ XƏBƏRDARLIĞI
 *  ---------------------------------------------------------------------------
 *  Göndərməzdən əvvəl boş qalmış vacib sahələri sadalayır və istifadəçidən
 *  təsdiq istəyir. BLOKLAMIR — operator davam edərsə sənəddə həmin yerlər boş
 *  çıxacaq və o, bunu bilərək seçib (14.08.2026 qərarı).
 *
 *  NİYƏ SERVERDƏ YOX, BRAUZERDƏ:
 *  Server tərəfdə etsək forma POST olunardı, POST isə nömrə ayrılan yerdir.
 *  Xəbərdarlıq üçün gedib-qayıtmaq mənasız risk yaradar. Burada isə forma
 *  ümumiyyətlə göndərilmir — heç bir nömrə toxunulmur.
 *
 *  MƏCBURİ sahələr (model, bazar dəyəri) bundan AYRIDIR: onlar həm formada
 *  `required`, həm də serverdə yoxlanılır və nömrədən əvvəl bloklayır.
 *  Bu skript yalnız "boş qala bilər, amma yəqin ki, qalmamalıdır" sahələr üçündür.
 *
 *  İSTİFADƏ: sahəyə  data-vacib="Görünən ad"  atributu qoyun. Dinamik əlavə
 *  olunan sətirlər (yeni zamin) də avtomatik tutulur, çünki siyahı məhz
 *  göndərmə anında oxunur.
 */
(function () {
    'use strict';

    var form = document.querySelector('form[data-bos-yoxlama]');
    if (!form) return;

    // Gizli bloklardakı sahələr sayılmır: hüquqi şəxs seçilməyibsə direktor
    // sahələri, fiziki zaminlərdə VÖEN və s. — onlar sənədə onsuz da düşmür.
    function gorunur(el) {
        return el.offsetParent !== null;
    }

    function bosdur(el) {
        return (el.value || '').trim().length === 0;
    }

    function bosSahelər() {
        var siyahi = [];

        // 1) data-vacib ilə işarələnmiş sahələr
        Array.prototype.forEach.call(form.querySelectorAll('[data-vacib]'), function (el) {
            if (gorunur(el) && bosdur(el)) siyahi.push(el.getAttribute('data-vacib'));
        });

        // 2) Zamin sətirləri — pasport və FİN-in İKİSİ birdən boşdursa.
        //    Ayrı-ayrılıqda işarələmək olmaz: adətən yalnız biri doldurulur və
        //    şablonda «Vəsiqə məlumatı» ikisindən mövcud olanı ilə yazılır.
        Array.prototype.forEach.call(form.querySelectorAll('.zamin-row'), function (row, i) {
            if (!gorunur(row)) return;
            var ad  = row.querySelector('input[name$=".Ad"]');
            var pas = row.querySelector('input[name$=".Pasport"]');
            var fin = row.querySelector('input[name$=".Fin"]');
            if (!ad || bosdur(ad)) return;               // adsız sətir onsuz da atılır
            if (pas && fin && bosdur(pas) && bosdur(fin))
                siyahi.push((i + 1) + '-ci zamin — pasport/FİN');
        });

        return siyahi;
    }

    form.addEventListener('submit', function (e) {
        var bos = bosSahelər();
        if (bos.length === 0) return;

        var metn = 'Aşağıdakı sahələr boşdur və sənəddə boş görünəcək:\n\n'
                 + bos.map(function (s) { return '  •  ' + s; }).join('\n')
                 + '\n\nDavam edilsin?';

        if (!window.confirm(metn)) e.preventDefault();
    });
})();

// Nov seçimi
document.querySelectorAll('.fn-nov-radio').forEach(function (radio) {
    radio.addEventListener('change', function () {
        document.querySelectorAll('.fn-nov-card').forEach(c => c.classList.remove('fn-nov-card--active'));
        this.closest('.fn-nov-card').classList.add('fn-nov-card--active');
    });
});

// Müddət hesabla — YALNIZ təqvim günü.
//
// 18.08.2026-ya qədər burada iş günü də «hesablanırdı»: `Math.round(diff*5/7)`,
// yəni sadəcə həftəsonu təxmini. Eyni `#durationText` elementinə Create.cshtml-dəki
// preview də yazır və ora BACKEND-in DƏQİQ rəqəmi (`data.isGun`) düşür. İki yazıcı,
// sıra zəmanəti yoxdur → hansı sonra işləsə o qalır:
//   • preview sonra gəlsə  → 5 (düzgün)
//   • bu funksiya sonra işləsə, yaxud preview keş qoruyucusuna (`key === lastKey`)
//     ilişib fetch etməsə → 4 (SƏHV)
// Real hadisə: 20–24.08.2026 üçün başlıq gah «~5 iş günü», gah «~4 iş günü»
// göstərirdi, halbuki aşağıdakı «İŞ GÜNÜ» kartı (yalnız backend yazır) həmişə 5 idi.
//
// Üstəlik təxminin özü səhv idi: əmək məzuniyyətində ödənilən günlər TƏQVİM günüdür
// (yalnız bayramlar çıxılır), həftəsonu çıxılmır — ×5/7 burada mənasızdır.
//
// QAYDA: bu funksiya rəqəmi UYDURMUR. Təqvim günü lokal hesablanır (dərhal görünür),
// iş günü isə yalnız backend cavabı gələndə əlavə olunur. Bax: Create.cshtml → refreshPreview.
function hesabla() {
    var bas = document.getElementById('baslamaTarixi').value;
    var bitis = document.getElementById('bitmeTarixi').value;
    var box = document.getElementById('durationBox');
    var txt = document.getElementById('durationText');

    if (!bas || !bitis) { box.style.display = 'none'; return; }

    var d1 = new Date(bas);
    var d2 = new Date(bitis);
    var diff = Math.round((d2 - d1) / (1000 * 60 * 60 * 24)) + 1;

    if (diff <= 0) { box.style.display = 'none'; return; }

    txt.textContent = diff + ' təqvim günü';
    box.style.display = 'flex';
}

document.getElementById('baslamaTarixi').addEventListener('change', hesabla);
document.getElementById('bitmeTarixi').addEventListener('change', hesabla);
hesabla();
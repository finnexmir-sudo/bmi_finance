/**
 * maas.js — Maaş modulu UI məntiq faylı
 * Hesablama formu, məzuniyyət kalkulyatoru, status dəyişikliyi
 */

(function () {
    'use strict';

    // ── Sənəd yüklənəndə ─────────────────────────────────────────
    document.addEventListener('DOMContentLoaded', function () {
        MaasForm.init();
        TopluHesabla.init();
        StatusModal.init();
        FilterForm.init();
        PrintDetal.init();
    });

    // ════════════════════════════════════════════════════════════
    // HESABLAMA FORMU — Məzuniyyət kalkulyatoru
    // ════════════════════════════════════════════════════════════
    const MaasForm = {
        init() {
            const mezGun = document.getElementById('MezuniyyetGunSayi');
            const isciId = document.getElementById('IsciId');
            const il = document.getElementById('Il');
            const ay = document.getElementById('Ay');

            if (!mezGun) return;

            // Məzuniyyət günü dəyişəndə ön izlə
            mezGun.addEventListener('input', () => this.mezuniyyetOnIzle());
            if (isciId) isciId.addEventListener('change', () => this.mezuniyyetOnIzle());

            // Canlı brüt hesablama (müxbir sahə)
            this.initCanliHesab();
        },

        // Serverdən məzuniyyət məbləğini al və göstər
        async mezuniyyetOnIzle() {
            const isciId = document.getElementById('IsciId')?.value;
            const il = document.getElementById('Il')?.value;
            const ay = document.getElementById('Ay')?.value;
            const gunSayi = document.getElementById('MezuniyyetGunSayi')?.value;
            const netice = document.getElementById('mezuniyyet-netice');

            if (!netice) return;

            if (!isciId || !gunSayi || gunSayi <= 0) {
                netice.textContent = '—';
                return;
            }

            try {
                netice.textContent = 'Hesablanır...';
                const url = `/HR/Maas/MezuniyyetOnIzle?isciId=${isciId}&il=${il}&ay=${ay}&gunSayi=${gunSayi}`;
                const resp = await fetch(url);
                const data = await resp.json();
                netice.textContent = data.mebleg + ' AZN';
            } catch {
                netice.textContent = '—';
            }
        },

        // Canlı brüt hesab: əsas maaş + bonus - cərimə
        initCanliHesab() {
            const brutEl = document.getElementById('brut-gosterici');
            if (!brutEl) return;

            const saheler = ['#BonusMeblegi', '#CerimeMeblegi'];

            const hesabla = () => {
                const esasMaas = parseFloat(document.getElementById('esas-maas-data')?.dataset.mebleg || '0');
                const bonus = parseFloat(document.getElementById('BonusMeblegi')?.value || '0');
                const cerime = parseFloat(document.getElementById('CerimeMeblegi')?.value || '0');
                const brut = Math.max(0, esasMaas + bonus - cerime);
                brutEl.textContent = brut.toLocaleString('az-AZ', { minimumFractionDigits: 2 }) + ' AZN';
            };

            saheler.forEach(sel => {
                const el = document.querySelector(sel);
                if (el) el.addEventListener('input', hesabla);
            });

            hesabla();
        }
    };

    // ════════════════════════════════════════════════════════════
    // TOPLU HESABLAMA — Təsdiq modal
    // ════════════════════════════════════════════════════════════
    const TopluHesabla = {
        init() {
            const btn = document.getElementById('btn-toplu-hesabla');
            if (!btn) return;

            btn.addEventListener('click', () => {
                const il = document.getElementById('filter-il')?.value;
                const ay = document.getElementById('filter-ay')?.value;
                const ayAd = this.ayAdi(parseInt(ay));

                const tesdiq = confirm(
                    `${il}/${ay} (${ayAd}) ayı üçün bütün aktiv işçilərin maaşını hesablamaq istəyirsiniz?\n\n` +
                    `Artıq hesablanmış işçilər atlanacaq.`
                );

                if (tesdiq) {
                    document.getElementById('toplu-il').value = il;
                    document.getElementById('toplu-ay').value = ay;
                    document.getElementById('form-toplu-hesabla').submit();
                }
            });
        },

        ayAdi(ay) {
            const adlar = ['', 'Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'İyun',
                'İyul', 'Avqust', 'Sentyabr', 'Oktyabr', 'Noyabr', 'Dekabr'];
            return adlar[ay] || '';
        }
    };

    // ════════════════════════════════════════════════════════════
    // STATUS DƏYİŞİKLİYİ — İnline təsdiq
    // ════════════════════════════════════════════════════════════
    const StatusModal = {
        mesajlar: {
            2: 'Bu maaşı TƏSDİQ etmək istəyirsiniz?\nTəsdiqlənmiş maaşı dəyişmək olmaz.',
            3: 'Bu maaşı ÖDƏNİLDİ kimi işarələmək istəyirsiniz?\nBank köçürməsi aparılmış hesab ediləcək.',
            4: 'Bu maaşı LƏĞV etmək istəyirsiniz?\nLəğv edilmiş maaşı bərpa etmək olmaz.'
        },

        init() {
            document.querySelectorAll('.btn-status-deyis').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const status = parseInt(btn.dataset.status);
                    const mesaj = this.mesajlar[status];

                    if (mesaj && !confirm(mesaj)) {
                        e.preventDefault();
                        return;
                    }

                    btn.closest('form').submit();
                });
            });
        }
    };

    // ════════════════════════════════════════════════════════════
    // FİLTER FORMU — Dəyişiklikdə avtomatik göndər
    // ════════════════════════════════════════════════════════════
    const FilterForm = {
        init() {
            const form = document.getElementById('form-filter');
            if (!form) return;

            form.querySelectorAll('select').forEach(sel => {
                sel.addEventListener('change', () => form.submit());
            });
        }
    };

    // ════════════════════════════════════════════════════════════
    // PRINT — Detal səhifəsi çap
    // ════════════════════════════════════════════════════════════
    const PrintDetal = {
        init() {
            const btn = document.getElementById('btn-print');
            if (!btn) return;

            btn.addEventListener('click', () => window.print());
        }
    };

    // ════════════════════════════════════════════════════════════
    // BANK FAYLI — Yükləmə düyməsi
    // ════════════════════════════════════════════════════════════
    window.bankFayliYukle = function (il, ay) {
        const tesdiq = confirm(`${il}/${ay} ayı üçün bank ödəniş faylını yükləmək istəyirsiniz?\nYalnız TƏSDİQLƏNMİŞ maaşlar daxil ediləcək.`);
        if (tesdiq) {
            window.location.href = `/HR/Maas/BankFayliYukle?il=${il}&ay=${ay}`;
        }
    };

})();

// ════════════════════════════════════════════════════════════
// PARAMETRİ MODAL — Yeni / Düzəlt
// ════════════════════════════════════════════════════════════
const ParametrModal = {
    modal: null,

    init() {
        this.modal = document.getElementById('parametr-modal');
        if (!this.modal) return;

        // Kənar klik ilə bağla
        this.modal.addEventListener('click', (e) => {
            if (e.target === this.modal) this.bağla();
        });
    },

    ac(tip, id, ad, deyer, baslama) {
        if (!this.modal) return;

        if (tip === 'yeni') {
            document.getElementById('modal-bashliq').textContent = 'Yeni Parametr';
            document.getElementById('parametr-form').action = document.getElementById('parametr-form').dataset.yaratUrl;
            document.getElementById('parametr-id').value = 0;
            document.getElementById('parametr-deyer').value = '';
            document.getElementById('parametr-baslama').value = new Date().toISOString().split('T')[0];
            document.getElementById('nov-qrup').style.display = 'block';
        } else {
            document.getElementById('modal-bashliq').textContent = ad + ' — Düzəlt';
            document.getElementById('parametr-form').action = document.getElementById('parametr-form').dataset.yenileUrl;
            document.getElementById('parametr-id').value = id;
            document.getElementById('parametr-deyer').value = deyer;
            document.getElementById('parametr-baslama').value = baslama;
            document.getElementById('nov-qrup').style.display = 'none';
        }

        this.modal.style.display = 'flex';
    },

    bağla() {
        if (this.modal) this.modal.style.display = 'none';
    }
};

// Global funksiyalar — HTML onclick-dən çağrılır
window.yeniParametrModal = () => ParametrModal.ac('yeni');
window.parametrDuzenle = (id, ad, deyer, baslama) => ParametrModal.ac('duzenle', id, ad, deyer, baslama);
window.modalBağla = () => ParametrModal.bağla();

// Init-ə əlavə et
document.addEventListener('DOMContentLoaded', function () {
    ParametrModal.init();
});
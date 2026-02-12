document.addEventListener("DOMContentLoaded", function () {

    // ========================
    // Şöbə dəyişəndə növləri yüklə
    // ========================
    var sobeSelect = document.getElementById('sobeSelect');
    if (sobeSelect) {
        sobeSelect.addEventListener('change', function () {
            var sobeId = this.value;
            var select = document.getElementById('senedNovuSelect');

            if (!sobeId) {
                select.innerHTML = '<option value="">Əvvəlcə şöbə seçin...</option>';
                return;
            }

            select.innerHTML = '<option value="">Yüklənir...</option>';

            fetch('/SenedDovriyyesi/Sened/SenedNovleriByShobe?sobeId=' + sobeId)

                .then(r => r.json())
                .then(data => {
                    select.innerHTML = '<option value="">Sənəd növü seçin...</option>';
                    data.forEach(x => {
                        var opt = document.createElement("option");
                        opt.value = x.id;
                        opt.textContent = x.ad;
                        select.appendChild(opt);
                    });

                    if (data.length === 0) {
                        select.innerHTML = '<option value="">Bu şöbədə növ yoxdur</option>';
                    }
                });
        });
    }

    // ========================
    // Yeni Növ Button Event
    // ========================
    var yeniBtn = document.querySelector('[onclick="openNovModal()"]');
    if (yeniBtn) {
        yeniBtn.addEventListener("click", function (e) {
            e.preventDefault();
            openNovModal();
        });
    }

});
// Sidebar toggle
(function () {
    var sidebar = document.getElementById('sdSidebar');
    var overlay = document.getElementById('sidebarOverlay');
    var toggle = document.getElementById('sidebarToggle');

    if (toggle) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('show');
            overlay.classList.toggle('show');
        });
    }
    if (overlay) {
        overlay.addEventListener('click', function () {
            sidebar.classList.remove('show');
            overlay.classList.remove('show');
        });
    }
})();

var fileInput = document.getElementById('fileInput');

if (fileInput) {
    fileInput.addEventListener('change', function () {
        var preview = document.getElementById('filePreview');
        var uploadBtn = document.getElementById('uploadBtn');

        if (this.files.length > 0) {
            var file = this.files[0];
            var maxSize = 25 * 1024 * 1024;

            if (file.size > maxSize) {
                alert('Fayl ölçüsü 25MB-dan böyük ola bilməz.');
                this.value = '';
                preview.style.display = 'none';
                uploadBtn.disabled = true;
                return;
            }
        }
    });
}


function openNovModal() {
    document.getElementById("novSobe").value = "";
    document.getElementById("novKod").value = "";
    document.getElementById("novAd").value = "";
    document.getElementById("novError").classList.add("d-none");
    document.getElementById("novSuccess").classList.add("d-none");
    new bootstrap.Modal(document.getElementById('novModal')).show();
}

function saveNov() {
    var sobeId = document.getElementById("novSobe").value;
    var kod = document.getElementById("novKod").value;
    var ad = document.getElementById("novAd").value;

    var errEl = document.getElementById("novError");
    var succEl = document.getElementById("novSuccess");
    errEl.classList.add("d-none");
    succEl.classList.add("d-none");

    if (!sobeId) {
        errEl.innerText = "Şöbə seçilməlidir.";
        errEl.classList.remove("d-none");
        return;
    }
    if (!kod.trim() || !ad.trim()) {
        errEl.innerText = "Kod və ad boş ola bilməz.";
        errEl.classList.remove("d-none");
        return;
    }

    var btn = document.getElementById("saveBtn");
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saxlanılır...';

    fetch('@Url.Action("YeniSenedNovu", "Sened", new { area = "SenedDovriyyesi" })', {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "RequestVerificationToken":
                document.querySelector('meta[name="RequestVerificationToken"]')?.content
                || document.querySelector('input[name="__RequestVerificationToken"]')?.value
        },
        body: JSON.stringify({ sobeId: parseInt(sobeId), kod: kod, ad: ad })
    })
        .then(r => r.json())
        .then(data => {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Saxla';

            if (!data.success) {
                errEl.innerText = data.message;
                errEl.classList.remove("d-none");
                return;
            }

            succEl.innerText = "Sənəd növü uğurla əlavə edildi!";
            succEl.classList.remove("d-none");

            setTimeout(function () {
                location.reload();
            }, 800);
        })
        .catch(function () {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Saxla';
            errEl.innerText = "Xəta baş verdi. Yenidən cəhd edin.";
            errEl.classList.remove("d-none");
        });
}

function filterTable() {
    var sobeFilter = document.getElementById("sobeFilter").value;
    var searchVal = document.getElementById("searchInput").value.toLowerCase();
    var rows = document.querySelectorAll("#novTable tbody tr[data-sobe]");
    var visibleCount = 0;

    rows.forEach(function (row) {
        var matchSobe = !sobeFilter || row.dataset.sobe === sobeFilter;
        var matchSearch = !searchVal ||
            row.dataset.kod.includes(searchVal) ||
            row.dataset.ad.includes(searchVal);

        if (matchSobe && matchSearch) {
            row.style.display = "";
            visibleCount++;
        } else {
            row.style.display = "none";
        }
    });
}
(function () {
    "use strict";

    // ── Antiforgery token ────────────────────────────────
    function getAntiForgeryToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    // ── Toast ────────────────────────────────────────────
    function showToast(message, type) {
        var existing = document.querySelector(".up-toast");
        if (existing) existing.remove();

        var toast = document.createElement("div");
        toast.className = "up-toast up-toast-" + type;
        toast.textContent = message;
        document.body.appendChild(toast);

        requestAnimationFrame(function () {
            toast.classList.add("up-toast-show");
        });

        setTimeout(function () {
            toast.classList.remove("up-toast-show");
            setTimeout(function () { toast.remove(); }, 300);
        }, 3000);
    }

    // ── Edit mode toggle ─────────────────────────────────
    var editMode = false;
    var editableFields = ["telefon", "email", "unvan"];

    function enterEditMode() {
        editMode = true;

        editableFields.forEach(function (field) {
            var display = document.querySelector('[data-display="' + field + '"]');
            var input = document.querySelector('[data-input="' + field + '"]');
            var editBtn = document.querySelector('[data-edit-btn="' + field + '"]');

            if (display) display.classList.add("up-hidden");
            if (editBtn) editBtn.classList.add("up-hidden");
            if (input) {
                input.classList.add("up-visible");
                input.value = display ? (display.dataset.value || "") : "";
            }
        });

        var saveBtn = document.getElementById("upSaveBtn");
        if (saveBtn) saveBtn.classList.add("up-visible");
    }

    function exitEditMode() {
        editMode = false;

        editableFields.forEach(function (field) {
            var display = document.querySelector('[data-display="' + field + '"]');
            var input = document.querySelector('[data-input="' + field + '"]');
            var editBtn = document.querySelector('[data-edit-btn="' + field + '"]');

            if (display) display.classList.remove("up-hidden");
            if (editBtn) editBtn.classList.remove("up-hidden");
            if (input) input.classList.remove("up-visible");
        });

        var saveBtn = document.getElementById("upSaveBtn");
        if (saveBtn) saveBtn.classList.remove("up-visible");
    }

    // ── Event delegation ─────────────────────────────────
    document.addEventListener("click", function (e) {
        // Edit button click
        var editBtn = e.target.closest("[data-edit-btn]");
        if (editBtn) {
            e.preventDefault();
            enterEditMode();
            var field = editBtn.getAttribute("data-edit-btn");
            var input = document.querySelector('[data-input="' + field + '"]');
            if (input) input.focus();
            return;
        }

        // Save button click
        if (e.target.closest("#upSaveBtn")) {
            e.preventDefault();
            saveContact();
            return;
        }
    });

    // ── Save contact via AJAX ────────────────────────────
    function saveContact() {
        var saveBtn = document.getElementById("upSaveBtn");
        if (!saveBtn) return;

        saveBtn.disabled = true;
        saveBtn.textContent = "Saxlanılır...";

        var telefonInput = document.querySelector('[data-input="telefon"]');
        var emailInput = document.querySelector('[data-input="email"]');
        var unvanInput = document.querySelector('[data-input="unvan"]');

        var params = new URLSearchParams();
        params.append("telefon", telefonInput ? telefonInput.value : "");
        params.append("email", emailInput ? emailInput.value : "");
        params.append("unvan", unvanInput ? unvanInput.value : "");
        params.append("__RequestVerificationToken", getAntiForgeryToken());

        fetch("/User/Profile/UpdateContact", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            body: params.toString()
        })
            .then(function (response) { return response.json(); })
            .then(function (data) {
                if (data.success) {
                    showToast(data.message, "success");
                    // Update display values
                    updateDisplayValue("telefon", telefonInput ? telefonInput.value : "");
                    updateDisplayValue("email", emailInput ? emailInput.value : "");
                    updateDisplayValue("unvan", unvanInput ? unvanInput.value : "");
                    exitEditMode();
                } else {
                    showToast(data.message || "Xəta baş verdi.", "error");
                }
            })
            .catch(function () {
                showToast("Serverlə əlaqə qurmaq mümkün olmadı.", "error");
            })
            .finally(function () {
                saveBtn.disabled = false;
                saveBtn.textContent = "Yadda saxla";
            });
    }

    function updateDisplayValue(field, value) {
        var display = document.querySelector('[data-display="' + field + '"]');
        if (!display) return;

        display.dataset.value = value;
        if (value && value.trim()) {
            display.textContent = value.trim();
            display.classList.remove("up-empty");
        } else {
            display.textContent = "Daxil edilməyib";
            display.classList.add("up-empty");
        }
    }
})();

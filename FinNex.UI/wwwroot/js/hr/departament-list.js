document.addEventListener("click", function (e) {
    const btn = e.target.closest(".btn-delete");
    if (!btn) return;

    const id = btn.dataset.id;

    if (confirm("Bu departamenti silmək istədiyinizdən əminsiniz?")) {
        fetch(`/HR/Departament/Delete/${id}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken":
                    document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        })
            .then(r => r.json())
            .then(data => {
                if (data.success) {
                    location.reload();
                } else {
                    alert(data.message || "Xəta baş verdi!");
                }
            })
            .catch(() => alert("Xəta baş verdi!"));
    }
});

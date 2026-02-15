document.addEventListener("DOMContentLoaded", function () {

    const parents = document.querySelectorAll(".hr-nav-parent");

    parents.forEach(parent => {
        parent.addEventListener("click", function () {

            const group = parent.closest(".hr-nav-group");

            // İstəsən digər açıq qrupları bağlayaq:
            document.querySelectorAll(".hr-nav-group").forEach(g => {
                if (g !== group) {
                    g.classList.remove("open");
                }
            });

            group.classList.toggle("open");
        });
    });

});

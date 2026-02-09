document.addEventListener("DOMContentLoaded", () => {

    const saveBtn = document.getElementById("saveNewBank");
    if (!saveBtn) return;

    saveBtn.addEventListener("click", () => {

        const bank = {
            name: NewBankName.value,
            code: NewBankCode.value,
            voen: NewBankVoen.value,
            swift: NewBankSwift.value,
            corr: NewBankCorr.value,
            agent: NewBankAgent.value
        };

        console.log("Yeni bank:", bank);

        bootstrap.Modal
            .getInstance(document.getElementById("newBankModal"))
            .hide();
    });
});


document.addEventListener("DOMContentLoaded", function () {
    const message = document.getElementById("saveMessage");
    const tokenInput = document.querySelector( '#antiForgeryForm input[name="__RequestVerificationToken"]'
    );

    document.querySelectorAll(".approval-dropdown").forEach(function (dropdown) {
        dropdown.dataset.previousStatus = dropdown.value;

        dropdown.addEventListener("change", async function () {
            const batchId = this.dataset.batchId;
            const rowId = this.dataset.rowId;
            const approvalStatus = this.value;

            if (!tokenInput) {
                showMessage("Security token not found. Refresh the page and try again.", "danger");
                return;
            }

            const formData = new FormData();
            formData.append("batchId", batchId);
            formData.append("rowId", rowId);
            formData.append("approvalStatus", approvalStatus);
            formData.append("__RequestVerificationToken", tokenInput.value);

            this.disabled = true;
            showMessage("Saving approval status...", "info");

            try {
                const response = await fetch("/UploadFile/UpdateApproval", {
                    method: "POST",
                    body: formData
                });

                const result = await response.json();

                if (!response.ok || !result.success) {
                    throw new Error(result.message || "Unable to save approval status.");
                }

                this.dataset.previousStatus = approvalStatus;
                showMessage(result.message, "success");
            } catch (error) {
                this.value = this.dataset.previousStatus;
                showMessage(error.message || "An error occurred while saving.", "danger");
            } finally {
                this.disabled = false;
            }
        });
    });

    function showMessage(text, type) {
        if (!message) return;

        message.className = `alert alert-${type}`;
        message.textContent = text;
    }
});

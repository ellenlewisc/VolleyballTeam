// Forms with data-confirm="message" ask through the shared modal before submitting.
(function () {
    const modal = document.getElementById('confirm-modal');
    if (!modal) {
        return;
    }

    const title = document.getElementById('confirm-modal-title');
    const message = document.getElementById('confirm-modal-message');
    const accept = modal.querySelector('[data-confirm-accept]');
    let pendingForm = null;

    document.addEventListener('submit', function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.dataset.confirm || form.dataset.confirmed === 'true') {
            return;
        }

        event.preventDefault();
        pendingForm = form;
        title.textContent = form.dataset.confirmTitle || 'Are you sure?';
        message.textContent = form.dataset.confirm;
        accept.textContent = form.dataset.confirmButton || 'Confirm';
        accept.className = form.dataset.confirmDanger === 'true' ? 'btn-danger' : 'btn-primary';
        modal.showModal();
    });

    accept.addEventListener('click', function () {
        if (pendingForm) {
            pendingForm.dataset.confirmed = 'true';
            modal.close();
            pendingForm.requestSubmit();
        }
    });

    modal.querySelector('[data-confirm-cancel]').addEventListener('click', function () {
        pendingForm = null;
        modal.close();
    });
})();

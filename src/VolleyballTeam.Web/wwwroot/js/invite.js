// Team and coaching role only apply when inviting a coach.
(function () {
    const role = document.querySelector('[data-invite-role]');
    const coachFields = document.querySelector('[data-invite-coach-fields]');
    if (!role || !coachFields) {
        return;
    }

    const teamError = coachFields.querySelector('[data-valmsg-for]');

    const sync = function () {
        const isCoach = role.value === 'Coach';
        coachFields.hidden = !isCoach;
        coachFields.querySelectorAll('select').forEach(function (select) {
            select.disabled = !isCoach;
        });
    };

    const clearTeamError = function () {
        if (teamError) {
            teamError.textContent = '';
            teamError.classList.remove('field-validation-error');
            teamError.classList.add('field-validation-valid');
        }
    };

    role.addEventListener('change', function () {
        sync();
        clearTeamError();
    });
    coachFields.addEventListener('change', clearTeamError);
    sync();
})();

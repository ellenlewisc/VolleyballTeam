(() => {
    const form = document.querySelector('[data-event-form]');
    if (!form) {
        return;
    }

    const type = form.querySelector('[data-event-type]');
    const opponent = form.querySelector('[data-event-opponent]');
    const starts = form.querySelector('[data-event-starts]');
    const ends = form.querySelector('[data-event-ends]');

    const showOpponent = () => {
        const isGame = type.value === 'Game';
        opponent.hidden = !isGame;
        opponent.querySelector('input').disabled = !isGame;
    };

    const parse = (value) => {
        const date = value ? new Date(value) : null;
        return date && !Number.isNaN(date.getTime()) ? date : null;
    };

    const format = (date) => {
        const pad = (n) => String(n).padStart(2, '0');
        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
    };

    // Moving the start keeps the event's length, the way calendar apps do.
    let previousStart = starts.value;
    starts.addEventListener('change', () => {
        const oldStart = parse(previousStart);
        const newStart = parse(starts.value);
        const end = parse(ends.value);
        if (oldStart && newStart && end) {
            ends.value = format(new Date(newStart.getTime() + (end.getTime() - oldStart.getTime())));
        }
        previousStart = starts.value;
    });

    type.addEventListener('change', showOpponent);
    showOpponent();
})();

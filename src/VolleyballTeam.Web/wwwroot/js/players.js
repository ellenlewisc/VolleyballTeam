// Club player directory: server-side DataTables over POST, sorted by named fields.
(function () {
    const table = document.getElementById('players-table');
    if (!table) {
        return;
    }

    const token = document.querySelector('#players-antiforgery input[name="__RequestVerificationToken"]').value;
    const escape = function (value) {
        const div = document.createElement('div');
        div.textContent = value == null ? '' : String(value);
        return div.innerHTML;
    };

    new DataTable(table, {
        serverSide: true,
        processing: true,
        ajax: {
            url: table.dataset.source,
            type: 'POST',
            headers: { RequestVerificationToken: token },
        },
        layout: {
            top: 'search',
            topStart: null,
            topEnd: null,
        },
        language: { search: '', searchPlaceholder: 'Search players or guardians' },
        order: [[0, 'asc']],
        columns: [
            {
                data: 'name',
                name: 'name',
                render: function (data, type, row) {
                    return type === 'display'
                        ? '<a class="font-medium text-club hover:underline" href="' + escape(row.url) + '">' + escape(data) + '</a>'
                        : data;
                },
            },
            { data: 'gradYear', name: 'gradYear', defaultContent: '—' },
            { data: 'currentTeams', name: 'currentTeams', orderable: false, render: function (data) { return escape(data) || '—'; } },
            { data: 'guardianCount', name: 'guardianCount', orderable: false },
        ],
    });
})();

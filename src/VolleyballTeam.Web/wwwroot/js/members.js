// Club members list: server-side DataTables over POST, sorted by named fields.
(function () {
    const table = document.getElementById('members-table');
    if (!table) {
        return;
    }

    const token = document.querySelector('#members-antiforgery input[name="__RequestVerificationToken"]').value;
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
        language: { search: '', searchPlaceholder: 'Search by name or email' },
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
            { data: 'email', name: 'email', render: function (data) { return escape(data); } },
            { data: 'role', name: 'role' },
            {
                data: 'status',
                name: 'status',
                orderable: false,
                render: function (data) {
                    const classes = data === 'Active' ? 'bg-green-100 text-green-800' : 'bg-slate-200 text-slate-700';
                    return '<span class="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium ' + classes + '">' + escape(data) + '</span>';
                },
            },
            { data: 'currentTeams', name: 'currentTeams', orderable: false, render: function (data) { return escape(data) || '—'; } },
        ],
    });
})();

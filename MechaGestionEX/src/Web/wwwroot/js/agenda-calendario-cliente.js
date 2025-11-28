document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendario');
    if (calendarEl) {
        var tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        tomorrow.setHours(0, 0, 0, 0);

        var calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'timeGridWeek',
            locale: 'es',
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'timeGridWeek,timeGridDay'
            },
            buttonText: {
                today: 'Hoy',
                week: 'Semana',
                day: 'Día'
            },
            height: 'auto',
            slotMinTime: '09:00:00',
            slotMaxTime: '17:00:00',
            slotDuration: '01:00:00',
            hiddenDays: [0, 6],
            allDaySlot: false,
            events: function (fetchInfo, successCallback, failureCallback) {
                var tallerId = $('#tallerSelect').val();
                if (!tallerId || tallerId == '0') {
                    successCallback([]);
                    return;
                }
                $.get('/Cliente/GetAgendasCliente?tallerId=' + tallerId + '&start=' + fetchInfo.start.toISOString() + '&end=' + fetchInfo.end.toISOString(), function (data) {
                    successCallback(data);
                }).fail(function () {
                    failureCallback('Error al cargar eventos');
                });
            },
            selectable: true,
            select: function (info) {
                var tallerId = $('#tallerSelect').val();
                if (!tallerId || tallerId == '0') {
                    alert('Seleccione un taller primero.');
                    return;
                }
                var url = '/Cliente/CreateAgenda?fecha=' + encodeURIComponent(info.startStr) + '&tallerId=' + tallerId;
                window.location.href = url;
            },
            selectOverlap: false,
            selectAllow: function (selectInfo) {
                return selectInfo.start >= tomorrow;
            },
            selectConstraint: {
                startTime: '09:00',
                endTime: '17:00'
            }
        });
        calendar.render();

        $('#tallerSelect').select2({
            theme: 'bootstrap-5',
            width: '100%',
            placeholder: 'Seleccione taller',
            allowClear: false,
            ajax: {
                url: '/Cliente/GetTalleresCliente',
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return { search: params.term };
                },
                processResults: function (data) {
                    return { results: data.results };
                }
            }
        });

        $.get('/Cliente/GetTallerClienteActivo', function (data) {
            if (data.id && data.id != 0) {
                var option = new Option(data.text, data.id, true, true);
                $('#tallerSelect').append(option).trigger('change');
                calendar.refetchEvents();
            }
        });

        $('#tallerSelect').on('select2:select', function (e) {
            var tallerId = e.params.data.id;
            $.post('/Cliente/ChangeTallerCliente', { tallerId: tallerId }, function (response) {
                if (response.success) {
                    calendar.refetchEvents();
                } else {
                    alert(response.message);
                }
            });
        });
    }
});
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
            events: {
                url: '/Cliente/GetAgendasCliente',
                failure: function () {
                    alert('Hubo un error al cargar las citas.');
                }
            },
            selectable: true,
            select: function (info) {
                var url = '/Cliente/CreateAgenda?fecha=' + encodeURIComponent(info.startStr);
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
    }
});
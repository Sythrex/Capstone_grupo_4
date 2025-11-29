document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendario');
    if (calendarEl) {
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        window.calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'timeGridWeek',
            locale: 'es',
            initialView: window.innerWidth < 768 ? 'timeGridDay' : 'timeGridWeek',
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
            events: {
                url: '/Agenda/GetAgendas',
                failure: function () {
                    alert('Hubo un error al cargar las citas!');
                }
            },
            eventClick: function (info) {
                info.jsEvent.preventDefault();
                if (info.event.url) {
                    window.location.href = info.event.url;
                }
            },
            selectable: true,
            select: function (info) {
                var url = '/Agenda/Create?fecha=' + encodeURIComponent(info.startStr);
                window.location.href = url;
            },
            selectOverlap: false,
            selectAllow: function (selectInfo) {
                return selectInfo.start >= today;
            },
            selectConstraint: {
                startTime: '09:00',
                endTime: '17:00'
            }
        });
        calendar.render();

        window.addEventListener('resize', function () {
            calendar.changeView(window.innerWidth < 768 ? 'timeGridDay' : 'timeGridWeek');
        });
    }
});
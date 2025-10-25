document.addEventListener('DOMContentLoaded', function () {

    var calendarEl = document.getElementById('calendario');

    if (calendarEl) {
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
            slotMinTime: '07:00:00',
            slotMaxTime: '19:00:00',
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
            }
        });

        calendar.render();
    }
});
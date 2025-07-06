document.addEventListener('DOMContentLoaded', function () {
    const calendarEl = document.getElementById('calendar');

    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        editable: true,
        selectable: true,
        displayEventTime: false,
        contentHeight: 'auto',        
        fixedWeekCount: true,         
        aspectRatio: 2.5,             

        // kliknięcie w dzień — dodanie availability tylko jeśli brak algorytmu
        dateClick: function (info) {
            if ($('#algorithmSelect').val() === "") {
                $('#availabilityId').val('');
                $('#availabilityDate').val(info.dateStr);
                $('#availableHours').val('');
                $('#availabilityModal').modal('show');
            }
        },

        // kliknięcie w event
        eventClick: function (info) {
            const isPlanned = info.event.extendedProps.isPlanned;

            if (isPlanned) {
                // modal z informacją o planowanym zadaniu
                $('#plannedTaskTitle').text(info.event.title);
                $('#plannedTaskSubtask').text(info.event.extendedProps.subtaskDescription);
                $('#plannedTaskHours').text(info.event.extendedProps.hours);
                $('#plannedTaskDate').text(info.event.start.toISOString().split('T')[0]);
                $('#plannedTaskModal').modal('show');
            } else {
                // modal do edycji dostępności
                $('#availabilityId').val(info.event.id);
                $('#availabilityDate').val(info.event.startStr);
                $('#availableHours').val(info.event.extendedProps.availableHours);
                $('#availabilityModal').modal('show');
            }
        },
    });

    calendar.render();

    // funkcja do pobrania dostępności
    function fetchAvailabilities(fetchInfo, successCallback, failureCallback) {
        $.ajax({
            url: '/Calendar/GetAvailabilities',
            type: 'GET',
            success: function (data) {
                const events = data.map(function (availability) {
                    return {
                        id: availability.id,
                        title: `${availability.availableHours} hours`,
                        start: availability.date,
                        extendedProps: {
                            availableHours: availability.availableHours,
                            isPlanned: false
                        }
                    };
                });
                successCallback(events);
            },
            error: failureCallback
        });
    }

    // najpierw załaduj dostępności od razu
    calendar.addEventSource(fetchAvailabilities);

    // formularz zapis dostępności
    $('#availabilityForm').on('submit', function (e) {
        e.preventDefault();
        const id = $('#availabilityId').val();
        const date = $('#availabilityDate').val();
        const hours = $('#availableHours').val();
        const data = { id: parseInt(id) || 0, date: date, availableHours: parseFloat(hours) };

        const url = id ? '/Calendar/UpdateAvailability' : '/Calendar/SaveAvailability';
        $.ajax({
            url: url,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: function () {
                calendar.refetchEvents();
                $('#availabilityModal').modal('hide');
            }
        });
    });

    // obsługa kasowania
    $('#deleteAvailability').on('click', function () {
        const id = $('#availabilityId').val();
        if (id) {
            $.ajax({
                url: '/Calendar/DeleteAvailability',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(parseInt(id)),
                success: function () {
                    calendar.refetchEvents();
                    $('#availabilityModal').modal('hide');
                }
            });
        }
    });

    // załaduj algorytmy do selecta
    $.ajax({
        url: '/Calendar/GetAlgorithmResults',
        method: 'GET',
        success: function (data) {
            const select = $('#algorithmSelect');
            select.empty();
            select.append('<option value="">availabilities</option>');
            data.forEach(r => {
                const dt = new Date(r.created + 'Z');
                const formattedDate = dt.toLocaleString('pl-PL', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit'
                });
                const label = `${r.algorithm} | ${formattedDate} | ${r.score.toFixed(2)}`;
                select.append(`<option value="${r.id}">${label}</option>`);
            });
        }
    });

    // zmiana wyboru w select
    $('#algorithmSelect').on('change', function () {
        const selectedResultId = $(this).val();

        // usuń wszystko
        calendar.getEventSources().forEach(src => src.remove());

        // zawsze dodaj availability od nowa
        calendar.addEventSource(fetchAvailabilities);

        if (selectedResultId) {
            calendar.addEventSource({
                url: '/Calendar/GetPlannedEntriesForResult',
                method: 'GET',
                extraParams: { resultId: selectedResultId },
                color: '#FF9800',
                failure() { console.error("Nie udało się pobrać planu"); }
            });
        }

        calendar.refetchEvents();
    });

});

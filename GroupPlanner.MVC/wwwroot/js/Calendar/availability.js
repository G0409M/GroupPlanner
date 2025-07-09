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

        dateClick: function (info) {
            if ($('#algorithmSelect').val() === "availability" || $('#algorithmSelect').val() === "") {
                $('#availabilityId').val('');
                $('#availabilityDate').val(info.dateStr);
                $('#availableHours').val('');
                $('#availabilityModal').modal('show');
            }
        },

        eventClick: function (info) {
            const isPlanned = info.event.extendedProps.isPlanned;

            if (isPlanned) {
                $('#plannedTaskTitle').text(info.event.extendedProps.taskName);
                $('#plannedTaskSubtask').text(info.event.extendedProps.subtaskDescription);
                $('#plannedTaskHours').text(info.event.extendedProps.hours);
                $('#plannedTaskDate').text(info.event.start.toISOString().split('T')[0]);
                $('#plannedTaskModal').modal('show');
            } else {
                $('#availabilityId').val(info.event.id);
                $('#availabilityDate').val(info.event.startStr);
                $('#availableHours').val(info.event.extendedProps.availableHours);
                $('#availabilityModal').modal('show');
            }
        },
    });

    calendar.render();

    // załaduj dostępności od razu
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

    calendar.addEventSource(fetchAvailabilities);

    // zapis dostępności
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

    // kasowanie
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

            // Available Hours jako optgroup
            const availableGroup = $('<optgroup label="Available Hours"></optgroup>');
            availableGroup.append(`<option value="availability">Default</option>`);
            select.append(availableGroup);

            const geneticResults = data.filter(r => r.algorithm === 'Genetic');
            const antResults = data.filter(r => r.algorithm === 'Ant');

            if (geneticResults.length > 0) {
                const geneticGroup = $('<optgroup label="Genetic Results"></optgroup>');
                geneticResults.forEach(r => {
                    const dt = new Date(r.created + 'Z');
                    const formattedDate = dt.toLocaleString('pl-PL', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    });
                    const label = `${formattedDate} | ${r.score.toFixed(2)}`;
                    geneticGroup.append(`<option value="${r.id}">${label}</option>`);
                });
                select.append(geneticGroup);
            }

            if (antResults.length > 0) {
                const antGroup = $('<optgroup label="Ant Results"></optgroup>');
                antResults.forEach(r => {
                    const dt = new Date(r.created + 'Z');
                    const formattedDate = dt.toLocaleString('pl-PL', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    });
                    const label = `${formattedDate} | ${r.score.toFixed(2)}`;
                    antGroup.append(`<option value="${r.id}">${label}</option>`);
                });
                select.append(antGroup);
            }
        }
    });

    // obsługa zmiany selecta
    $('#algorithmSelect').on('change', function () {
        const selectedResultId = $(this).val();

        // wyczyść wszystkie źródła
        calendar.getEventSources().forEach(src => src.remove());

        // zawsze dodaj availability od nowa
        calendar.addEventSource(fetchAvailabilities);

        if (selectedResultId && selectedResultId !== "availability") {
            calendar.addEventSource({
                url: '/Calendar/GetPlannedEntriesForResult',
                method: 'GET',
                extraParams: { resultId: selectedResultId },
                failure() { console.error("Nie udało się pobrać planu"); },
                success: function (events) {
                    const taskColors = {};
                    let colorIndex = 0;
                    const colorPalette = [
                        '#FF5722', '#3F51B5', '#4CAF50', '#FFC107', '#9C27B0',
                        '#00BCD4', '#795548', '#607D8B'
                    ];

                    const grouped = {};

                    events.forEach(ev => {
                        const key = `${ev.start}|${ev.extendedProps.taskEncodedName}|${ev.extendedProps.subtaskDescription}`;
                        if (!grouped[key]) {
                            grouped[key] = {
                                id: ev.id,
                                title: ev.extendedProps.taskName + ` - ${ev.extendedProps.hours}h`,
                                start: ev.start,
                                color: '',
                                extendedProps: {
                                    ...ev.extendedProps,
                                    hours: ev.extendedProps.hours
                                }
                            };
                        } else {
                            grouped[key].extendedProps.hours += ev.extendedProps.hours;
                            grouped[key].title = ev.extendedProps.subtaskDescription + ` - ${grouped[key].extendedProps.hours}h`;
                        }
                    });

                    // Nadawanie kolorów po tasku
                    Object.values(grouped).forEach(ev => {
                        const taskName = ev.extendedProps.taskEncodedName;
                        if (!taskColors[taskName]) {
                            taskColors[taskName] = colorPalette[colorIndex % colorPalette.length];
                            colorIndex++;
                        }
                        ev.color = taskColors[taskName];
                    });

                    return Object.values(grouped);
                }

            });
        }

        calendar.refetchEvents();
    });

    $('#saveScheduleBtn').on('click', function () {
        const events = calendar.getEvents();

        const schedule = events
            .filter(e => e.extendedProps?.isPlanned)
            .map(e => ({
                Date: e.startStr,
                Hours: e.extendedProps.hours,
                SubtaskDescription: e.extendedProps.subtaskDescription,
                Subtask: {
                    Id: e.extendedProps.subtaskId // 👈 KLUCZOWE: ID subtaska potrzebne do uzupełnienia po stronie backendu
                }
            }));
        if (schedule.length === 0) {
            alert("There are no planned tasks to save.");
            return;
        }

        $.ajax({
            url: '/Calendar/SaveUserSchedule',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(schedule),
            success: function (response) {
                alert('Schedule saved successfully!');
                console.log('Saved schedule id:', response.scheduleId);
            },
            error: function () {
                alert('Failed to save schedule.');
            }
        });
    });

});

document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');
    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        editable: true,
        selectable: true,
        displayEventTime: false,
        events: fetchAvailabilities,
        dateClick: function (info) {
            // Open modal to add new availability on date click
            $('#availabilityId').val('');
            $('#availabilityDate').val(info.dateStr);
            $('#availableHours').val('');
            $('#availabilityModal').modal('show');
        },
        eventClick: function (info) {
            // Open modal to edit availability on event click
            $('#availabilityId').val(info.event.id);
            $('#availabilityDate').val(info.event.startStr);
            $('#availableHours').val(info.event.extendedProps.availableHours);
            $('#availabilityModal').modal('show');
        }
    });

    calendar.render();

    // Fetch events for calendar
    function fetchAvailabilities(fetchInfo, successCallback, failureCallback) {
        $.ajax({
            url: '/DailyAvailability/GetAvailabilities',
            type: 'GET',
            success: function (data) {
                // Map each availability to a FullCalendar event object
                var events = data.map(function (availability) {
                    return {
                        id: availability.id,
                        title: availability.availableHours + ' hours', // Display hours as title
                        start: availability.date,
                        extendedProps: {
                            availableHours: availability.availableHours
                        }
                    };
                });
                successCallback(events);
            }
        });
    }

    // Handle form submission for adding/editing availability
    $('#availabilityForm').on('submit', function (e) {
        e.preventDefault();
        var id = $('#availabilityId').val();
        var date = $('#availabilityDate').val();
        var hours = $('#availableHours').val();
        var data = { id: parseInt(id) || 0, date: date, availableHours: parseFloat(hours) };

        if (id) {
            // Update availability
            $.ajax({
                url: '/DailyAvailability/UpdateAvailability',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(data),
                success: function () {
                    calendar.refetchEvents();
                    $('#availabilityModal').modal('hide');
                }
            });
        } else {
            // Create new availability
            $.ajax({
                url: '/DailyAvailability/SaveAvailability',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(data),
                success: function () {
                    calendar.refetchEvents();
                    $('#availabilityModal').modal('hide');
                }
            });
        }
    });

    // Handle delete availability
    $('#deleteAvailability').on('click', function () {
        var id = $('#availabilityId').val();
        if (id) {
            $.ajax({
                url: '/DailyAvailability/DeleteAvailability',
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
});
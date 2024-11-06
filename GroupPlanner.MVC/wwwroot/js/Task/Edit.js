$(document).ready(function () {
    // Funkcja do załadowania subtasks
    LoadSubtasks();

    // Pobierz deadline głównego zadania
    const taskDeadline = new Date($("#createSubtaskModal input[name='Deadline']").data("task-deadline"));

    // Obsługa zdarzenia submit dla formularza tworzenia subtasku
    $("#createSubtaskModal form").submit(function (event) {
        event.preventDefault();

        const subtaskDeadline = new Date($("#createSubtaskModal input[name='Deadline']").val());
        const taskDeadline = new Date($("#createSubtaskModal input[name='Deadline']").data("task-deadline"));

        if (subtaskDeadline > taskDeadline) {
            toastr["error"]("Subtask deadline cannot exceed parent task deadline.");
            return;
        }

        $.ajax({
            url: $(this).attr('action'),
            type: $(this).attr('method'),
            data: $(this).serialize(),
            success: function (data) {
                toastr["success"]("Created subtask.");
                LoadSubtasks();
            },
            error: function (xhr) {
                let errorMessage = "Something went wrong";

                // Sprawdzamy, czy odpowiedź jest w formacie JSON
                try {
                    const responseJson = JSON.parse(xhr.responseText);
                    if (responseJson.Deadline && responseJson.Deadline.length > 0) {
                        errorMessage = responseJson.Deadline[0]; // Wyciągamy pierwszy komunikat błędu dla Deadline
                    }
                } catch (e) {
                    console.error("Error parsing JSON response:", e);
                }

                toastr["error"](errorMessage); // Wyświetlenie tylko komunikatu
            }
        });
    });


});

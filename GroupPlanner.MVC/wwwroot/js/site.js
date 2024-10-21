const RenderSubtasks = (subtasks, container) => {
    container.empty();

    // Mapa do konwersji statusów
    const statusMap = {
        0: "Nierozpoczęte",
        1: "W trakcie",
        2: "Ukończone"
    };

    for (const subtask of subtasks) {
        const statusText = statusMap[subtask.progressStatus] || "Nieznany status"; // W przypadku nieznanego statusu

        // Sprawdzanie liczby godzin i wybór odpowiedniego słowa
        const hourText = subtask.estimatedTime === 1 ? "hour" : "hours";

        container.append(
            `<div class="card border-secondary mb-3" style="max-width: 18rem;">
                <div class="card-body">
                    <p class="card-text text-center" style="font-weight: bold">${subtask.description}</p>
                    <hr style="border-top: 1px solid #ccc;"> <!-- Dodanie linii oddzielającej -->
                    <p class="card-text"><strong>Deadline:</strong> ${subtask.deadline}</p>
                    <p class="card-text"><strong>Status:</strong> ${statusText}</p>
                    <p class="card-text"><strong>Estimated Time:</strong> ${subtask.estimatedTime} ${hourText}</p>
                </div>
            </div>`
        );
    }
}

const LoadSubtasks = () => {
    const container = $("#subtasks");
    const taskEncodedName = container.data("encodedName");

    $.ajax({
        url: `/Task/${taskEncodedName}/Subtask`,
        type: 'get',
        success: function (data) {
            if (!data.length) {
                container.html("There are no subtasks for this task.");
            } else {
                RenderSubtasks(data, container);
            }
        },
        error: function () {
            toastr["error"]("Something went wrong");
        }
    });
}

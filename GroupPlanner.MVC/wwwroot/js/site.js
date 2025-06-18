const RenderSubtasks = (subtasks, container) => {
    container.empty();

    const statusMap = {
        0: "Nierozpoczęte",
        1: "W trakcie",
        2: "Ukończone"
    };

    for (const subtask of subtasks) {
        const statusText = statusMap[subtask.progressStatus] || "Nieznany status"; 

        const hourText = subtask.estimatedTime === 1 ? "hour" : "hours";

        container.append(
            `<div class="card border-secondary mb-3" style="max-width: 18rem;">
                <div class="card-body">
                    <p class="card-text text-center" style="font-weight: bold">${subtask.description}</p>
                    <hr style="border-top: 1px solid #ccc;"> <!-- Dodanie linii oddzielającej -->
                    <p class="card-text"><strong>Status:</strong> ${statusText}</p>
                    <p class="card-text"><strong>Estimated Time:</strong> ${subtask.estimatedTime} ${hourText}</p>
                    <button class="btn btn-danger btn-sm delete-subtask" data-subtask-id="${subtask.id}">Usuń</button>
                </div>
            </div>`
        );
    }
    container.find(".delete-subtask").click(function () {
        const subtaskId = $(this).data("subtask-id");
        DeleteSubtask(subtaskId);
    });
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
const DeleteSubtask = (subtaskId) => {
    const container = $("#subtasks");
    const taskEncodedName = container.data("encodedName");

    $.ajax({
        url: `/Task/${taskEncodedName}/Subtask/${subtaskId}`,
        type: 'DELETE',
        success: function () {
            toastr["success"]("Subtask deleted successfully");
            LoadSubtasks(); // Ponownie ładujemy listę podzadań
            LoadEstimatedTimeChart(); // Odświeżamy wykres
        },
        error: function () {
            toastr["error"]("Failed to delete subtask");
        }
    });
}

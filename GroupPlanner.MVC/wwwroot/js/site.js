(function ($) {
    const statusMap = {
        0: "Not Started",
        1: "In Progress",
        2: "Completed"
    };

    function renderSubtasks(subtasks, container) {
        container.empty();

        const isEditMode = container.data("editMode") === true || container.data("editMode") === "true";

        if (!subtasks.length) {
            container.append(`
                <div class="alert alert-info text-center">
                    No subtasks for this task.
                </div>
            `);
            return;
        }

        const row = $('<div class="row gy-4"></div>');

        subtasks.forEach(s => {
            const statusText = statusMap[s.progressStatus] || "Unknown";
            const badgeClass = s.progressStatus === 2
                ? 'success'
                : s.progressStatus === 1
                    ? 'warning'
                    : 'secondary';

            const hrText = s.estimatedTime === 1 ? 'hour' : 'hours';

            let actionButtons = "";
            if (!isEditMode) {
                actionButtons = `
                    <button class="btn btn-outline-success btn-sm increase-worked"
                        data-subtask-id="${s.id}">
                        <i class="bi bi-plus-circle"></i> +1h
                    </button>
                    <button class="btn btn-outline-secondary btn-sm decrease-worked"
                        data-subtask-id="${s.id}">
                        <i class="bi bi-dash-circle"></i> -1h
                    </button>
                `;
            } else {
                actionButtons = `
                    <button class="btn btn-outline-danger btn-sm delete-subtask"
                        data-subtask-id="${s.id}">
                        <i class="bi bi-trash"></i> Delete
                    </button>
                `;
            }

            row.append(`
                <div class="col-sm-6 col-md-4 mb-4">
                    <div class="card h-100 shadow-sm">
                        <div class="card-body d-flex flex-column">
                            <h5 class="card-title">${s.description}</h5>
                            <p class="mb-2">
                                <span class="badge bg-${badgeClass}">${statusText}</span>
                            </p>
                            <p class="text-muted mb-2">
                                <i class="bi bi-clock me-1"></i>${s.estimatedTime} ${hrText}
                            </p>
                            <p class="text-muted mb-4">
                                <i class="bi bi-check-circle me-1"></i>${s.workedHours} worked
                            </p>
                            <div class="mt-auto d-flex gap-2 justify-content-end align-items-center">
                                ${actionButtons}
                            </div>
                        </div>
                    </div>
                </div>
            `);
        });

        container.append(row);

        if (!isEditMode) {
            container.find('.increase-worked')
                .off('click')
                .on('click', function () {
                    const id = $(this).data('subtaskId');
                    updateWorkedHours(id, +1);
                });

            container.find('.decrease-worked')
                .off('click')
                .on('click', function () {
                    const id = $(this).data('subtaskId');
                    updateWorkedHours(id, -1);
                });
        } else {
            container.find('.delete-subtask')
                .off('click')
                .on('click', function () {
                    const id = $(this).data('subtaskId');
                    deleteSubtask(id);
                });
        }
    }

    function loadSubtasks() {
        const container = $('#subtasks');
        if (!container.length) return;
        const name = container.data('encodedName');
        $.get(`/Task/${name}/Subtask`)
            .done(data => renderSubtasks(data, container))
            .fail(() => toastr.error("Failed to load subtasks"));
    }

    function updateWorkedHours(subtaskId, delta) {
        const name = $('#subtasks').data('encodedName');
        $.ajax({
            url: `/Task/${name}/Subtask/${subtaskId}/WorkedHours`,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(delta)
        })
            .done(() => {
                toastr.success("Updated worked hours");
                loadSubtasks();
                if (typeof loadEstimatedTimeChart === 'function') {
                    loadEstimatedTimeChart();
                }
                refreshTaskStatus(name);
            })
            .fail(() => toastr.error("Failed to update worked hours"));
    }

    function deleteSubtask(subtaskId) {
        const name = $('#subtasks').data('encodedName');
        $.ajax({
            url: `/Task/${name}/Subtask/${subtaskId}`,
            type: 'DELETE'
        })
            .done(() => {
                toastr.success("Deleted subtask");
                loadSubtasks();
                if (typeof loadEstimatedTimeChart === 'function') {
                    loadEstimatedTimeChart();
                }
                refreshTaskStatus(name);
            })
            .fail(() => toastr.error("Failed to delete subtask"));
    }

    function refreshTaskStatus(taskEncodedName) {
        $.get(`/Task/${taskEncodedName}/Status`)
            .done(status => {
                const badge = $(".task-status-badge");
                if (badge.length) {
                    badge.text(status);

                    if (status === "Completed") {
                        badge.removeClass("bg-warning bg-secondary")
                            .addClass("bg-success")
                            .removeClass("text-dark");
                    } else if (status === "InProgress") {
                        badge.removeClass("bg-success bg-secondary")
                            .addClass("bg-warning text-dark");
                    } else {
                        badge.removeClass("bg-success bg-warning text-dark")
                            .addClass("bg-secondary");
                    }
                }
            })
            .fail(() => toastr.error("Failed to refresh task status"));
    }

    $(document).ready(function () {
        window.LoadSubtasks = loadSubtasks;
        loadSubtasks();
    });
})(jQuery);

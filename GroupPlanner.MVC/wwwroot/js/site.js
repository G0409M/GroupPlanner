(function ($) {
    // status map
    const statusMap = { 0: "Not Started", 1: "In Progress", 2: "Completed" };

    function renderSubtasks(subtasks, container) {
        container.empty();
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
            const badge = statusText === 'Completed' ? 'success'
                : statusText === 'In Progress' ? 'warning'
                    : 'secondary';
            const hrText = s.estimatedTime === 1 ? 'hour' : 'hours';

            row.append(`
        <div class="col-sm-6 col-md-4 mb-4">
          <div class="card h-100 shadow-sm">
            <div class="card-body d-flex flex-column">
              <h5 class="card-title">${s.description}</h5>
              <p class="mb-2">
                <span class="badge bg-${badge}">${statusText}</span>
              </p>
              <p class="text-muted mb-4">
                <i class="bi bi-clock me-1"></i>${s.estimatedTime} ${hrText}
              </p>
              <div class="mt-auto text-end">
                <button class="btn btn-outline-danger btn-sm delete-subtask"
                        data-subtask-id="${s.id}">
                  Delete
                </button>
              </div>
            </div>
          </div>
        </div>
      `);
        });
        container.append(row);

        // podpinamy handler
        container.find('.delete-subtask')
            .off('click')
            .on('click', function () {
                deleteSubtask($(this).data('subtaskId'));
            });
    }

    function loadSubtasks() {
        const container = $('#subtasks');
        if (!container.length) return;
        const name = container.data('encodedName');
        $.get(`/Task/${name}/Subtask`)
            .done(data => renderSubtasks(data, container))
            .fail(() => toastr.error("Failed to load subtasks"));
    }

    function deleteSubtask(id) {
        const container = $('#subtasks');
        const name = container.data('encodedName');
        $.ajax({
            url: `/Task/${name}/Subtask/${id}`,
            type: 'DELETE'
        })
            .done(() => {
                toastr.success("Subtask deleted successfully");
                loadSubtasks();
                // wywołaj chart tylko jeśli istnieje
                if (typeof loadEstimatedTimeChart === 'function') {
                    loadEstimatedTimeChart();
                }
            })
            .fail(() => toastr.error("Failed to delete subtask"));
    }

    // expose and init
    $(document).ready(function () {
        window.LoadSubtasks = loadSubtasks;
        window.DeleteSubtask = deleteSubtask;
        loadSubtasks();
    });
})(jQuery);

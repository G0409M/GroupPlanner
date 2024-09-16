const RenderSubtasks = (subtasks, container) => {
    container.empty();

    for (const subtask of subtasks) {
        container.append(
            `<div class="card border-secondary mb-3" style="max-width: 18rem;">
          <div class="card-header">${subtask.deadline}</div>
          <div class="card-body">
            <h5 class="card-title">${subtask.description}</h5> 
          </div>
        </div>`)
    }
}


const LoadSubtasks = () => {
    const container = $("#subtasks")
    const taskEncodedName = container.data("encodedName");

    $.ajax({
        url: `/Task/${taskEncodedName}/Subtask`,
        type: 'get',
        success: function (data) {
            if (!data.length) {
                container.html("There are no subtask for this task.")
            } else {
                RenderSubtasks(data, container)
            }
        },
        error: function () {
            toastr["error"]("Something went wrong")
        }
    })
}

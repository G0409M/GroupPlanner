(function ($) {
    $(function () {
        const modal = $('#createSubtaskModal');
        if (!modal.length) return;

        modal.find('form').on('submit', function (e) {
            e.preventDefault();
            const form = $(this);

            $.ajax({
                url: form.attr('action'),
                type: form.attr('method'),
                data: form.serialize()
            })
                .done(() => {
                    toastr.success("Created subtask.");

                    if (typeof LoadSubtasks === 'function') LoadSubtasks();

                    // dociąg status taska po dodaniu subtaska
                    const name = $('#subtasks').data('encodedName');
                    $.get(`/Task/${name}/Status`)
                        .done(status => {
                            const badge = $(".task-status-badge");
                            if (badge.length) {
                                badge.text(status);

                                if (status === "Completed") {
                                    badge.removeClass("bg-warning bg-secondary")
                                        .addClass("bg-success")
                                        .removeClass("text-dark");
                                } else if (status === "InProgress" || status === "In Progress") {
                                    badge.removeClass("bg-success bg-secondary")
                                        .addClass("bg-warning text-dark");
                                } else {
                                    badge.removeClass("bg-success bg-warning text-dark")
                                        .addClass("bg-secondary");
                                }
                            }
                        })
                        .fail(() => toastr.error("Failed to reload task status"));
                })
                .fail(xhr => {
                    let msg = "Something went wrong";
                    try {
                        const err = JSON.parse(xhr.responseText);
                        if (err.Deadline && err.Deadline.length) {
                            msg = err.Deadline[0];
                        }
                    } catch { }
                    toastr.error(msg);
                });
        });
    });
})(jQuery);

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

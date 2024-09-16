$(document).ready(function () {

    
    LoadSubtasks()


    $("#createSubtaskModal form").submit(function (event) {
        event.preventDefault();

        $.ajax({
            url: $(this).attr('action'),
            type: $(this).attr('method'),
            data: $(this).serialize(),
            success: function (data) {
                toastr["success"]("Created subtask.")
                LoadSubtasks()
            },
            error: function () {
                toastr["error"]("Something went wrong.")
            }
        })
    });
});
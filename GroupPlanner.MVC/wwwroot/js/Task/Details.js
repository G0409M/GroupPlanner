let estimatedTimeChartInstance = null; // Przechowujemy instancję wykresu

$(document).ready(function () {
    LoadSubtasks();
    LoadEstimatedTimeChart();
});

const LoadEstimatedTimeChart = () => {
    const taskEncodedName = $("#subtasks").data("encodedName");

    $.ajax({
        url: `/Task/${taskEncodedName}/Subtask`,
        type: 'get',
        success: function (subtasks) {
            console.log(subtasks); // Debug: Sprawdź dane subtasks

            // Jeśli istnieje poprzedni wykres, zniszcz go
            if (estimatedTimeChartInstance) {
                estimatedTimeChartInstance.destroy();
            }

            if (subtasks.length) {
                const labels = subtasks.map(subtask => subtask.description);
                const data = subtasks.map(subtask => subtask.estimatedTime);

                const ctx = document.getElementById('estimatedTimeChart').getContext('2d');
                estimatedTimeChartInstance = new Chart(ctx, {
                    type: 'doughnut',
                    data: {
                        labels: labels,
                        datasets: [{
                            label: 'Estimated Time in Hours',
                            data: data,
                            backgroundColor: subtasks.map((_, index) => `rgba(${(index * 50) % 255}, ${(index * 100) % 255}, ${(index * 150) % 255}, 0.6)`),
                            borderColor: subtasks.map((_, index) => `rgba(${(index * 50) % 255}, ${(index * 100) % 255}, ${(index * 150) % 255}, 1)`),
                            borderWidth: 1
                        }]
                    },
                    options: {
                        responsive: true,
                        plugins: {
                            legend: {
                                position: 'top',
                            },
                            tooltip: {
                                callbacks: {
                                    label: function (context) {
                                        return `${context.label}: ${context.raw} hours`;
                                    }
                                }
                            }
                        }
                    }
                });
            } else {
                $("#estimatedTimeChart").replaceWith("<p>No subtasks to display</p>");
            }
        },
        error: function () {
            toastr["error"]("Failed to load subtask data");
        }
    });
}


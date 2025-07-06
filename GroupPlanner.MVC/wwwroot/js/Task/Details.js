(function ($) {
    let estimatedTimeChartInstance = null;

    $(function () {
        // lista podzadań (site.js)
        if (typeof LoadSubtasks === 'function') {
            LoadSubtasks();
        }
        // wykres – tylko jeśli jest canvas
        if (document.getElementById('estimatedTimeChart')) {
            loadEstimatedTimeChart();
        }
    });

    window.loadEstimatedTimeChart = function () {
        const canvas = document.getElementById('estimatedTimeChart');
        if (!canvas) return;

        const name = $('#subtasks').data('encodedName');
        $.get(`/Task/${name}/Subtask`)
            .done(subtasks => {
                if (estimatedTimeChartInstance) {
                    estimatedTimeChartInstance.destroy();
                }
                if (!subtasks.length) {
                    $(canvas).parent().html("<p class='text-center text-muted'>No subtasks to display</p>");
                    return;
                }
                const labels = subtasks.map(s => s.description);
                const data = subtasks.map(s => s.estimatedTime);
                const ctx = canvas.getContext('2d');

                estimatedTimeChartInstance = new Chart(ctx, {
                    type: 'doughnut',
                    data: {
                        labels, datasets: [{
                            data,
                            backgroundColor: labels.map((_, i) =>
                                `rgba(${(i * 50) % 255},${(i * 80) % 255},${(i * 120) % 255},0.6)`),
                            borderColor: labels.map((_, i) =>
                                `rgba(${(i * 50) % 255},${(i * 80) % 255},${(i * 120) % 255},1)`),
                            borderWidth: 1
                        }]
                    },
                    options: {
                        cutout: '65%', maintainAspectRatio: false,
                        plugins: {
                            title: {
                                display: true,
                                text: 'Estimated Time in Hours',
                                padding: { top: 10, bottom: 20 },
                                font: { size: 16, weight: '500' }
                            },
                            legend: { position: 'bottom', labels: { boxWidth: 12, padding: 12 } },
                            tooltip: {
                                callbacks: { label: ctx => `${ctx.label}: ${ctx.raw} hrs` }
                            }
                        }
                    }
                });
            })
            .fail(() => toastr.error("Failed to load chart data"));
    };
})(jQuery);

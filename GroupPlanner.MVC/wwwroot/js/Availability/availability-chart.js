document.addEventListener('DOMContentLoaded', function () {
    const ctx = document.getElementById('availabilityChart').getContext('2d');
    const tableBody = document.querySelector('#dataTable tbody');
    let chart;
    const errorMessage = document.getElementById('error-message');

    function updateChart(data) {
        const labels = data.map(item =>
            new Date(item.date).toLocaleDateString('en-US', { day: 'numeric', month: 'short', year: 'numeric' })
        );
        const hours = data.map(item => item.availableHours);

        const barColors = hours.map(value => {
            if (value === 1) return 'rgba(255, 99, 132, 0.8)';
            if (value >= 2 && value <= 3) return 'rgba(255, 159, 64, 0.8)';
            if (value >= 4 && value <= 5) return 'rgba(255, 205, 86, 0.8)';
            if (value >= 6 && value <= 7) return 'rgba(75, 192, 192, 0.8)';
            return 'rgba(54, 162, 235, 0.8)';
        });

        if (chart) chart.destroy();

        chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    data: hours,
                    backgroundColor: barColors,
                    borderColor: barColors.map(color => color.replace('0.8', '1')),
                    borderWidth: 1,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                layout: {
                    padding: 10
                },
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        suggestedMax: 24,
                        ticks: {
                            stepSize: 0.5,
                            precision: 1
                        }
                    }
                }
            }
        });
    }

    function updateTable(data) {
        tableBody.innerHTML = '';
        data.forEach(item => {
            const row = `<tr>
                <td>${new Date(item.date).toLocaleDateString('en-US', { day: 'numeric', month: 'short', year: 'numeric' })}</td>
                <td>${item.availableHours}</td>
            </tr>`;
            tableBody.insertAdjacentHTML('beforeend', row);
        });
    }

    function fetchDataAndRenderChart(startDate, endDate) {
        fetch('/Availability/GetDailyAvailabilityData')
            .then(response => response.json())
            .then(data => {
                const grouped = {};

                data.forEach(item => {
                    const date = new Date(item.date);
                    const dateKey = date.toISOString().split('T')[0];

                    if (!grouped[dateKey]) {
                        grouped[dateKey] = 0;
                    }

                    grouped[dateKey] += parseFloat(item.availableHours);
                });

                let aggregatedData = Object.entries(grouped).map(([date, availableHours]) => ({
                    date,
                    availableHours
                }));

                const start = new Date(startDate);
                const end = new Date(endDate);

                aggregatedData = aggregatedData.filter(item => {
                    const itemDate = new Date(item.date);
                    return itemDate >= start && itemDate <= end;
                });

                aggregatedData.sort((a, b) => new Date(a.date) - new Date(b.date));

                if (aggregatedData.length === 0) {
                    errorMessage.textContent = 'No data available for the selected date range.';
                    errorMessage.classList.remove('d-none');
                } else {
                    errorMessage.classList.add('d-none');
                    updateChart(aggregatedData);
                    updateTable(aggregatedData);
                }
            })
            .catch(() => {
                errorMessage.textContent = 'An error occurred while fetching data.';
                errorMessage.classList.remove('d-none');
            });
    }

    const today = new Date();
    const fiveDaysAgo = new Date();
    const tenDaysLater = new Date();
    fiveDaysAgo.setDate(today.getDate() - 5);
    tenDaysLater.setDate(today.getDate() + 10);

    flatpickr("#startDate", {
        defaultDate: fiveDaysAgo,
        dateFormat: "Y-m-d"
    });

    flatpickr("#endDate", {
        defaultDate: tenDaysLater,
        dateFormat: "Y-m-d"
    });

    document.getElementById('filterButton').addEventListener('click', () => {
        const startDate = document.getElementById('startDate').value;
        const endDate = document.getElementById('endDate').value;
        fetchDataAndRenderChart(startDate, endDate);
    });

    document.getElementById('resetButton').addEventListener('click', () => {
        document.getElementById('startDate')._flatpickr.setDate(fiveDaysAgo);
        document.getElementById('endDate')._flatpickr.setDate(tenDaysLater);
        fetchDataAndRenderChart(fiveDaysAgo.toISOString().split('T')[0], tenDaysLater.toISOString().split('T')[0]);
    });

    fetchDataAndRenderChart(fiveDaysAgo.toISOString().split('T')[0], tenDaysLater.toISOString().split('T')[0]);
});

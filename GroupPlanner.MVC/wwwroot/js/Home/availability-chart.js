document.addEventListener('DOMContentLoaded', function () {
    const ctx = document.getElementById('availabilityChart').getContext('2d');
    const tableBody = document.querySelector('#dataTable tbody');
    let chart;
    const errorMessage = document.createElement('div');
    errorMessage.id = 'error-message';
    document.body.appendChild(errorMessage);

    /**
 * Aktualizuje wykres z danymi.
 * @param {Array} data - Dane do wykresu.
 */
    function updateChart(data) {
        const labels = data.map(item =>
            new Date(item.date).toLocaleDateString('en-US', { day: 'numeric', month: 'short', year: 'numeric' })
        );
        const hours = data.map(item => item.availableHours);

        const barColors = hours.map(value => {
            if (value === 1) return 'rgba(255, 99, 132, 0.8)'; // Wartość 1: czerwony
            if (value >= 2 && value <= 3) return 'rgba(255, 159, 64, 0.8)'; // Wartości 2-3: pomarańczowy
            if (value >= 4 && value <= 5) return 'rgba(255, 205, 86, 0.8)'; // Wartości 4-5: żółty
            if (value >= 6 && value <= 7) return 'rgba(75, 192, 192, 0.8)'; // Wartości 6-7: zielony
            return 'rgba(54, 162, 235, 0.8)'; // Wartości większe niż 7: niebieski
        });

        if (chart) chart.destroy();

        chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    data: hours,
                    backgroundColor: barColors,
                    borderColor: barColors.map(color => color.replace('0.8', '1')), // Intensywniejszy kolor dla obramowania
                    borderWidth: 1,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        max:24,
                    }
                }
            }
        });
    }


    /**
     * Aktualizuje tabelę z danymi.
     * @param {Array} data - Dane do tabeli.
     */
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

    /**
     * Pobiera dane z serwera i aktualizuje wykres oraz tabelę.
     * @param {string} startDate - Data początkowa.
     * @param {string} endDate - Data końcowa.
     */
    function fetchDataAndRenderChart(startDate, endDate) {
        fetch('/Home/GetDailyAvailabilityData')
            .then(response => response.json())
            .then(data => {
                const filteredData = data.filter(item => {
                    const date = new Date(item.date);
                    return date >= new Date(startDate) && date <= new Date(endDate);
                });
                if (filteredData.length === 0) {
                    errorMessage.textContent = 'No data available for the selected date range.';
                } else {
                    errorMessage.textContent = '';
                    updateChart(filteredData);
                    updateTable(filteredData);
                }
            })
            .catch(() => {
                errorMessage.textContent = 'An error occurred while fetching data.';
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

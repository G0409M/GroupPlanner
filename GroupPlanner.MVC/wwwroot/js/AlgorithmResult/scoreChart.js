window.renderScoreChart = function (scoreHistoryJson) {
    const ctx = document.getElementById('scoreChart')?.getContext('2d');
    if (!ctx) return;

    const scoreData = JSON.parse(scoreHistoryJson);

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: scoreData.map((_, i) => i + 1),
            datasets: [{
                label: 'Funkcja celu',
                data: scoreData,
                borderWidth: 2,
                pointRadius: 1,
                pointHoverRadius: 4,
                borderColor: 'rgba(54, 162, 235, 1)',
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                fill: false,
                tension: 0.3
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        font: {
                            size: 14
                        }
                    }
                },
                title: {
                    display: true,
                    text: 'Funkcja celu (Score w kolejnych iteracjach)',
                    font: {
                        size: 18
                    }
                }
            },
            scales: {
                x: {
                    title: {
                        display: true,
                        text: 'Iteracja',
                        font: { size: 14 }
                    },
                    ticks: {
                        autoSkip: true,
                        maxTicksLimit: 20,
                        font: { size: 12 }
                    },
                    grid: {
                        display: false
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: 'Wartość funkcji celu',
                        font: { size: 14 }
                    },
                    ticks: {
                        callback: value => value.toFixed(3),
                        font: { size: 12 }
                    },
                    grid: {
                        color: '#e0e0e0'
                    }
                }
            }
        }
    });
};

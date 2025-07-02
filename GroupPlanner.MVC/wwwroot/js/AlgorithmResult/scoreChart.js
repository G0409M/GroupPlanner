window.renderScoreChart = function (scoreHistoryJson) {
    const ctx = document.getElementById('scoreChart')?.getContext('2d');
    if (!ctx) return;

    const scoreData = JSON.parse(scoreHistoryJson);

    const dataMin = Math.min(...scoreData);
    const dataMax = Math.max(...scoreData);

    const marginPct = 0.05;
    const range = dataMax - dataMin;
    const margin = range * marginPct;

    const minY = dataMin - margin;
    const maxY = dataMax + margin;

    let decimalPlaces = 7;

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
                    labels: { font: { size: 14 } }
                },
                title: {
                    display: true,
                    text: 'Funkcja celu (Score w kolejnych iteracjach)',
                    font: { size: 18 }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `Funkcja celu: ${context.parsed.y.toFixed(decimalPlaces)}`;
                        }
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
                    grid: { display: false }
                },
                y: {
                    title: {
                        display: true,
                        text: 'Wartość funkcji celu',
                        font: { size: 14 }
                    },
                    suggestedMin: minY,
                    suggestedMax: maxY,
                    ticks: {
                        callback: value => value.toFixed(decimalPlaces),
                        font: { size: 12 }
                    },
                    grid: { color: '#e0e0e0' }
                }
            }
        }
    });
};

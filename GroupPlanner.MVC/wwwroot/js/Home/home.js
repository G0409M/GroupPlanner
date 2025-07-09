document.addEventListener("DOMContentLoaded", function () {
    // =========================
    // 📊 Subtask Status Pie Chart
    // =========================
    const pieCtx = document.getElementById('statusPieChart');

    if (pieCtx && typeof window.subtasksKPI === 'object') {
        const data = {
            labels: ["Completed", "In Progress", "Not Started"],
            datasets: [{
                label: "Subtask Status",
                data: [
                    window.subtasksKPI.CompletedCount || 0,
                    window.subtasksKPI.InProgressCount || 0,
                    window.subtasksKPI.NotStartedCount || 0
                ],
                backgroundColor: ["#81c784", "#ffd54f", "#e57373"], // pastelowe kolory
                borderWidth: 2
            }]
        };

        if (Chart.getChart("statusPieChart")) {
            Chart.getChart("statusPieChart").destroy();
        }

        new Chart(pieCtx.getContext("2d"), {
            type: 'pie',
            data: data,
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'left',
                        labels: {
                            usePointStyle: true,
                            pointStyle: 'circle',
                            padding: 15,
                            font: {
                                size: 13,
                                weight: 'bold'
                            }
                        }
                    }
                },
                animation: {
                    animateScale: true,
                    animateRotate: true
                }
            }
        });
    }

    // =========================
    // ⏳ Remaining Time per Task – Pie Chart
    // =========================
    const remainingPie = document.getElementById('remainingPieChart');
    if (remainingPie && Array.isArray(window.taskRemainingData)) {
        const pieLabels = window.taskRemainingData.map(t => t.TaskName || "(no name)");
        const pieValues = window.taskRemainingData.map(t => t.Remaining || 0);

        const colors = [
            '#4e79a7', '#f28e2b', '#e15759', '#76b7b2',
            '#59a14f', '#edc949', '#af7aa1', '#ff9da7',
            '#9c755f', '#bab0ab'
        ];

        if (Chart.getChart("remainingPieChart")) {
            Chart.getChart("remainingPieChart").destroy();
        }

        new Chart(remainingPie.getContext("2d"), {
            type: 'pie',
            data: {
                labels: pieLabels,
                datasets: [{
                    data: pieValues,
                    backgroundColor: colors,
                    borderColor: '#fff',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'left',
                        labels: {
                            usePointStyle: true,
                            pointStyle: 'circle',
                            padding: 15
                        }
                    },
                    
                },
                animation: {
                    animateScale: true,
                    animateRotate: true
                }
            }
        });
    }

    // =========================
    // 🗓️ Mini Dot Calendar Grid
    // =========================
    const calendarEl = document.getElementById('dashboardMiniCalendar');

    if (calendarEl && Array.isArray(window.plannedEntries)) {
        const calendarMap = new Map();
        window.plannedEntries.forEach(entry => {
            const dateStr = entry.Date?.substring(0, 10);
            const hours = entry.Hours ?? 0;
            if (!dateStr) return;
            calendarMap.set(dateStr, (calendarMap.get(dateStr) || 0) + hours);
        });

        const now = new Date();
        const year = now.getFullYear();
        const month = now.getMonth();
        const firstDay = new Date(year, month, 1);
        const lastDay = new Date(year, month + 1, 0);
        const daysInMonth = lastDay.getDate();
        const startWeekday = (firstDay.getDay() + 6) % 7;

        let html = `<div class="mini-calendar-grid">`;
        const weekDays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
        for (const day of weekDays) {
            html += `<div class="mini-calendar-header">${day}</div>`;
        }

        for (let i = 0; i < startWeekday; i++) {
            html += `<div class="mini-calendar-cell empty"></div>`;
        }

        for (let day = 1; day <= daysInMonth; day++) {
            const date = new Date(year, month, day);
            const dateKey = date.toLocaleDateString('en-CA');
            const dots = calendarMap.get(dateKey) || 0;
            const dotStr = Array.from({ length: dots }, () => '●').join('');

            html += `
                <div class="mini-calendar-cell">
                    <div class="mini-day">${day}</div>
                    <div class="dots">${dotStr}</div>
                </div>`;
        }

        html += `</div>`;
        calendarEl.innerHTML = html;
    }

    // =========================
    // ✅ Handle Mark as Done Click
    // =========================
    document.addEventListener('click', function (e) {
        const button = e.target.closest('.markWorkedByHours');
        if (!button) return;

        const subtaskId = parseInt(button.getAttribute('data-subtask-id'));
        const hours = parseInt(button.getAttribute('data-hours'));

        if (!subtaskId || !hours) return;

        fetch('/Home/MarkWorkedByHours', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ subtaskId, hours })
        })
            .then(res => res.ok ? res.json() : Promise.reject())
            .then(data => {
                if (data.success) location.reload();
                else alert("Update failed.");
            })
            .catch(() => alert("Error updating worked hours"));
    });
});

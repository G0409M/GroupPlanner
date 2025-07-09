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
                backgroundColor: ["#4caf50", "#ff9800", "#f44336"],
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
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            pointStyle: 'circle'
                        }
                    }
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

        let html = `
            <div class="mini-calendar-grid">
                <div class="mini-calendar-header">Mon</div>
                <div class="mini-calendar-header">Tue</div>
                <div class="mini-calendar-header">Wed</div>
                <div class="mini-calendar-header">Thu</div>
                <div class="mini-calendar-header">Fri</div>
                <div class="mini-calendar-header">Sat</div>
                <div class="mini-calendar-header">Sun</div>
        `;

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

});

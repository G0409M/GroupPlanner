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
            const dateKey = date.toISOString().substring(0, 10);
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
    // ✅ Upcoming Task Day
    // =========================
    const upcomingTasksContainer = document.getElementById("upcomingTasksContainer");

    if (upcomingTasksContainer && Array.isArray(window.userSchedule)) {
        const schedule = window.userSchedule;

        const upcoming = schedule
            .filter(entry => new Date(entry.Date) >= new Date())
            .sort((a, b) => new Date(a.Date) - new Date(b.Date));

        if (upcoming.length === 0) {
            upcomingTasksContainer.innerHTML = `<div class="alert alert-info text-center">No upcoming tasks scheduled.</div>`;
        } else {
            const nearest = upcoming[0];
            const subtaskCards = nearest.Entries.map(e => {
                const worked = e.workedHours || 0;
                const total = e.estimatedTime || 0;
                const status = e.progressStatus ?? 0;
                const badge = status === 2 ? "success" : status === 1 ? "warning" : "secondary";
                const hrText = total === 1 ? "hour" : "hours";
                const description = e.SubtaskDescription || "(no description)";
                const subtaskId = e.SubtaskId;

                return `
                <div class="col-md-6 col-lg-4 mb-3">
                    <div class="card h-100 shadow-sm">
                        <div class="card-body d-flex flex-column">
                            <h5 class="card-title">${description}</h5>
                            <span class="badge bg-${badge} mb-2">${["Not Started", "In Progress", "Completed"][status]}</span>
                            <p class="text-muted mb-1"><i class="bi bi-clock me-1"></i>${total} ${hrText}</p>
                            <p class="text-muted mb-3"><i class="bi bi-check-circle me-1"></i>${worked} worked</p>
                            <div class="mt-auto d-flex gap-2 justify-content-end">
                                <button class="btn btn-outline-success btn-sm markWorked" data-subtask-id="${subtaskId}">
                                    <i class="bi bi-plus-circle"></i> +1h
                                </button>
                            </div>
                        </div>
                    </div>
                </div>`;
            }).join("");

            upcomingTasksContainer.innerHTML = `
                <h5 class="mb-3 text-primary">
                    <i class="bi bi-calendar-event me-1"></i> ${nearest.Date.substring(0, 10)}
                </h5>
                <div class="row">${subtaskCards}</div>
            `;

            document.querySelectorAll('.markWorked').forEach(button => {
                button.addEventListener('click', function () {
                    const subtaskId = parseInt(this.getAttribute('data-subtask-id'));
                    if (!subtaskId) return;

                    fetch(`/Home/IncrementWorked`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ subtaskId })
                    })
                        .then(res => res.ok ? res.json() : Promise.reject())
                        .then(data => {
                            if (data.success) location.reload();
                            else alert("Update failed.");
                        })
                        .catch(() => alert("Error updating worked hours"));
                });
            });
        }
    }
});

document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("run-form");
    const progressBar = document.getElementById("progressBar");
    const statusText = document.getElementById("statusText");
    const loadingSection = document.getElementById("loading-section");

    if (!form || !progressBar || !statusText || !loadingSection) return;

    const submitBtn = form.querySelector('button[type="submit"]');
    let progressTimeout;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/algorithmHub")
        .build();

    connection.on("AlgorithmProgress", function (data) {
        clearTimeout(progressTimeout);
        const rawPct = ((data.generation + 1) / data.totalGenerations) * 100;
        const pct = Math.floor(rawPct);
        progressBar.style.width = pct + "%";
        progressBar.innerText = pct + "%";
        statusText.innerText = `Generation ${data.generation + 1}, score: ${data.score.toFixed(4)}`;
    });

    connection.start()
        .catch(err => {
            console.error(err);
            alert("Could not connect to progress hub. Results may not update in real time.");
        });

    form.addEventListener("submit", async function (e) {
        e.preventDefault();

        submitBtn && (submitBtn.disabled = true);

        // Reset progress bar and show loading section
        progressBar.style.width = "0%";
        progressBar.innerText = "0%";
        progressBar.classList.toggle("bg-success", false);
        progressBar.classList.toggle("bg-info", true);
        statusText.innerText = "";
        loadingSection.style.display = "block";

        // If no progress event arrives in 30s, hide loading
        progressTimeout = setTimeout(() => {
            loadingSection.style.display = "none";
            submitBtn && (submitBtn.disabled = false);
            alert("No progress update received. Please try again later.");
        }, 30000);

        const formData = new FormData(form);
        const data = Object.fromEntries(formData.entries());

        try {
            const response = await fetch(form.action, {
                method: "POST",
                body: new URLSearchParams(data)
            });

            if (!response.ok) {
                clearTimeout(progressTimeout);
                const errorText = await response.text();
                console.error("Server error:", errorText);
                alert("Server returned an error:\n\n" + errorText);
                loadingSection.style.display = "none";
                submitBtn && (submitBtn.disabled = false);
                return;
            }

            const result = await response.json();
            clearTimeout(progressTimeout);

            if (result.success) {
                progressBar.classList.toggle("bg-info", false);
                progressBar.classList.toggle("bg-success", true);
                progressBar.style.width = "100%";
                progressBar.innerText = "100%";
                statusText.innerText = "Algorithm completed.";

                const resultId = result.resultId;
                setTimeout(() => {
                    window.location.href = `/AlgorithmResult/Details/${resultId}`;
                }, 1000);
            } else {
                alert("Algorithm failed to start (server returned success=false).");
                loadingSection.style.display = "none";
                submitBtn && (submitBtn.disabled = false);
            }

        } catch (error) {
            clearTimeout(progressTimeout);
            console.error("JavaScript error:", error);
            alert("An error occurred in JavaScript:\n" + error.message);
            loadingSection.style.display = "none";
            submitBtn && (submitBtn.disabled = false);
        }
    });
});

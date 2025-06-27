document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("run-form");
    const progressBar = document.getElementById("progressBar");
    const statusText = document.getElementById("statusText");
    const loadingSection = document.getElementById("loading-section");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/algorithmHub")
        .build();

    connection.on("AlgorithmProgress", function (data) {
        const percentage = ((data.generation + 1) / data.totalGenerations) * 100;
        progressBar.style.width = percentage + "%";
        progressBar.innerText = Math.floor(percentage) + "%";
        statusText.innerText = `Generacja ${data.generation + 1}, score: ${data.score.toFixed(4)}`;
    });

    connection.start().catch(err => console.error(err));

    if (form) {
        form.addEventListener("submit", async function (e) {
            e.preventDefault();

            // Resetuj pasek i pokaż sekcję ładowania
            progressBar.style.width = "0%";
            progressBar.innerText = "0%";
            progressBar.classList.remove("bg-success");
            progressBar.classList.add("bg-info");
            statusText.innerText = "";
            loadingSection.style.display = "block";

            const formData = new FormData(form);
            const data = Object.fromEntries(formData.entries());

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: new URLSearchParams(data)
                });

                const result = await response.json();
                if (result.success) {
                    progressBar.classList.remove("bg-info");
                    progressBar.classList.add("bg-success");
                    progressBar.style.width = "100%";
                    progressBar.innerText = "100%";
                    statusText.innerText = "Algorytm zakończony.";

                    // ✅ Pauza 3 sekundy przed przekierowaniem
                    const resultId = result.resultId;
                    setTimeout(() => {
                        window.location.href = `/AlgorithmResult/Details/${resultId}`;
                    }, 3000);
                }

                if (result.success) {
                    progressBar.classList.remove("bg-info");
                    progressBar.classList.add("bg-success");
                    progressBar.style.width = "100%";
                    progressBar.innerText = "100%";
                    statusText.innerText = "Algorytm zakończony.";

                    // krótka pauza zanim przekierujemy
                    setTimeout(() => {
                        window.location.href = `/AlgorithmResult/Details/${result.resultId}`;
                    }, 1000); // 1 sekunda
                } else {
                    alert("Nie udało się uruchomić algorytmu.");
                }

            } catch (error) {
                console.error(error);
                alert("Wystąpił błąd podczas uruchamiania algorytmu.");
            }
        });
    }
   



});

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

                if (!response.ok) {
                    // jeśli np. 500
                    const errorText = await response.text();
                    console.error("Błąd serwera:", errorText);
                    alert("Serwer zwrócił błąd:\n\n" + errorText);
                    loadingSection.style.display = "none";
                    return;
                }

                const result = await response.json();

                if (result.success) {
                    progressBar.classList.remove("bg-info");
                    progressBar.classList.add("bg-success");
                    progressBar.style.width = "100%";
                    progressBar.innerText = "100%";
                    statusText.innerText = "Algorytm zakończony.";

                    const resultId = result.resultId;
                    setTimeout(() => {
                        window.location.href = `/AlgorithmResult/Details/${resultId}`;
                    }, 1000);
                } else {
                    alert("Nie udało się uruchomić algorytmu (serwer zwrócił success=false).");
                    loadingSection.style.display = "none";
                }

            } catch (error) {
                console.error("Błąd JS:", error);
                alert("Wystąpił błąd po stronie JavaScript:\n" + error.message);
                loadingSection.style.display = "none";
            }
        });
    }
});

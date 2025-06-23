const connection = new signalR.HubConnectionBuilder()
    .withUrl("/algorithmHub")
    .build();

connection.on("AlgorithmStarted", () => {
    document.getElementById("statusArea").style.display = "block";
    document.getElementById("statusMessage").innerText = "Algorithm is running...";
});

connection.on("AlgorithmFinished", () => {
    document.getElementById("statusMessage").innerText = "Algorithm finished!";
    setTimeout(() => {
        document.getElementById("statusArea").style.display = "none";
    }, 5000);
});

connection.start().catch(err => console.error(err));

// Optional: hook into form to send start message (if needed)
document.getElementById("runBtn")?.addEventListener("click", function () {
    connection.invoke("NotifyAlgorithmStarted").catch(err => console.error(err));
});

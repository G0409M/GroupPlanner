document.addEventListener("DOMContentLoaded", function () {
    const algorithmSelect = document.getElementById("algorithmSelect");
    const geneticParams = document.getElementById("geneticParams");
    const antParams = document.getElementById("antParams");

    function toggleParamSections() {
        if (algorithmSelect.value === "Genetic") {
            geneticParams.style.display = "block";
            antParams.style.display = "none";
        } else {
            geneticParams.style.display = "none";
            antParams.style.display = "block";
        }
    }

    algorithmSelect.addEventListener("change", toggleParamSections);
    toggleParamSections(); // initial state
});

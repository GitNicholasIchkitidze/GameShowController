document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("filterForm");
    const votesTable = document.getElementById("votesTable");
    const summaryDiv = document.getElementById("summary");

    async function loadVotes() {
        const params = new URLSearchParams(new FormData(form));
        const url = `/Admin/VotesMonitor?handler=Data&${params.toString()}`;
        const res = await fetch(url);
        const data = await res.json();

        // обновляем таблицу
        votesTable.innerHTML = "";
        data.votes.forEach(v => {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td>${v.userName}</td>
                <td>${v.message}</td>
                <td>${new Date(v.createdAt).toLocaleString()}</td>
            `;
            votesTable.appendChild(tr);
        });

        // обновляем статистику
        const entries = Object.entries(data.summary);
        summaryDiv.innerHTML = entries.length
            ? `<h5>RESULTS:</h5>
               <ul>${entries.map(([msg, count]) => `<li><b>${msg}</b>: ${count}</li>`).join("")}</ul>`
            : "<p>NO DATA</p>";
    }

    form.addEventListener("submit", (e) => {
        e.preventDefault();
        loadVotes();
    });

    // автообновление каждые 5 секунд
    setInterval(loadVotes, 5000);

    loadVotes();
});

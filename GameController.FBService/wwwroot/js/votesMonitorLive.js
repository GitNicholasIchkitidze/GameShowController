// votesMonitorLive.js-ის შიგთავსი

let liveMode = false;
let timer = null;
// ახალი: გლობალური ცვლადი ინტერვალისთვის
let intervalDuration = 10000; // დეფოლტი 10 წამი

document.addEventListener('DOMContentLoaded', () => {
    const liveButton = document.getElementById('toggleLive');
    const intervalSelect = document.getElementById('updateInterval'); // ახალი

    // განახლების ინტერვალის ცვლილება
    if (intervalSelect) {
        intervalSelect.addEventListener('change', (e) => {
            intervalDuration = parseInt(e.target.value);
            // თუ Live Mode ჩართულია, გადავტვირთოთ ტაიმერი ახალი ინტერვალით
            if (liveMode) {
                stopLiveUpdates();
                startLiveUpdates();
            }
        });
    }

    liveButton.addEventListener('click', () => {
        // ... (არსებული ლოგიკა იგივეა)
        liveMode = !liveMode;
        if (liveMode) {
            // Live ON
            liveButton.classList.remove('btn-success');
            liveButton.classList.add('btn-danger');
            liveButton.textContent = '■ STOP';
            status.textContent = 'Live ON...';            
            startLiveUpdates();
        } else {
            // Live OFF
            liveButton.classList.remove('btn-danger');
            liveButton.classList.add('btn-success');
            // ახალი ტექსტი, თქვენი მოთხოვნის მიხედვით
            liveButton.textContent = '▶ Live MODE'; // ან 'Live MODE OFF'
            status.textContent = 'Refresh Stopped.';
            stopLiveUpdates();
            
        }
    });
});

function startLiveUpdates() {
    loadVotes();
    // ტაიმერი იღებს intervalDuration-ს
    timer = setInterval(loadVotes, intervalDuration);
}

function stopLiveUpdates() {
    clearInterval(timer);
}

async function loadVotes() {
    try {
        // ... (არსებული fetch ლოგიკა)
        const from = document.getElementById('FromDate').value;
        const to = document.getElementById('ToDate').value;
        const response = await fetch(`/Admin/VotesMonitor?handler=JsonVotes&from=${from}&to=${to}`);
        if (!response.ok) return;

        const { votes, analytics } = await response.json();

        const lastUpdatedElement = document.getElementById('lastUpdated')
        lastUpdatedElement.classList.remove('pulsate-effect');
        // 4. ბოლო განახლების დროის ჩვენება
        lastUpdatedElement.textContent = `Last refresh: ${new Date().toLocaleString()}`;

        lastUpdatedElement.classList.add('pulsate-effect');
        setTimeout(() => {
            lastUpdatedElement.classList.remove('pulsate-effect');
        }, 500);


        // 1. საერთო სტატისტიკის განახლება
        document.getElementById('totalVotesCount').textContent = analytics.totalVotes;
        document.getElementById('totalUniqueUsersCount').textContent = analytics.totalUniqueUsers;

        // 2. ვარიანტების ანალიზის განახლება
        const optionsSummary = document.getElementById('optionsSummary');
        optionsSummary.innerHTML = analytics.options.map(option => {

            // ტოპ 3 მომხმარებლის ფორმატირება
            const topUsersList = option.topUsers.map(u =>
                // 4. მეორე ჰორიზონტალური ხაზი
                `<span><strong>${u.userName}</strong> (${u.userVoteCount} ballot)</span>`
            ).join(' | ');

            return `
                <div class="mb-3 p-2 border-bottom">


                    <div class="table-responsive">
                        <table class="table table-borderless mb-0">
                            <tbody>
                                <tr>
                                    <td class="fw-bold" style="width: 40%">${option.option}</td>
                                    <td class="text-center" style="width: 30%">Votes: ${option.voteCount} (${option.percentage.toFixed(2)}%)</td>
                                    <td class="text-end" style="width: 30%">Unique Voter: ${option.uniqueUsers}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>



                    
                    <div class="small text-muted">
                        Top 3 fans: ${topUsersList}
                    </div>
                </div>
            `;
        }).join('');

        // 3. ცხრილის განახლება
        const table = document.getElementById('votesTable');
        table.innerHTML = votes.map(v => `
            <tr>
                <td>${v.userName}</td>
                <td>${v.message}</td>
                <td>${v.candidatePhone}</td>
                <td>${new Date(v.timestamp).toLocaleString()}</td>
            </tr>
        `).join('');

    } catch (err) {
        console.error(err);
    }
}
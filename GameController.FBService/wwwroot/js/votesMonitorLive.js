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


    // ახალი: Listening Toggle ლოგიკა
    const toggleListeningBtn = document.getElementById('toggleListening');
    const statusDiv = document.getElementById('status');
    let currentListening = false; // საწყისი მნიშვნელობა

    // ფუნქცია სტატუსის განახლებისთვის
    function updateButtonText() {
        toggleListeningBtn.textContent = `Listening: ${currentListening ? 'ON' : 'OFF'}`;
        toggleListeningBtn.classList.toggle('btn-primary', currentListening);
        toggleListeningBtn.classList.toggle('btn-danger', !currentListening);
    }

    // მიმდინარე სტატუსის მიღება
    async function loadListeningStatus() {
        try {
            const response = await fetch('/api/FacebookWebhooks/CheckVotingModeStatus');
            if (!response.ok) throw new Error('Failed to fetch status');
            currentListening = await response.json();
            updateButtonText();
            statusDiv.textContent = `Listening mode loaded: ${currentListening ? 'Active' : 'Inactive'}`;
        } catch (error) {
            console.error(error);
            statusDiv.textContent = 'Error loading listening status';
            statusDiv.classList.add('text-danger');
        }
    }

    // Toggle ფუნქცია
    async function toggleListening_() {
        const newStatus = !currentListening;
        try {
            const response = await fetch('/api/FacebookWebhooks/SetBooleanKeyValue', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ key: 'fb_listening_active', active: newStatus })
            });
            if (!response.ok) throw new Error('Failed to toggle status');
            currentListening = newStatus;
            updateButtonText();
            statusDiv.textContent = `Listening mode changed to: ${currentListening ? 'Active' : 'Inactive'}`;
            statusDiv.classList.remove('text-danger');
        } catch (error) {
            console.error(error);
            statusDiv.textContent = 'Error toggling listening mode';
            statusDiv.classList.add('text-danger');
        }
    }

    async function toggleListening() {
        const newStatus = !currentListening;
        try {
            // გააგზავნე query string-ად: ?key=...&active=...
            const url = `/api/FacebookWebhooks/SetBooleanKeyValue?key=fb_listening_active&active=${newStatus}`;
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json' // შეგიძლია დატოვო, მაგრამ body არაა საჭირო
                }
                // body არ გამოიყენო, რადგან query string-ია
            });
            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Failed to toggle status: ${response.status} - ${errorText}`);
            }
            currentListening = newStatus;
            updateButtonText();
            statusDiv.textContent = `Listening mode changed to: ${currentListening ? 'Active' : 'Inactive'}`;
            statusDiv.classList.remove('text-danger');
        } catch (error) {
            console.error(error);
            statusDiv.textContent = 'Error toggling listening mode: ' + error.message;
            statusDiv.classList.add('text-danger');
        }
    }


    // ჩატვირთვისას იძახე
    loadListeningStatus();

    // ღილაკის event
    toggleListeningBtn.addEventListener('click', toggleListening);

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
                `<span><strong>${u.userName}</strong> (${u.userVoteCount} ხმა)</span>`
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
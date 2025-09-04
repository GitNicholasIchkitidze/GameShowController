// WebSocket-ის კავშირი C# სერვერთან
const ws = new WebSocket('ws://localhost:5000/ws-casparcg');

// ვარიანტები, რომლებსაც UI-ში ვაჩვენებთ
const voteOptions = ['A', 'B', 'C', 'D'];
const progressBarColors = {
    'A': 'bg-red-500',
    'B': 'bg-green-500',
    'C': 'bg-blue-500',
    'D': 'bg-yellow-500'
};

// UI-ს ელემენტების განახლების ფუნქცია
function update(data) {
    // შეტყობინების ტიპის შემოწმება
    //if (data.type !== 'update_vote_results') {
    //    console.warn('Unknown Type data:', data.type);
    //    return;
    //}

    const results = data.results;
    const totalVotes = data.totalVotes;
    const lastVoterName = data.voterName;
    const lastVoteAnswer = data.lastVoteAnswer;

    if (!data.Results || data.TotalVotes === undefined || data.VoterName === undefined) {
        console.error("Invalid data structure received.");
        return;
    }

    // თითოეული ვარიანტის განახლება
    voteOptions.forEach(option => {
        const count = results[option] || 0;
        const percentage = totalVotes > 0 ? (count / totalVotes) * 100 : 0;

        const progressBar = document.getElementById(`progress-bar-${option}`);
        const voteCountElement = document.getElementById(`vote-count-${option}`);

        if (progressBar) {
            // პროგრეს ბარის სიგანის განახლება
            progressBar.style.width = `${percentage}%`;
            // ფერის განახლება
            Object.values(progressBarColors).forEach(color => progressBar.classList.remove(color));
            progressBar.classList.add(progressBarColors[option]);
        }
        if (voteCountElement) {
            // ხმების რაოდენობის განახლება
            voteCountElement.innerText = `${count} ხმა`;
        }
    });

    // სულ ხმების განახლება
    const totalVotesElement = document.getElementById('total-votes');
    if (totalVotesElement) {
        totalVotesElement.innerText = `სულ ხმა: ${totalVotes}`;
    }

    // ბოლო ხმის მიმცემის განახლება
    const lastVoterElement = document.getElementById('last-voter');
    if (lastVoterElement) {
        if (lastVoterName) {
            lastVoterElement.innerText = `${lastVoterName} (${lastVoteAnswer})`;
            // ანიმაციის ხელახლა ჩართვა
            lastVoterElement.classList.remove('fade-in-out');
            void lastVoterElement.offsetWidth; // Force reflow
            lastVoterElement.classList.add('fade-in-out');
        } else {
            lastVoterElement.innerText = `-`;
            lastVoterElement.classList.remove('fade-in-out');
        }
    }
}


// WebSocket-ის კავშირის დამყარებისას
ws.onopen = () => {
    console.log('WebSocket Conncetion Established.');
    const registrationMessage = JSON.stringify({
        type: 'register',
        templateName: 'YTVote',
    });
    ws.send(registrationMessage);
    console.info("Registration Request Sent. " + registrationMessage);
};

// სერვერიდან მონაცემების მიღებისას
ws.onmessage = (event) => {
    console.log('Data received: ' + event.data);



    try {
        const data = JSON.parse(event.data);
        console.log('Data:', data);
        update(data);
    } catch (e) {
        console.error('JSON parse Error:', e);
    }
};

// კავშირის დახურვისას
ws.onclose = (event) => {
    console.log('WebSocket Connection Closed.', event.code, event.reason);
};

// შეცდომის დროს
ws.onerror = (error) => {
    console.error('WebSocket Error:', error);
};

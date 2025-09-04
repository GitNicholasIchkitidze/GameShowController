
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
    function updateUI(data) {
            // შეტყობინების ტიპის შემოწმება
            if (data.type !== 'update_vote_results') {
        console.warn('მიღებულია უცნობი ტიპის შეტყობინება:', data.type);
    return;
            }

    const results = data.results;
    const totalVotes = data.totalVotes;
    const lastVoterName = data.voterName;
    const lastVoteAnswer = data.lastVoteAnswer;

            // თითოეული ვარიანტის განახლება
            voteOptions.forEach(option => {
                const count = results[option] || 0;
                const percentage = totalVotes > 0 ? (count / totalVotes) * 100 : 0;

    const progressBar = document.getElementById(`progress-bar-${option}`);
    const voteCountElement = document.getElementById(`vote-count-${option}`);

    if (progressBar) {
        progressBar.style.width = `${percentage}%`;
    progressBar.className = `progress-bar ${progressBarColors[option]}`;
                }
    if (voteCountElement) {
        voteCountElement.innerText = `${count} ხმა`;
                }
            });

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
        console.log('WebSocket კავშირი დამყარებულია.');
    const registrationMessage = JSON.stringify({
        type: 'register',
    templateName: 'YTVote',
            });
    ws.send(registrationMessage);
    console.log("სარეგისტრაციო შეტყობინება გაიგზავნა.");
        };

        // სერვერიდან მონაცემების მიღებისას
        ws.onmessage = (event) => {
        console.log('მონაცემები მიღებულია:', event.data);
    try {
                const data = JSON.parse(event.data);
    updateUI(data);
            } catch (e) {
        console.error('JSON მონაცემების დამუშავების შეცდომა:', e);
            }
        };

        // კავშირის დახურვისას
        ws.onclose = () => {
        console.log('WebSocket კავშირი დაიხურა.');
        };

        // შეცდომისას
        ws.onerror = (error) => {
        console.error('WebSocket შეცდომა:', error);
        };


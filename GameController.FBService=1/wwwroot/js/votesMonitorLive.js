// votesMonitorLive.js-ის შიგთავსი

let liveMode = false;
let timer = null;
// ახალი: გლობალური ცვლადი ინტერვალისთვის
let intervalDuration = 10000; // დეფოლტი 10 წამი

// ADDED (2025-12): Last Voters paging state
let lastVotersPage = 1;
let lastVotersPageSize = 25;
let lastVotersTotalPages = 1;


document.addEventListener('DOMContentLoaded', () => {
    const liveButton = document.getElementById('toggleLive');
    const intervalSelect = document.getElementById('updateInterval'); // ახალი
    const manualRefreshBtn = document.getElementById('manualRefresh'); // ADDED (2026-01)
    const resetMetricsBtn = document.getElementById('resetMetrics'); // ADDED (2026-01)

    

    // ADDED (2025-12): init paging controls (Prev/Next/PageSize)
    initLastVotersPaging();

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
        
        liveMode = !liveMode;
        if (liveMode) {
            // Live ON
            liveButton.classList.remove('btn-success');
            liveButton.classList.add('btn-danger');
            liveButton.textContent = '■ STOP';
            if (statusDiv) statusDiv.textContent = 'Live ON...';    
            startLiveUpdates();
        } else {
            // Live OFF
            liveButton.classList.remove('btn-danger');
            liveButton.classList.add('btn-success');
            // ახალი ტექსტი, თქვენი მოთხოვნის მიხედვით
            liveButton.textContent = '▶ Live MODE'; // ან 'Live MODE OFF'
            if (statusDiv) statusDiv.textContent = 'Refresh Stopped.';
            stopLiveUpdates();
            
        }
    });
        
    if (manualRefreshBtn) {
        manualRefreshBtn.addEventListener('click', async () => {
            await refreshOnce('manual');
        });
    }

    if (resetMetricsBtn) {
        resetMetricsBtn.addEventListener('click', async () => {
            await loadMetrics(true);
            setText('m_metrics_last_reset', `Since: ${new Date().toLocaleString()}`);
        });
    }

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

    // ADDED (2025-12): initial metrics load (even without live mode)
    loadMetrics();
    loadVotes();

});

// ADDED (2025-12): wire up paging controls
function initLastVotersPaging() {
    const pageSizeSel = document.getElementById('lastVotersPageSize');
    const prevBtn = document.getElementById('lastVotersPrev');
    const nextBtn = document.getElementById('lastVotersNext');

    if (pageSizeSel) {
        pageSizeSel.value = String(lastVotersPageSize);
        pageSizeSel.addEventListener('change', () => {
            lastVotersPageSize = parseInt(pageSizeSel.value, 10) || 25;
            lastVotersPage = 1; // reset to first page when pageSize changes
            loadVotes();
        });
    }

    if (prevBtn) {
        prevBtn.addEventListener('click', () => {
            if (lastVotersPage > 1) {
                lastVotersPage--;
                loadVotes();
            }
        });
    }

    if (nextBtn) {
        nextBtn.addEventListener('click', () => {
            if (lastVotersPage < lastVotersTotalPages) {
                lastVotersPage++;
                loadVotes();
            }
        });
    }
}

function startLiveUpdates_() {
    loadVotes();
    loadMetrics(); 
    // ტაიმერი იღებს intervalDuration-ს
    timer = setInterval(() => {
        loadVotes();
        loadMetrics(); // ✅ important
    }, intervalDuration);
}

function stopLiveUpdates() {
    clearInterval(timer);
}

function startLiveUpdates() {
    refreshOnce('live-start');
    timer = setInterval(() => {
        refreshOnce('live-tick');
    }, intervalDuration);
}


async function loadVotes_OLD() {
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
                                    <td class="text-center" style="width: 30%">Votes: <span class="fw-bold">${option.voteCount}</span> (${option.percentage.toFixed(2)}%) 🟢 👍 ${option.voteCountYes} - 🔴 👎 ${option.voteCountNo}</td>
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

async function loadVotes() {
    try {
        const from = document.getElementById('FromDate').value;
        const to = document.getElementById('ToDate').value;

        // ADDED (2025-12): URL encode to avoid breaking querystring (':' 'T' etc.)
        const fromQ = encodeURIComponent(from || '');
        const toQ = encodeURIComponent(to || '');

        // ADDED (2025-12): send page/pageSize to backend
        const response = await fetch(`/Admin/VotesMonitor?handler=JsonVotes&from=${from}&to=${to}&page=${lastVotersPage}&pageSize=${lastVotersPageSize}`);
        if (!response.ok) return;

        // ADDED (2025-12): also read pagination payload
        const { votes, analytics, pagination } = await response.json();

        // ADDED (2025-12): update pager state/UI
        updateLastVotersPager(pagination);

        const lastUpdatedElement = document.getElementById('lastUpdated')
        lastUpdatedElement.classList.remove('pulsate-effect');
        lastUpdatedElement.textContent = `Last refresh: ${new Date().toLocaleString()}`;
        lastUpdatedElement.classList.add('pulsate-effect');
        setTimeout(() => lastUpdatedElement.classList.remove('pulsate-effect'), 500);

        document.getElementById('totalVotesCount').textContent = analytics.totalVotes;
        document.getElementById('totalUniqueUsersCount').textContent = analytics.totalUniqueUsers;

        const optionsSummary = document.getElementById('optionsSummary');
        optionsSummary.innerHTML = analytics.options.map(option => {
            const topUsersList = option.topUsers.map(u =>
                `<span><strong>${u.userName}</strong> (${u.userVoteCount} ხმა)</span>`
            ).join(' | ');

            return `
                <div class="mb-3 p-2 border-bottom">
                    <div class="table-responsive">
                        <table class="table table-borderless mb-0">
                            <tbody>
                                <tr>
                                    <td class="fw-bold" style="width: 40%">${option.option}</td>

                                    <td class="text-center" style="width: 45%">
                                    Votes:<span class="fw-bold">${option.voteCount}</span> (${option.percentage.toFixed(2)}%) 
                                    <span class="text-nowrap ms-2">🟢 👍&nbsp;<strong>${option.voteCountYes}</strong></span>
                                    <span class="mx-2">-</span>
                                    <span class="text-nowrap">🔴 👎&nbsp;<strong>${option.voteCountNo}</strong></span>
                                    </td>


                                    <td class="text-end" style="width: 15%">Unique: ${option.uniqueUsers}</td>
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

        // Last Voters table body
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

// ADDED (2025-12): fetch metrics from Razor handler
async function loadMetrics(reset = false) {
    try {

        const resp = await fetch(`/Admin/VotesMonitor?handler=JsonMetrics&reset=${reset}`);

        if (!resp.ok) return;

        const data = await resp.json();
        updateMetricsUI(data);
    } catch (err) {
        console.error('loadMetrics failed', err);
    }
}

// ADDED (2025-12): update DOM
function updateMetricsUI(data) {
    if (!data) return;

    // queue
    const q = data.queue || {};
    setText('m_queue_capacity', q.capacity);
    setText('m_queue_depth', q.currentDepth);
    setText('m_queue_peak', q.peakDepthSinceLastPoll); 
    setText('m_queue_dropped', q.droppedCount);

    // counters (camelCase from System.Text.Json)
    const c = data.counters || {};
    setText('m_ingress', c.ingressCount);
    setText('m_enq_ok', c.enqueueOk);
    setText('m_enq_drop', c.enqueueDropped);
    setText('m_dequeued', c.dequeued);
    setText('m_proc_ok', c.processedOk);
    setText('m_proc_fail', c.processedFailed);
    setText('m_garb_msg', c.garbageMessages);
    setText('m_not_in_time_user_msg', c.notInTimeUserMessages);
    setText('m_db_save_msg', c.recsSavedInDB);
    setText('m_inflight', c.inFlight); 
    

    if (data.serverTime) {
        // show local time
        setText('m_metrics_time', `Metrics: ${new Date(data.serverTime).toLocaleString()}`);
    }
    if (data.metricsResetDate) {
        //setText('m_metrics_last_reset', `Since: ${new Date(data.metricsResetDate).toLocaleString()}`);
    }
}

// ADDED (2025-12): update paging UI text/buttons
function updateLastVotersPager(p) {
    if (!p) return;

    lastVotersPage = p.page || 1;
    lastVotersPageSize = p.pageSize || lastVotersPageSize;
    lastVotersTotalPages = p.totalPages || 1;

    const info = document.getElementById('lastVotersPageInfo');
    if (info) info.textContent = `Page ${lastVotersPage} / ${lastVotersTotalPages}`;

    const totals = document.getElementById('lastVotersTotals');
    if (totals) totals.textContent = `Total rows: ${p.totalCount ?? '-'}`;

    const prevBtn = document.getElementById('lastVotersPrev');
    const nextBtn = document.getElementById('lastVotersNext');
    if (prevBtn) prevBtn.disabled = lastVotersPage <= 1;
    if (nextBtn) nextBtn.disabled = lastVotersPage >= lastVotersTotalPages;

    const pageSizeSel = document.getElementById('lastVotersPageSize');
    if (pageSizeSel && pageSizeSel.value !== String(lastVotersPageSize)) {
        pageSizeSel.value = String(lastVotersPageSize);
    }
}

function setText(id, value) {
    const el = document.getElementById(id);
    if (!el) return;
    el.textContent = (value === null || value === undefined) ? '-' : value;
}

async function refreshOnce(reason) {
    const manualRefreshBtn = document.getElementById('manualRefresh');
    const statusDiv = document.getElementById('status');

    if (reason === 'manual' && manualRefreshBtn) {
        manualRefreshBtn.disabled = true;
    }

    try {
        if (reason === 'manual' && statusDiv) statusDiv.textContent = 'Refreshing...';

        await Promise.all([
            loadVotes(),
            loadMetrics()
        ]);

        if (reason === 'manual' && statusDiv) {
            statusDiv.textContent = liveMode ? 'Live ON...' : 'Refreshed.';
        }
    } catch (e) {
        console.error('refreshOnce failed', e);
        if (reason === 'manual' && statusDiv) statusDiv.textContent = 'Refresh failed.';
    } finally {
        if (reason === 'manual' && manualRefreshBtn) {
            manualRefreshBtn.disabled = false;
        }
    }
}
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

    //დამატებილია 13_01 ედიტი2 - ensure bootstrap bundle is available for tooltips
    ensureBootstrapTooltipsReady();

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
            setText('m_clicker_metrics_last_reset', ` Since: ${new Date().toLocaleString()}`);

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

    //დამატებილია 13_01 ედიტი2
    refreshTooltipsSafe();

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

function stopLiveUpdates() {
    clearInterval(timer);
}

function startLiveUpdates() {
    refreshOnce('live-start');
    timer = setInterval(() => {
        refreshOnce('live-tick');
    }, intervalDuration);
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

        //დამატებილია 13_01 ედიტი2 - tooltips for dynamically rendered DOM
        refreshTooltipsSafe();

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
    setText('m_db_err_while_save', c.errorDBWhileSave);
    setText('m_inflight', c.inFlight);

    // ✅ NEW (2026-01): Clicker per-candidate / per-flag metrics
    renderClickerMetrics(c);

    if (data.serverTime) {
        // show local time
        setText('m_metrics_time', `Metrics: ${new Date(data.serverTime).toLocaleString()}`);
    }

    //დამატებილია 13_01 ედიტი2
    refreshTooltipsSafe();
}

function renderClickerMetrics(counters) {
    const tbody = document.getElementById('m_clicker_candidates_tbody');
    const flagsBox = document.getElementById('m_clicker_flags_box');

    if (!tbody || !flagsBox) return;

    const saved = counters?.savedInDBByCandidate || {};
    const bands = counters?.candidateRiskBands || {}; // ახალი დამატებული
    const flags = counters?.candidateFlagMetrics || {};

    // candidates union (saved + bands)
    const candidatesSet = new Set([
        ...Object.keys(saved),
        ...Object.keys(bands)
    ]);

    const candidates = Array.from(candidatesSet);
    if (candidates.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" class="text-muted">No data yet…</td></tr>`;
        flagsBox.innerHTML = `<div class="text-muted">No data yet…</div>`;
        return;
    }

    // Sort by total (prefer SavedInDBByCandidate, fallback to sum of bands)
    candidates.sort((a, b) => {
        const ta = Number(saved[a] ?? sumBands(bands[a]));
        const tb = Number(saved[b] ?? sumBands(bands[b]));
        return tb - ta;
    });

    // Build RiskBand table: Candidate | Normal | Suspicious(30–59) | Very(60–99) | Blocked(100+)
    tbody.innerHTML = candidates.map(candidate => {
        const b = bands[candidate] || {};
        const normal = b.NORMAL ?? 0;
        const suspicious = b.SUSPICIOUS_30_59 ?? 0;
        const verySuspicious = b.VERY_SUSPICIOUS_60_99 ?? 0;
        const blocked = b.BLOCKED_100_PLUS ?? 0; // RiskScore >= 100  // ახალი დამატებული

        return `
            <tr>
                <td class="fw-bold">${escapeHtml(candidate)}</td>
                <td class="text-end">${formatInt(normal)}</td>
                <td class="text-end">${formatInt(suspicious)}</td>
                <td class="text-end">${formatInt(verySuspicious)}</td>
                <td class="text-end" style="color:red">${formatInt(blocked)}</td>
            </tr>
        `;
    }).join('');

    // Build per-candidate top flags list (unchanged behavior)
    flagsBox.innerHTML = candidates.map(candidate => {
        const f = flags[candidate] || {};

        // Exclude helper flags from “top list”
        const entries = Object.entries(f)
            .filter(([k, v]) => v > 0 && k !== 'SUSPICIOUS' && k !== 'BLOCKED')
            .sort((a, b) => (b[1] || 0) - (a[1] || 0))
            .slice(0, 6);

        const badges = entries.length === 0
            ? `<span class="text-muted">No flags</span>`
            : entries.map(([k, v]) => {
                return `<span class="badge bg-secondary me-1 mb-1">${escapeHtml(k)}: ${formatInt(v)}</span>`;
            }).join('');

        return `
            <div class="p-2 border rounded">
                <div class="fw-bold mb-1">${escapeHtml(candidate)}</div>
                <div class="d-flex flex-wrap">${badges}</div>
            </div>
        `;
    }).join('');
}

function sumBands(b) { // ახალი დამატებული
    if (!b) return 0;
    return Number(b.NORMAL ?? 0)
        + Number(b.SUSPICIOUS_30_59 ?? 0)
        + Number(b.VERY_SUSPICIOUS_60_99 ?? 0)
        + Number(b.BLOCKED_100_PLUS ?? 0);
}



function formatInt(v) {
    const n = Number(v);
    return Number.isFinite(n) ? n.toLocaleString() : '-';
}

// Basic HTML escaping for UI safety
function escapeHtml(str) {
    if (str === null || str === undefined) return '';
    return String(str)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');
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


//დამატებილია 13_01 ედიტი2 - robust tooltip init (dispose + init)
// Works even if DOM is re-rendered by JS.
function refreshTooltipsSafe() {
    try {
        if (!window.bootstrap || !bootstrap.Tooltip) return;

        // Dispose existing instances to avoid duplicates
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
            const inst = bootstrap.Tooltip.getInstance(el);
            if (inst) inst.dispose();
        });

        // Init new instances
        document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
            new bootstrap.Tooltip(el, { trigger: 'hover', html: false });
        });
    } catch (e) {
        console.warn('refreshTooltipsSafe failed', e);
    }
}

//დამატებილია 13_01 ედიტი2
function ensureBootstrapTooltipsReady() {
    // Already present?
    if (window.bootstrap && bootstrap.Tooltip) return true;

    // Try to load bootstrap bundle dynamically:
    // 1) local lib path (typical for ASP.NET templates)
    // 2) CDN fallback
    const candidates = [
        '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
        '/lib/bootstrap/dist/js/bootstrap.bundle.js',
        'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js'
    ];

    for (const src of candidates) {
        try {
            const ok = loadScriptOnce(src);
            if (ok && window.bootstrap && bootstrap.Tooltip) return true;
        } catch (e) {
            // continue to next
        }
    }

    console.warn('Bootstrap Tooltip is not available. Make sure Bootstrap bundle is loaded.');
    return false;
}

//დამატებილია 13_01 ედიტი2
function loadScriptOnce(src) {
    return new Promise((resolve) => {
        // If already loaded
        const existing = Array.from(document.getElementsByTagName('script'))
            .find(s => (s.src || '').toLowerCase() === src.toLowerCase());
        if (existing) {
            // give it a tick
            setTimeout(() => resolve(true), 0);
            return;
        }

        const s = document.createElement('script');
        s.src = src;
        s.async = true;

        s.onload = () => resolve(true);
        s.onerror = () => resolve(false);

        document.head.appendChild(s);
    });
}
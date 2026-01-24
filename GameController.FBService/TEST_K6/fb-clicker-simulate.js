import http from "k6/http";
import { sleep, check } from "k6";
import { SharedArray } from "k6/data";

// --------------------
// ENV CONFIG
// --------------------
const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";
const WEBHOOK_PATH = __ENV.WEBHOOK_PATH || "/api/FacebookWebhooks/HandleWebhook";
const RECIPIENT_ID = __ENV.RECIPIENT_ID || "190729677455227";

const COOLDOWN_SEC = parseInt(__ENV.COOLDOWN_SEC || "60", 10);
const HUG_TOL_SEC = parseInt(__ENV.HUG_TOL_SEC || "2", 10);
const AFTER_W_SEC = parseInt(__ENV.AFTER_W_SEC || "5", 10);

const USER_POOL = parseInt(__ENV.USER_POOL || "300", 10);

// ✅ Fixed candidates as requested
const CANDIDATES = ["მარიამი", "ორი მუზა", "ბიჭები ქუთაისიდან"];

// --------------------
// Helpers
// --------------------
function randInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function pick(arr) {
    return arr[randInt(0, arr.length - 1)];
}

function nowMs() {
    return Date.now();
}

function makeMid(senderId) {
    return `m_k6_${senderId}_${nowMs()}_${Math.random().toString(16).slice(2)}`;
}

function fbPostbackBody({ senderId, recipientId, payload, title, mid }) {
    return JSON.stringify({
        object: "page",
        entry: [
            {
                time: nowMs(),
                id: recipientId,
                messaging: [
                    {
                        sender: { id: senderId },                // ✅ TESTUSER_...
                        recipient: { id: recipientId },
                        timestamp: nowMs(),
                        postback: {
                            title: title || payload,
                            payload: payload,                      // ✅ "<Candidate>:YES"
                            mid: mid || makeMid(senderId),
                        },
                    },
                ],
            },
        ],
    });
}

function sendWebhook(body) {
    const url = `${BASE_URL}${WEBHOOK_PATH}`;
    const res = http.post(url, body, { headers: { "Content-Type": "application/json" } });

    check(res, {
        "status 2xx": (r) => r.status >= 200 && r.status < 300,
    });

    return res;
}

// ✅ Shared fake users, prefixed with TESTUSER_
const USERS = new SharedArray("users", function () {
    const arr = [];
    for (let i = 0; i < USER_POOL; i++) {
        arr.push(`TESTUSER_${i + 1}`);
    }
    return arr;
});

// --------------------
// k6 OPTIONS
// --------------------
export const options = {
    scenarios: {
        // 1) Cooldown hugging
        cooldown_hugging: {
            executor: "per-vu-iterations",
            vus: parseInt(__ENV.VUS_HUG || "20", 10),
            iterations: parseInt(__ENV.ITERS_HUG || "10", 10),
            maxDuration: "30m",
            exec: "scenarioCooldownHugging",
        },

        // 2) After-cooldown burst (two-step per user)
        after_cooldown_burst: {
            executor: "constant-arrival-rate",
            rate: parseInt(__ENV.BURST_RPS || "30", 10),
            timeUnit: "1s",
            duration: __ENV.BURST_DUR || "20s",
            preAllocatedVUs: parseInt(__ENV.BURST_VUS || "200", 10),
            maxVUs: parseInt(__ENV.BURST_MAX_VUS || "400", 10),
            exec: "scenarioAfterCooldownBurst",
        },

        // 3) Same-second burst
        same_second_burst: {
            executor: "ramping-arrival-rate",
            startRate: parseInt(__ENV.SAME_START || "0", 10),
            timeUnit: "1s",
            stages: [
                { target: parseInt(__ENV.SAME_PEAK || "150", 10), duration: __ENV.SAME_RAMP || "5s" },
                { target: parseInt(__ENV.SAME_PEAK || "150", 10), duration: __ENV.SAME_HOLD || "5s" },
                { target: 0, duration: __ENV.SAME_DOWN || "5s" },
            ],
            preAllocatedVUs: parseInt(__ENV.SAME_VUS || "300", 10),
            maxVUs: parseInt(__ENV.SAME_MAX_VUS || "600", 10),
            exec: "scenarioSameSecondBurst",
        },
    },
    thresholds: {
        http_req_failed: ["rate<0.02"],
        http_req_duration: ["p(95)<1200"],
    },
};

// --------------------
// SCENARIOS
// --------------------
export function scenarioCooldownHugging() {
    const userId = USERS[(__VU - 1) % USERS.length];
    const cand = pick(CANDIDATES);

    const payload = `${cand}:YES`;
    const body = fbPostbackBody({
        senderId: userId,
        recipientId: RECIPIENT_ID,
        payload,
        title: `${cand} 👍`,
    });

    sendWebhook(body);

    // cooldown + jitter 0..tol
    const jitter = randInt(0, HUG_TOL_SEC);
    sleep(COOLDOWN_SEC + jitter);
}

export function scenarioAfterCooldownBurst() {
    const userId = pick(USERS);
    const cand = pick(CANDIDATES);
    const payload = `${cand}:YES`;

    // attempt #1
    sendWebhook(
        fbPostbackBody({
            senderId: userId,
            recipientId: RECIPIENT_ID,
            payload,
            title: `${cand} 👍`,
        })
    );

    // attempt #2 in cooldown..cooldown+W window
    sleep(COOLDOWN_SEC + randInt(0, AFTER_W_SEC));

    sendWebhook(
        fbPostbackBody({
            senderId: userId,
            recipientId: RECIPIENT_ID,
            payload,
            title: `${cand} 👍`,
        })
    );
}

export function scenarioSameSecondBurst() {
    const userId = pick(USERS);
    const cand = pick(CANDIDATES);
    const payload = `${cand}:YES`;

    sendWebhook(
        fbPostbackBody({
            senderId: userId,
            recipientId: RECIPIENT_ID,
            payload,
            title: `${cand} 👍`,
        })
    );
}

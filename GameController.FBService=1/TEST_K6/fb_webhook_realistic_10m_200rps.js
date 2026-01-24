import http from "k6/http";
import { check, sleep } from "k6";
import { Counter } from "k6/metrics";

// -------------------- CONFIG --------------------
const USERS = parseInt(__ENV.USERS || "500", 10);          // 500 users
const TARGET_RPS = parseFloat(__ENV.RPS || "200");         // 200 ingress/sec
const DURATION_SEC = parseInt(__ENV.DURATION_SEC || "600", 10); // 10 minutes = 600s

const BASE_URL = (__ENV.BASE_URL || "https://localhost:7112").replace(/\/$/, "");
const PATH = __ENV.PATH || "/api/FacebookWebhooks";
const URL = `${BASE_URL}${PATH}`;

const TIMEOUT = __ENV.TIMEOUT || "5s";
const PAGE_ID = __ENV.PAGE_ID || "PAGE_ID_TEST";

// Postback payload candidates (default matches your older test style)
const POSTBACK_PAYLOADS = (__ENV.POSTBACK_PAYLOADS || "ლორენცო:Yes,ბონდო:Yes,მარიამი:Yes")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);

// target pacing per user
const PER_USER_INTERVAL_SEC = USERS / TARGET_RPS; // 500/200 = 2.5 sec

// -------------------- METRICS --------------------
const ingressTotal = new Counter("ingress_total");
const sentGetStarted = new Counter("sent_getstarted");
const sentPostback = new Counter("sent_postback");
const sentText = new Counter("sent_text");

// -------------------- OPTIONS --------------------
export const options = {
    insecureSkipTLSVerify: true,
    noConnectionReuse: false,
    maxRedirects: 0,
    discardResponseBodies: true,

    scenarios: {
        realistic_10m: {
            executor: "constant-vus",
            vus: USERS,
            duration: `${DURATION_SEC}s`,
            gracefulStop: "0s",
        },
    },

    thresholds: {
        http_req_failed: ["rate<0.01"],
        http_req_duration: ["p(95)<200"],
    },
};

// -------------------- HELPERS --------------------
function randInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function randString(len) {
    const chars = "abcdefghijklmnopqrstuvwxyz0123456789";
    let s = "";
    for (let i = 0; i < len; i++) s += chars.charAt(randInt(0, chars.length - 1));
    return s;
}

function pickPostback() {
    return POSTBACK_PAYLOADS[randInt(0, POSTBACK_PAYLOADS.length - 1)];
}

function makeEnvelope(userId, eventObj) {
    const now = Date.now();
    return {
        object: "page",
        entry: [
            {
                id: PAGE_ID,
                time: now,
                messaging: [
                    {
                        sender: { id: userId },
                        recipient: { id: PAGE_ID },
                        timestamp: now,
                        ...eventObj,
                    },
                ],
            },
        ],
    };
}

function postEvent(payloadObj) {
    ingressTotal.add(1);

    const res = http.post(URL, JSON.stringify(payloadObj), {
        headers: { "Content-Type": "application/json" },
        timeout: TIMEOUT,
    });

    check(res, {
        "status is 200": (r) => r.status === 200,
        "status is 307/308": (r) => r.status === 307 || r.status === 308,
        "status is 400": (r) => r.status === 400,
    });

    return res;
}

// -------------------- TEST LOGIC --------------------
export default function () {
    // user identity stable per VU
    const userId = `USER_${__VU}`; // 1..USERS

    // jitter small start spread (prevents huge 500 req burst in one exact millisecond)
    sleep(Math.random() * 0.5);

    const startMs = Date.now();
    const endMs = startMs + DURATION_SEC * 1000;

    let didGetStarted = false;
    let didFirstPostback = false;

    while (Date.now() < endMs) {
        const loopStart = Date.now();
        const elapsedSec = (loopStart - startMs) / 1000;

        const mid = `mid.k6.${userId}.${__ITER}.${loopStart}`;

        // 1) each user sends "getstarted" once
        if (!didGetStarted) {
            const payload = makeEnvelope(userId, {
                message: {
                    mid,
                    text: "getstarted",
                },
            });

            sentGetStarted.add(1);
            postEvent(payload);

            didGetStarted = true;
        }
        // 2) until 20 seconds: keep traffic flowing (we'll send 5-char texts)
        else if (elapsedSec < 20) {
            const payload = makeEnvelope(userId, {
                message: {
                    mid,
                    text: randString(5),
                },
            });

            sentText.add(1);
            postEvent(payload);
        }
        // 3) at ~20 seconds: first random PostBack
        else if (!didFirstPostback) {
            const payload = makeEnvelope(userId, {
                postback: {
                    mid,
                    payload: pickPostback(),
                },
            });

            sentPostback.add(1);
            postEvent(payload);

            didFirstPostback = true;
        }
        // 4) next 1 minute (20s..80s): random 5-char text payloads
        else if (elapsedSec < 80) {
            const payload = makeEnvelope(userId, {
                message: {
                    mid,
                    text: randString(5),
                },
            });

            sentText.add(1);
            postEvent(payload);
        }
        // 5) after 1 minute window: random PostBack payloads till end
        else {
            const payload = makeEnvelope(userId, {
                postback: {
                    mid,
                    payload: pickPostback(),
                },
            });

            sentPostback.add(1);
            postEvent(payload);
        }

        // Pace: ~1 request per VU per 2.5 sec => 500/2.5 = 200 rps
        const spentSec = (Date.now() - loopStart) / 1000;
        const sleepSec = Math.max(0, PER_USER_INTERVAL_SEC - spentSec);
        sleep(sleepSec);
    }
}

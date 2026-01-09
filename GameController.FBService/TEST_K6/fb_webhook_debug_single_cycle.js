import http from "k6/http";
import { check, sleep } from "k6";

// -------------------- CONFIG (env overridable) --------------------
const BASE_URL = (__ENV.BASE_URL || "https://localhost:7112").replace(/\/$/, "");
const PATH = __ENV.PATH || "/api/FacebookWebhooks";
const URL = `${BASE_URL}${PATH}`;

const TIMEOUT = __ENV.TIMEOUT || "10s";
const PAGE_ID = __ENV.PAGE_ID || "PAGE_ID_TEST";
const USER_ID = __ENV.USER_ID || "TESTUSER_DEBUG_1";

// voteMinuteRange (cooldown) seconds (use your real config value)
const VOTE_MINUTE_RANGE_SEC = parseInt(__ENV.VOTE_MINUTE_RANGE_SEC || "60", 10);

// pacing inside the 1-minute text window
const TEXT_WINDOW_PACE_SEC = parseFloat(__ENV.PACE_SEC || "2.5");

// probability to send an extra postback during the 1-minute text window
// (these should be denied by cooldown and counted as WithinTimeRangeMessages)
const POSTBACK_PROB = parseFloat(__ENV.POSTBACK_PROB || "0.25");

// after the 60s window, ensure we are really past cooldown before final postback
const FINAL_POSTBACK_SAFETY_SEC = parseFloat(__ENV.FINAL_PB_SAFETY_SEC || "1.5");

// Postback payload candidates
const POSTBACK_PAYLOADS = (__ENV.POSTBACK_PAYLOADS || "ლორენცო:Yes,ბონდო:Yes,მარიამი:Yes")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);

// -------------------- OPTIONS --------------------
export const options = {
    insecureSkipTLSVerify: true,
    maxRedirects: 0,
    discardResponseBodies: true,
    vus: 1,
    iterations: 1,
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
    if (POSTBACK_PAYLOADS.length === 0) return "Default:Yes";
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

function postEvent(tag, payloadObj) {
    const t0 = Date.now();
    const res = http.post(URL, JSON.stringify(payloadObj), {
        headers: { "Content-Type": "application/json" },
        timeout: TIMEOUT,
    });
    const dt = Date.now() - t0;

    console.log(`[${tag}] status=${res.status} duration_ms=${dt}`);

    // Debug mode: force strict 200 to immediately see redirects/400s/etc.
    check(res, { "status is 200": (r) => r.status === 200 });

    return res;
}

// -------------------- TEST LOGIC --------------------
export default function () {
    // small jitter to avoid perfect sync (not super important for 1 VU, but harmless)
    sleep(Math.random() * 0.2);

    const startedAt = Date.now();
    let firstPostbackAtMs = 0;

    // 1) getstarted
    {
        const mid = `mid.k6.${USER_ID}.getstarted.${startedAt}`;
        const payload = makeEnvelope(USER_ID, {
            message: { mid, text: "getstarted" },
        });

        console.log("STEP 1: Sending getstarted");
        postEvent("getstarted", payload);
    }

    // 2) wait 20 seconds
    console.log("STEP 2: Sleeping 20s before first postback...");
    sleep(20);

    // 3) first random postback (opens cooldown window)
    {
        firstPostbackAtMs = Date.now();
        const mid = `mid.k6.${USER_ID}.postback1.${firstPostbackAtMs}`;
        const pb = pickPostback();
        const payload = makeEnvelope(USER_ID, {
            postback: { mid, payload: pb },
        });

        console.log(`STEP 3: Sending first postback payload="${pb}"`);
        postEvent("postback_20s", payload);
    }

    // 4) next 60 seconds: random 5-char texts, and sometimes extra postbacks (should be denied)
    console.log(
        `STEP 4: 60s window -> texts pace=${TEXT_WINDOW_PACE_SEC}s, extra postback prob=${POSTBACK_PROB}, cooldown=${VOTE_MINUTE_RANGE_SEC}s`
    );

    const windowStart = Date.now();
    const windowEnd = windowStart + 60 * 1000;
    let i = 0;

    while (Date.now() < windowEnd) {
        const loopStart = Date.now();

        // send garbage text
        {
            const mid = `mid.k6.${USER_ID}.text.${i}.${loopStart}`;
            const txt = randString(5);
            const payload = makeEnvelope(USER_ID, {
                message: { mid, text: txt },
            });

            console.log(`  text#${i} -> "${txt}"`);
            postEvent("text_window", payload);
        }

        // sometimes send an extra postback during cooldown window (should be denied -> WithinTimeRangeMessages++)
        {
            const elapsedSinceFirstPbSec = (Date.now() - firstPostbackAtMs) / 1000;

            // keep these extra postbacks safely inside the cooldown (avoid border where it may become accepted)
            const safeCooldownSec = Math.max(0, VOTE_MINUTE_RANGE_SEC - 2);

            if (elapsedSinceFirstPbSec < safeCooldownSec && Math.random() < POSTBACK_PROB) {
                const midPb = `mid.k6.${USER_ID}.pb_in_window.${i}.${Date.now()}`;
                const pb = pickPostback();
                const payloadPb = makeEnvelope(USER_ID, {
                    postback: { mid: midPb, payload: pb },
                });

                console.log(`  + extra postback (should be denied) -> "${pb}"`);
                postEvent("postback_in_window", payloadPb);
            }
        }

        i++;

        // pacing
        const spentSec = (Date.now() - loopStart) / 1000;
        sleep(Math.max(0, TEXT_WINDOW_PACE_SEC - spentSec));
    }

    // Ensure we're past cooldown before final postback (so it should be accepted)
    {
        const elapsedSinceFirstPbSec = (Date.now() - firstPostbackAtMs) / 1000;
        const needWaitSec = VOTE_MINUTE_RANGE_SEC + FINAL_POSTBACK_SAFETY_SEC - elapsedSinceFirstPbSec;

        if (needWaitSec > 0) {
            console.log(`Waiting extra ${needWaitSec.toFixed(2)}s to ensure cooldown is over...`);
            sleep(needWaitSec);
        }
    }

    // 5) final random postback (after cooldown -> should be accepted)
    {
        const mid = `mid.k6.${USER_ID}.postback2.${Date.now()}`;
        const pb = pickPostback();
        const payload = makeEnvelope(USER_ID, {
            postback: { mid, payload: pb },
        });

        console.log(`STEP 5: Sending final postback payload="${pb}"`);
        postEvent("postback_after_1m", payload);
    }

    const totalSec = ((Date.now() - startedAt) / 1000).toFixed(2);
    console.log(`DONE: single cycle finished in ${totalSec}s`);
}

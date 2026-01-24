import http from "k6/http";
import { check, sleep } from "k6";

// ================================================================
// fb_webhook_debug_500_users_single_cycle.js
// ------------------------------------------------
// Runs the SAME "single user cycle" in parallel for 500 users.
// Each VU == 1 unique FB user, executes exactly 1 full cycle:
//   1) send "getstarted" (message)
//   2) wait 20s
//   3) send random PostBack (vote)  -> opens cooldown window
//   4) for 60s: send random 5-char texts; sometimes extra PostBack inside cooldown (should be denied)
//   5) wait until cooldown fully passed, then send final PostBack (should be accepted)
// ================================================================

// -------------------- CONFIG (env overridable) --------------------
const BASE_URL = (__ENV.BASE_URL || "https://localhost:7112").replace(/\/$/, "");
const PATH = __ENV.PATH || "/api/FacebookWebhooks";
const URL = `${BASE_URL}${PATH}`;

const TIMEOUT = __ENV.TIMEOUT || "10s";
const PAGE_ID = __ENV.PAGE_ID || "PAGE_ID_TEST";

// Unique user id per VU (Facebook sender id)
const USER_PREFIX = __ENV.USER_PREFIX || "TESTUSER_";
const USER_ID_PAD = parseInt(__ENV.USER_ID_PAD || "0", 10); // e.g. 4 => TESTUSER_0001

// voteMinuteRange (cooldown) seconds (use your real config value)
const VOTE_MINUTE_RANGE_SEC = parseInt(__ENV.VOTE_MINUTE_RANGE_SEC || "60", 10);

// pacing inside the 1-minute text window (smaller = more texts)
const TEXT_WINDOW_PACE_SEC = parseFloat(__ENV.PACE_SEC || "2.5");

// probability to send an extra postback during the 1-minute text window
// (these should be denied by cooldown and counted as WithinTimeRangeMessages)
const POSTBACK_PROB = parseFloat(__ENV.POSTBACK_PROB || "0.25");

// after the 60s window, ensure we are really past cooldown before final postback
const FINAL_POSTBACK_SAFETY_SEC = parseFloat(__ENV.FINAL_PB_SAFETY_SEC || "1.5");

// Add a small start jitter to avoid a perfect "thundering herd" at t=0
const START_JITTER_MAX_SEC = parseFloat(__ENV.START_JITTER_MAX_SEC || "1.0");

// Logs (VERY IMPORTANT: keep off for 500 VUs, logs kill performance)
const VERBOSE = (__ENV.VERBOSE || "0") === "1";

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
  scenarios: {
    single_cycle_500_users: {
      executor: "per-vu-iterations",
      vus: parseInt(__ENV.VUS || "500", 10),
      iterations: 1,
      maxDuration: __ENV.MAX_DURATION || "3m30s",
    },
  },
  // thresholds: {
  //   http_req_failed: ["rate<0.01"],
  //   http_req_duration: ["p(95)<200"],
  // },
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

function padNum(n, width) {
  if (!width || width <= 0) return String(n);
  const s = String(n);
  return s.length >= width ? s : "0".repeat(width - s.length) + s;
}

function currentUserId() {
  // __VU is 1-based
  const suffix = USER_ID_PAD > 0 ? padNum(__VU, USER_ID_PAD) : String(__VU);
  return `${USER_PREFIX}${suffix}`;
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
  const res = http.post(URL, JSON.stringify(payloadObj), {
    headers: { "Content-Type": "application/json" },
    timeout: TIMEOUT,
  });

  check(res, { "status is 200": (r) => r.status === 200 });

  return res;
}

// -------------------- TEST LOGIC --------------------
export default function () {
  const USER_ID = currentUserId();

  // Start jitter
  if (START_JITTER_MAX_SEC > 0) sleep(Math.random() * START_JITTER_MAX_SEC);

  const startedAt = Date.now();
  let firstPostbackAtMs = 0;

  // 1) getstarted
  {
    const mid = `mid.k6.${USER_ID}.getstarted.${startedAt}`;
    const payload = makeEnvelope(USER_ID, { message: { mid, text: "getstarted" } });
    if (VERBOSE) console.log(`[${USER_ID}] STEP1 getstarted`);
    postEvent(payload);
  }

  // 2) wait 20 seconds
  sleep(20);

  // 3) first random postback (opens cooldown window)
  {
    firstPostbackAtMs = Date.now();
    const mid = `mid.k6.${USER_ID}.postback1.${firstPostbackAtMs}`;
    const pb = pickPostback();
    const payload = makeEnvelope(USER_ID, { postback: { mid, payload: pb } });
    if (VERBOSE) console.log(`[${USER_ID}] STEP3 postback1="${pb}"`);
    postEvent(payload);
  }

  // 4) next 60 seconds: random 5-char texts, and sometimes extra postbacks (should be denied)
  const windowStart = Date.now();
  const windowEnd = windowStart + 60 * 1000;
  let i = 0;

  while (Date.now() < windowEnd) {
    const loopStart = Date.now();

    // send garbage text
    {
      const mid = `mid.k6.${USER_ID}.text.${i}.${loopStart}`;
      const txt = randString(5);
      const payload = makeEnvelope(USER_ID, { message: { mid, text: txt } });
      if (VERBOSE) console.log(`[${USER_ID}] text#${i} "${txt}"`);
      postEvent(payload);
    }

    // sometimes send an extra postback during cooldown window (should be denied -> WithinTimeRangeMessages++)
    {
      const elapsedSinceFirstPbSec = (Date.now() - firstPostbackAtMs) / 1000;
      const safeCooldownSec = Math.max(0, VOTE_MINUTE_RANGE_SEC - 2);

      if (elapsedSinceFirstPbSec < safeCooldownSec && Math.random() < POSTBACK_PROB) {
        const midPb = `mid.k6.${USER_ID}.pb_in_window.${i}.${Date.now()}`;
        const pb = pickPostback();
        const payloadPb = makeEnvelope(USER_ID, { postback: { mid: midPb, payload: pb } });
        if (VERBOSE) console.log(`[${USER_ID}] +extra postback (denied) "${pb}"`);
        postEvent(payloadPb);
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
    if (needWaitSec > 0) sleep(needWaitSec);
  }

  // 5) final random postback (after cooldown -> should be accepted)
  {
    const mid = `mid.k6.${USER_ID}.postback2.${Date.now()}`;
    const pb = pickPostback();
    const payload = makeEnvelope(USER_ID, { postback: { mid, payload: pb } });
    if (VERBOSE) console.log(`[${USER_ID}] STEP5 postback2="${pb}"`);
    postEvent(payload);
  }

  if (VERBOSE) {
    const totalSec = ((Date.now() - startedAt) / 1000).toFixed(2);
    console.log(`[${USER_ID}] DONE in ${totalSec}s`);
  }
}

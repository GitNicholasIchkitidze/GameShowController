import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    insecureSkipTLSVerify: true,
    noConnectionReuse: false,
    maxRedirects: 0,
    scenarios: {
        ramp_to_300: {
            executor: 'ramping-arrival-rate',
            startRate: 50,
            timeUnit: '1s',
            preAllocatedVUs: 200,
            maxVUs: 2000,
            stages: [
                { target: 300, duration: '30s' }, // ramp up
                { target: 300, duration: '60s' }, // hold
            ],
        },
        burst_1000: {
            executor: 'ramping-arrival-rate',
            startTime: '95s',
            startRate: 300,
            timeUnit: '1s',
            preAllocatedVUs: 500,
            maxVUs: 4000,
            stages: [
                { target: 1000, duration: '10s' }, // ramp
                { target: 1000, duration: '20s' }, // hold burst
                { target: 300, duration: '10s' },  // back down
            ],
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<200'], // webhook ACK ideally fast
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5224';
//const BASE_URL = 'http://localhost:5224';
const URL = `${BASE_URL}/api/FacebookWebhooks`;

function makePayload(mid) {
    // Postback payload passes your controller filter (messageType = "postback")
    const payloads = ["ლორენცო:Yes", "ბონდო:Yes", "მარიამი:Yes"];
    const randomPayload = payloads[Math.floor(Math.random() * payloads.length)];
    return {
        object: "page",
        entry: [
            {
                id: "PAGE_ID_TEST",
                time: Date.now(),
                messaging: [
                    {
                        sender: { id: "USER_" + (__VU % 10000) }, // many users
                        recipient: { id: "PAGE_ID_TEST" },
                        timestamp: Date.now(),
                        postback: {
                            mid: mid,
                            payload: randomPayload
                        }
                    }
                ]
            }
        ]
    };
}

export default function () {
    const mid = `mid.k6.${__VU}.${__ITER}.${Date.now()}`;

    const payload = makePayload(mid);

    const res = http.post(URL, JSON.stringify(payload), {
        headers: { 'Content-Type': 'application/json' },
        timeout: '5s'
    });

    check(res, {
        'status is 200': (r) => r.status === 200,
        'status is 307/308': (r) => r.status === 307 || r.status === 308,
        'status is 400': (r) => r.status === 400,
    });

    // tiny sleep to reduce CPU busy-loop
    sleep(0.001);
}

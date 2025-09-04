// Countdown.js
const ws = new WebSocket('ws://192.168.0.3:5000/ws-casparcg');
let countdownInterval;
let endTime; // მიზნობრივი დასრულების დრო
let isPaused = false;
let pauseTime = 0;

const testUpdateMessage = {
    //type: "update_question",
    //Question: "რამდენი ფეხი აქვს ობობას?",
    //QuestionImage: "", // შეგიძლიათ აქ სურათის URL ჩაწეროთ
    type: "pause_countdown",
    endTime: 0 
    
};

const testEvent = {
    data: JSON.stringify(testUpdateMessage)
};

ws.onopen = () => {
    console.log('WebSocket connection established to C# server from Countdown.');
    const registrationMessage = JSON.stringify({
        type: 'register',
        templateName: 'Countdown',
    });
    ws.send(registrationMessage);

    console.info(
        'Registration Request Sent. ' +
        registrationMessage
    );
};

ws.onmessage = (event) => {
    console.log('Received data for Countdown: ' + event.data);

    try {
        const jsonData = JSON.parse(event.data);

        if (jsonData.type === 'Start') {
            // მივიღოთ დასრულების დრო სერვერიდან
            // endTime არის მილიწამებში (UNIX timestamp)
            endTime = jsonData.endTime;
            isPaused = false;
            pauseTime = 0;

            // დავამატოთ ლოგიკა, რომელიც ითვლის დროს
            startCountdownLogic();
        } else if (jsonData.type === 'Pause') {
            // პაუზის ლოგიკა
            if (!isPaused) {
                isPaused = true;
                pauseTime = Date.now(); // შევინახოთ პაუზის დრო
                clearInterval(countdownInterval);
                console.log('Countdown paused.');
            }
        } else if (jsonData.type === 'Resume') {
            // განახლების ლოგიკა
            if (isPaused) {
                isPaused = false;
                // გამოვთვალოთ გასული დრო პაუზის დროს და გამოვაკლოთ endTime-ს
                const timePassedDuringPause = Date.now() - pauseTime;
                endTime += timePassedDuringPause;

                startCountdownLogic();
                console.log('Countdown resumed.');
            }
        } else {
            countdownTextElement.innerText = "0"
        }
    } catch (e) {
        console.error('Error parsing JSON data or updating elements:', e);
    }
};


window.update = function (data) {
    console.log('update');

};
function startCountdownLogic() {
    // გავაჩეროთ წინა კაუნტდაუნი, თუ არსებობს
    if (countdownInterval) {
        clearInterval(countdownInterval);
    }

    const countdownTextElement = document.getElementById('countdown-text');

    if (!countdownTextElement) return;

    // დავიწყოთ ახალი კაუნტდაუნი
    countdownInterval = setInterval(() => {
        const now = Date.now();
        const timeLeft = Math.max(0, Math.floor((endTime - now) / 1000)); // წამებში

        countdownTextElement.innerText = timeLeft;

        if (timeLeft <= 0) {
            clearInterval(countdownInterval);
            countdownTextElement.innerText = '0';
            console.log('Countdown finished.');
            // აქ შეგიძლიათ დაამატოთ ლოგიკა, მაგალითად, ელემენტის დამალვა
        }
    }, 100); // 100ms განახლება უფრო ზუსტია
}

ws.onclose = () => {
    console.log('WebSocket connection closed.');
    if (countdownInterval) {
        clearInterval(countdownInterval);
    }
};

ws.onerror = (error) => {
    console.error('WebSocket error:', error);
};
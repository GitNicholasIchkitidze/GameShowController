// CountDown.js
const ws = new WebSocket('ws://localhost:5000/ws-casparcg');
let countdownInterval;
let endTime; // მიზნობრივი დასრულების დრო
let isPaused = false;
let pauseTime = 0;

ws.onopen = () => {
    console.log('WebSocket connection established to C# server from CountDown.');
    const registrationMessage = JSON.stringify({
        type: 'register',
        templateName: 'CountDown',
    });
    ws.send(registrationMessage);
};

ws.onmessage = (event) => {
    console.log('Received data for CountDown: ' + event.data);

    try {
        const jsonData = JSON.parse(event.data);

        if (jsonData.type === 'start_countdown') {
            // მივიღოთ დასრულების დრო სერვერიდან
            // endTime არის მილიწამებში (UNIX timestamp)
            endTime = jsonData.endTime;
            isPaused = false;
            pauseTime = 0;

            // დავამატოთ ლოგიკა, რომელიც ითვლის დროს
            startCountDownLogic();
        } else if (jsonData.type === 'pause_countdown') {
            // პაუზის ლოგიკა
            if (!isPaused) {
                isPaused = true;
                pauseTime = Date.now(); // შევინახოთ პაუზის დრო
                clearInterval(countdownInterval);
                console.log('CountDown paused.');
            }
        } else if (jsonData.type === 'resume_countdown') {
            // განახლების ლოგიკა
            if (isPaused) {
                isPaused = false;
                // გამოვთვალოთ გასული დრო პაუზის დროს და გამოვაკლოთ endTime-ს
                const timePassedDuringPause = Date.now() - pauseTime;
                endTime += timePassedDuringPause;

                startCountDownLogic();
                console.log('CountDown resumed.');
            }
        }
    } catch (e) {
        console.error('Error parsing JSON data or updating elements:', e);
    }
};

function startCountDownLogic() {
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
            console.log('CountDown finished.');
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
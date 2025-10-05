const ws = new WebSocket('ws://192.168.0.4:5000/ws-casparcg');
let flipInterval;
let currentFlipState = 'text';

// CSS ცვლადების წაკითხვა
const getCssVar = (varName) => {
    return getComputedStyle(document.documentElement).getPropertyValue(varName);
};

const showDuration = parseFloat(getCssVar('--show-duration'));
const hideDuration = parseFloat(getCssVar('--hide-duration'));
const textSwapDuration = parseFloat(getCssVar('--text-swap-duration'));
const flipLogo = parseInt(getCssVar('--flip-logo'));
const flipLogoInterval = parseFloat(getCssVar('--flip-logo-interval'));

ws.onopen = () => {
    console.log('WebSocket connection established from Lower Third GSAP.');

    // რეგისტრაციის მოთხოვნის გაგზავნა
    const registrationMessage = JSON.stringify({
        type: 'register',
        templateName: 'tPs1',
    });
    ws.send(registrationMessage);
    console.info('Registration Request Sent: ' + registrationMessage);
};

ws.onmessage = (event) => {
    console.log('Received data for Lower Third GSAP: ' + event.data);

    try {
        const jsonData = JSON.parse(event.data);

        if (jsonData.Status === 'show') {
            updateText(jsonData);
            showAnimation();
        } else if (jsonData.Status === 'hide') {
            hideAnimation();
        } else if (jsonData.Status === 'update') {
            updateText(jsonData);
        } else if (jsonData.Status === 'nextSecondLine') {
            nextSecondLine(jsonData.SecondLine);
        }
    } catch (e) {
        console.error('Error parsing JSON data or updating elements:', e);
    }
};

function updateText(data) {
    document.getElementById('headline-text').innerText = data.Headline || '';
    document.getElementById('second-line-text').innerText = data.SecondLine || '';
    document.getElementById('breaking-news-text').innerText = data.BreakingNews || '';
    document.getElementById('breaking-news-logo').src = getCssVar('--logo-url').replace(/url\(['"]?([^'"]+)['"]?\)/, '$1');

    if (flipLogo === 1) {
        startLogoFlip();
    } else {
        stopLogoFlip();
        gsap.to("#breaking-news-text", { duration: 0.5, opacity: 1, ease: "power2.out" });
        gsap.to("#breaking-news-logo", { duration: 0.5, opacity: 0, ease: "power2.out" });
    }
}

function startLogoFlip() {
    stopLogoFlip();
    const breakingNewsText = document.getElementById('breaking-news-text');
    const breakingNewsLogo = document.getElementById('breaking-news-logo');

    flipInterval = setInterval(() => {
        if (currentFlipState === 'text') {
            gsap.to(breakingNewsText, { duration: 0.5, opacity: 0, ease: "power2.out" });
            gsap.to(breakingNewsLogo, { duration: 0.5, opacity: 1, ease: "power2.out" });
            currentFlipState = 'logo';
        } else {
            gsap.to(breakingNewsLogo, { duration: 0.5, opacity: 0, ease: "power2.out" });
            gsap.to(breakingNewsText, { duration: 0.5, opacity: 1, ease: "power2.out" });
            currentFlipState = 'text';
        }
    }, flipLogoInterval * 1000);
}

function stopLogoFlip() {
    if (flipInterval) {
        clearInterval(flipInterval);
        flipInterval = null;
    }
}

function nextSecondLine(newText) {
    if (newText) {
        const secondLineElement = document.getElementById('second-line-text');
        gsap.to(secondLineElement, {
            bottom: "-100%",
            opacity: 0,
            duration: textSwapDuration,
            onComplete: () => {
                secondLineElement.innerText = newText;
                gsap.to(secondLineElement, {
                    bottom: "0",
                    opacity: 1,
                    duration: textSwapDuration
                });
            }
        });
    }
}

function showAnimation() {
    gsap.timeline()
        .to("#container", { duration: showDuration, x: "0%", ease: "power2.out" })
        .to("#breaking-news-box", { duration: showDuration, x: "0%", opacity: 1, ease: "power2.out" }, `-=${showDuration - 0.1}`)
        .to("#main-box", { duration: showDuration, x: "0%", opacity: 1, ease: "power2.out" }, `-=${showDuration - 0.2}`);
}

function hideAnimation() {
    stopLogoFlip();
    gsap.timeline()
        .to("#container", { duration: hideDuration, x: "100%", ease: "power2.in" });
}

window.play = function () { };
window.stop = function () { };
window.update = function () { };
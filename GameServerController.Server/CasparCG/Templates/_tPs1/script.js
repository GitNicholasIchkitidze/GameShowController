const ws = new WebSocket('ws://192.168.0.4:5000/ws-casparcg');
let flipInterval;

const getCssVar = (varName) => {
    return getComputedStyle(document.documentElement).getPropertyValue(varName);
};

const showDuration = parseFloat(getCssVar('--show-duration'));
const hideDuration = parseFloat(getCssVar('--hide-duration'));

ws.onopen = () => {
    console.log('WebSocket connection established from Lower Third GSAP.');
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
    document.getElementById('top-text').innerText = data.TopText || '';
    document.getElementById('headline-text').innerText = data.Headline || '';
    document.getElementById('second-line-text').innerText = data.SecondLine || '';
    document.getElementById('breaking-news-text').innerText = data.BreakingNews || '';
    document.getElementById('logo-image').src = getCssVar('--logo-url').replace(/url\(['"]?([^'"]+)['"]?\)/, '$1');

    const flipLogo = parseInt(getCssVar('--flip-logo'));
    if (flipLogo === 1) {
        startLogoFlip();
    } else {
        stopLogoFlip();
        gsap.to("#breaking-news-text", { opacity: 1 });
        gsap.to("#logo-image", { opacity: 0 });
    }
}

function nextSecondLine(newText) {
    // ანიმაცია იგივე რჩება
    const secondLineElement = document.getElementById('second-line-text');
    gsap.to(secondLineElement, {
        opacity: 0,
        y: 20,
        duration: 0.3,
        onComplete: () => {
            secondLineElement.innerText = newText;
            gsap.to(secondLineElement, { opacity: 1, y: 0, duration: 0.5 });
        }
    });
}

function startLogoFlip() {
    stopLogoFlip();
    const breakingNewsText = document.getElementById('breaking-news-text');
    const logoImage = document.getElementById('logo-image');
    const flipLogoInterval = parseFloat(getCssVar('--flip-logo-interval')) * 1000;

    gsap.to(breakingNewsText, { opacity: 1, duration: 0.5 });
    gsap.to(logoImage, { opacity: 0, duration: 0.5 });

    flipInterval = setInterval(() => {
        if (gsap.getProperty(breakingNewsText, "opacity") === 1) {
            gsap.to(breakingNewsText, { opacity: 0, duration: 0.5 });
            gsap.to(logoImage, { opacity: 1, duration: 0.5 });
        } else {
            gsap.to(logoImage, { opacity: 0, duration: 0.5 });
            gsap.to(breakingNewsText, { opacity: 1, duration: 0.5 });
        }
    }, flipLogoInterval);
}

function stopLogoFlip() {
    if (flipInterval) {
        clearInterval(flipInterval);
        flipInterval = null;
    }
}

function showAnimation() {
    gsap.timeline()
        .to("#container", { duration: showDuration, x: "0%", ease: "power2.out" });
}

function hideAnimation() {
    stopLogoFlip();
    gsap.timeline()
        .to("#container", { duration: hideDuration, x: "100%", ease: "power2.in" });
}

window.play = function () { };
window.stop = function () { };
window.update = function () { };
const ws = new WebSocket('ws://192.168.0.4:5000/ws-casparcg');

ws.onopen = () => {
    console.log('WebSocket connection established from Lower Third GSAP.');
    const registrationMessage = JSON.stringify({
        type: 'register',
        templateName: 'tPs1',
    });
    ws.send(registrationMessage);
    console.info('Registration Request Sent. ' + registrationMessage);
};

ws.onmessage = (event) => {
    console.log('Received data for Lower Third GSAP: ' + event.data);

    try {
        const jsonData = JSON.parse(event.data);

        // Update text fields
        document.getElementById('breaking-news-box').innerText = jsonData.BreakingNews || 'BREAKING NEWS';
        document.getElementById('headline-text').innerText = jsonData.Headline || '';
        document.getElementById('second-line-text').innerText = jsonData.SecondLine || '';

        // Handle animation based on data
        if (jsonData.Status === 'show') {
            gsap.timeline()
                .to("#container", { duration: 0.5, x: "0%", ease: "power2.out" })
                .to("#breaking-news-box", { duration: 0.5, x: "0%", opacity: 1, ease: "power2.out" }, "-=0.2")
                .to("#main-box", { duration: 0.5, x: "0%", opacity: 1, ease: "power2.out" }, "-=0.3");
        } else if (jsonData.Status === 'hide') {
            gsap.timeline()
                .to("#container", { duration: 0.5, x: "100%", ease: "power2.in" });
        }
    } catch (e) {
        console.error('Error parsing JSON data or updating elements:', e);
    }
};

ws.onclose = () => {
    console.log('WebSocket connection closed.');
};

ws.onerror = (error) => {
    console.error('WebSocket error:', error);
};

// CasparCG-ის სტანდარტული ფუნქციები ახლა არაფერს აკეთებს, რადგან WebSocket გამოიყენება.
window.play = function () { };
window.stop = function () { };
window.update = function () { };
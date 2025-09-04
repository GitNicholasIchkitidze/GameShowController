// question-lowerthird-script.js
const ws = new WebSocket('ws://localhost:5000/ws-casparcg');

ws.onopen = () => {
    console.log('WebSocket connection established to C# server from QuestionFull.');
    const registrationMessage = JSON.stringify({
        type: 'register',
        templateName: 'QuestionFull',
    });
    ws.send(registrationMessage);
};

ws.onmessage = (event) => {
    console.log('Received data for QuestionFull: ' + event.data);

    try {
        const jsonData = JSON.parse(event.data);

        // Update the question text
        const questionTextElement =
            document.getElementById('question-text');
        if (questionTextElement) {
            questionTextElement.innerText = jsonData.Question;
        }

        // Update the answers based on the new HTML structure
        const answersContainer =
            document.getElementById('answers-container');
        if (answersContainer) {
            answersContainer.innerHTML = '';
            if (jsonData.Answers) {
                jsonData.Answers.forEach((answer, index) => {
                    const answerItem = document.createElement('div');
                    answerItem.className = 'answer-item';

                    // Create a child div to un-skew the text
                    const answerText = document.createElement('div');
                    answerText.innerText = answer;
                    answerItem.appendChild(answerText);

                    answersContainer.appendChild(answerItem);
                });
            }
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

// This function is not used with WebSockets, but can be useful for debugging
// function onData(xmlData) {
//   // Implementation to parse old CasparCG data
// }
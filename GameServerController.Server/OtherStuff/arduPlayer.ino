#include <SPI.h>
#include <Ethernet.h>
#include <ArduinoJson.h>

// --- ქსელის პარამეტრები ---
const char* serverHost = "192.168.210.61";
const int serverPort = 5005;
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };
IPAddress staticIP(192, 168, 210, 55);
IPAddress gateway(192, 168, 210, 1);

// --- ღილაკების პინები ---
const int buttonPin1 = 2;
const int buttonPin2 = 3;
const int buttonPin3 = 18;
const int buttonPin4 = 19;

// --- გლობალური ცვლადები ---
volatile bool buttonsEnabled = true;
String answers[4] = { "Answer 1", "Answer 2", "Answer 3", "Answer 4" };
volatile unsigned long lastDebounceTime[4] = { 0, 0, 0, 0 };
const unsigned long debounceDelay = 200;
EthernetClient client;

// ეს ცვლადი აკონტროლებს, მუშაობს თუ არა კოდი სიმულაციის რეჟიმში
// false - ფიზიკური ღილაკები, true - სერიული პორტის სიმულაცია
const bool simulationMode = true;

// --- დამხმარე ფუნქციების პროტოტიპები ---
void sendAnswer(String answer);
void disableButtons();
void enableButtons();

// --- Interrupt მომსახურების რუტინები ---
void button1ISR() {
    if (buttonsEnabled && (millis() - lastDebounceTime[0]) > debounceDelay) {
        lastDebounceTime[0] = millis();
        sendAnswer(answers[0]);
        disableButtons();
    }
}

void button2ISR() {
    if (buttonsEnabled && (millis() - lastDebounceTime[1]) > debounceDelay) {
        lastDebounceTime[1] = millis();
        sendAnswer(answers[1]);
        disableButtons();
    }
}

void button3ISR() {
    if (buttonsEnabled && (millis() - lastDebounceTime[2]) > debounceDelay) {
        lastDebounceTime[2] = millis();
        sendAnswer(answers[2]);
        disableButtons();
    }
}

void button4ISR() {
    if (buttonsEnabled && (millis() - lastDebounceTime[3]) > debounceDelay) {
        lastDebounceTime[3] = millis();
        sendAnswer(answers[3]);
        disableButtons();
    }
}

// --- SETUP ფუნქცია ---
void setup() {
    Serial.begin(115200);

    // პინების დაყენება
    pinMode(buttonPin1, INPUT_PULLUP);
    pinMode(buttonPin2, INPUT_PULLUP);
    pinMode(buttonPin3, INPUT_PULLUP);
    pinMode(buttonPin4, INPUT_PULLUP);

    if (!simulationMode) {
        // Interrupt-ების მიმაგრება ფიზიკური ღილაკებისთვის
        attachInterrupt(digitalPinToInterrupt(buttonPin1), button1ISR, FALLING);
        attachInterrupt(digitalPinToInterrupt(buttonPin2), button2ISR, FALLING);
        attachInterrupt(digitalPinToInterrupt(buttonPin3), button3ISR, FALLING);
        attachInterrupt(digitalPinToInterrupt(buttonPin4), button4ISR, FALLING);
    }

    // Ethernet-ის ინიციალიზაცია სტატიკური IP-ს გამოყენებით
    Serial.println("Initializing Ethernet with static IP...");
    Ethernet.begin(mac, staticIP, gateway);
    
    delay(1000);
    Serial.print("Local IP: ");
    Serial.println(Ethernet.localIP());

    Serial.println("Arduino is ready.");
    if (simulationMode) {
        Serial.println("Using simulation mode. Type 1, 2, 3 or 4 and press Enter to send an answer.");
    }
    else {
        Serial.println("Using physical buttons.");
    }

    enableButtons();
}

// --- LOOP ფუნქცია ---
void loop() {
    if (simulationMode) {
        if (Serial.available() > 0) {
            String input = Serial.readStringUntil('\n');
            input.trim();
            int buttonNumber = input.toInt();

            if (buttonsEnabled) {
                if (buttonNumber >= 1 && buttonNumber <= 4) {
                    sendAnswer(answers[buttonNumber - 1]);
                    //disableButtons(); // პასუხის გაგზავნის შემდეგ ღილაკები ითიშება
                }
                else {
                    Serial.println("Invalid input. Please enter a number from 1 to 4.");
                }
            }
        }
    }
}

// --- დამხმარე ფუნქციები ---
void sendAnswer(String answer) {
    if (!simulationMode) {
        // Interrupt-ების გამორთვა მონაცემების გაგზავნისას
        noInterrupts();
    }

    Serial.println("Attempting to connect to WinForms server...");
    
    // EthernetClient-ის ობიექტი უნდა იყოს შექმნილი ფუნქციის შიგნით
    // რათა თავიდან ავიცილოთ პოტენციური კავშირის პრობლემები
    EthernetClient currentClient;

    if (currentClient.connect(serverHost, serverPort)) {
        Serial.println("Connected to WinForms server, sending answer...");

        DynamicJsonDocument doc(256);
        doc["answer"] = answer;
        String output;
        serializeJson(doc, output);
        
        currentClient.print(output);
        
        Serial.println("Answer sent: " + output);
        currentClient.stop();
    } else {
        Serial.println("Connection to WinForms server failed!");
    }

    if (!simulationMode) {
        // Interrupt-ების ჩართვა
        interrupts();
    }
}

void disableButtons() {
    buttonsEnabled = false;
    Serial.println("Buttons are now disabled.");
}

void enableButtons() {
    buttonsEnabled = true;
    Serial.println("Buttons are now enabled. Waiting for a response.");
}
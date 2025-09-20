#include <SPI.h>
#include <Ethernet.h>
#include <WebSocketsClient.h>
#include <ArduinoJson.h>

// --- ქსელის პარამეტრები ---
// შეცვალეთ ეს პარამეტრები თქვენი ქსელის კონფიგურაციის მიხედვით
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED }; // თქვენი Ethernet Shield-ის MAC მისამართი
IPAddress serverIP(192, 168, 1, 100); // SignalR სერვერის ლოკალური IP მისამართი
const int serverPort = 80;
const char* clientName = "ArduinoPlayer1"; // მოთამაშის უნიკალური სახელი
String hubPath = String("/gamehub?name=") + clientName;

WebSocketsClient webSocketClient;

// --- ღილაკების პინები ---
// გამოიყენება Interrupt პინები Arduino Mega-ზე
const int buttonPin1 = 2;
const int buttonPin2 = 3;
const int buttonPin3 = 18;
const int buttonPin4 = 19;

// --- გლობალური ცვლადები ---
volatile bool buttonsEnabled = false; // ღილაკების აქტიური სტატუსი
String answers[4]; // კლიენტზე შემოსული პასუხები

// Debounce ცვლადები
volatile unsigned long lastDebounceTime[4] = {0, 0, 0, 0};
const unsigned long debounceDelay = 200; // Debounce-ის შეფერხება მილიწამებში

// Reconnect ცვლადები
const unsigned long reconnectDelay = 5000; // ხელახალი დაკავშირების მცდელობა 5 წამში ერთხელ
unsigned long lastReconnectAttempt = 0;

// --- Interrupt მომსახურების რუტინები (ISRs) ---
// თითოეული ღილაკის დაჭერა იწვევს შესაბამისი ფუნქციის გამოძახებას
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

// --- Setup ფუნქცია ---
void setup() {
  Serial.begin(115000);
  
  // პინების დაყენება
  pinMode(buttonPin1, INPUT_PULLUP);
  pinMode(buttonPin2, INPUT_PULLUP);
  pinMode(buttonPin3, INPUT_PULLUP);
  pinMode(buttonPin4, INPUT_PULLUP);

  // Interrupt-ების მიმაგრება
  attachInterrupt(digitalPinToInterrupt(buttonPin1), button1ISR, FALLING);
  attachInterrupt(digitalPinToInterrupt(buttonPin2), button2ISR, FALLING);
  attachInterrupt(digitalPinToInterrupt(buttonPin3), button3ISR, FALLING);
  attachInterrupt(digitalPinToInterrupt(buttonPin4), button4ISR, FALLING);

  // Ethernet-ის ინიციალიზაცია
  Serial.println("Initializing Ethernet...");
  if (Ethernet.begin(mac) == 0) {
    Serial.println("Failed to configure Ethernet using DHCP");
    // თუ DHCP ვერ მოხერხდა, მიანიჭეთ სტატიკური IP
    IPAddress ip(192, 168, 1, 177);
    Ethernet.begin(mac, ip);
  }
  delay(1000);
  Serial.print("Local IP: ");
  Serial.println(Ethernet.localIP());

  // WebSocket-ის კავშირის დაწყება
  webSocketClient.begin(serverIP, serverPort, hubPath.c_str());
  webSocketClient.onEvent(webSocketEvent);
  
  disableButtons();
}

// --- Loop ფუნქცია ---
void loop() {
  webSocketClient.loop();

  // ხელახალი დაკავშირების ლოგიკა
  if (!webSocketClient.isConnected() && (millis() - lastReconnectAttempt > reconnectDelay)) {
    Serial.println("Attempting to reconnect...");
    webSocketClient.begin(serverIP, serverPort, hubPath.c_str());
    lastReconnectAttempt = millis();
  }
}

// --- WebSocket-ის მოვლენების დამმუშავებელი ---
void webSocketEvent(WStype_t type, uint8_t * payload, size_t length) {
  switch (type) {
    case WStype_CONNECTED:
      Serial.println("WebSocket Connected! Sending SignalR handshake...");
      // SignalR-ის პროტოკოლის მოთხოვნა
      webSocketClient.sendTXT("{\"protocol\":\"json\",\"version\":1}\x1e");
      break;

    case WStype_TEXT:
      // შეტყობინების დასასრულის სიმბოლოს (\x1e) მოშორება
      if (payload[length-1] == '\x1e') {
        payload[length-1] = '\0';
      }

      DynamicJsonDocument doc(1024);
      DeserializationError error = deserializeJson(doc, payload);

      if (error) {
        Serial.print("JSON deserialize failed: ");
        Serial.println(error.c_str());
        return;
      }

      // შეტყობინების ტიპის შემოწმება 'target' ველით
      if (doc.containsKey("target")) {
        String target = doc["target"];

        if (target == "ReceiveQuestion") {
          // კითხვის მიღებისას
          String question = doc["arguments"][0]["Question"];
          JsonArray answersArray = doc["arguments"][0]["Answers"];

          Serial.println("Received Question: " + question);

          int i = 0;
          for (JsonVariant v : answersArray) {
            answers[i] = v.as<String>();
            Serial.println("Answer " + String(i + 1) + ": " + answers[i]);
            i++;
          }
          enableButtons();
        } else if (target == "UpdateRegistrationStatus") {
          // რეგისტრაციის სტატუსის მიღებისას
          bool registered = doc["arguments"][0];
          if (registered) {
            Serial.println("Player Registered Successfully!");
          } else {
            Serial.println("Player Registration Failed!");
          }
        }
      }
      break;
    
    case WStype_DISCONNECTED:
      Serial.println("Disconnected. Will try to reconnect in a moment...");
      disableButtons();
      break;
  }
}

// --- დამხმარე ფუნქციები ---
void sendAnswer(String answer) {
  // Interrupt-ების დროებით გათიშვა
  noInterrupts();
  
  DynamicJsonDocument doc(256);
  doc["target"] = "SubmitAnswer";
  JsonArray args = doc.createNestedArray("arguments");
  args.add(answer);

  String output;
  serializeJson(doc, output);
  
  // შეტყობინების გაგზავნა SignalR-ის ფორმატში
  webSocketClient.sendTXT(output + "\x1e"); 
  Serial.println("Sent Answer: " + answer);
  
  // Interrupt-ების ხელახლა ჩართვა
  interrupts();
}

void disableButtons() {
  buttonsEnabled = false;
  Serial.println("Buttons are now disabled.");
}

void enableButtons() {
  buttonsEnabled = true;
  Serial.println("Buttons are now enabled. Waiting for a response.");
}
#include <SPI.h>
#include <Ethernet.h>

// Newer Ethernet shields have a MAC address printed on a sticker on the shield
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };
// use the numeric IP instead of the name for the server:
IPAddress server(192, 168, 1, 4);

String tempFromSenzor = "0";
double tempFromUser = 0;

const int heating = 9;
const int air = 8;
const int serverControl = 7;
const int buttonPinControlServer = 3;
const int buttonPinControlHeating = 2;

int variableControlServer = 1 ;
int variableControlHeating = 1 ;
int buttonStateControlServer = 0;         // current state of the button for server control
int buttonStateControlHeating = 0; // current state of the button for heating control


// Set the static IP address to use if the DHCP fails to assign
IPAddress ip(192, 168, 0, 177);

// Initialize the Ethernet client library
// with the IP address and port of the server
// that you want to connect to (port 80 is default for HTTP):
EthernetClient client;

// Open connection to the HTTP server
bool connect(IPAddress server) {
  Serial.print("Connect to ");
  Serial.println(server);

  bool ok = client.connect(server, 90);

  Serial.println(ok ? "Connected" : "Connection Failed!");
  return ok;
}

// Close the connection with the HTTP server
void disconnect() {
  Serial.println("Disconnect");
  client.stop();
}

// Pause for a 50 seconds
void wait() {
  Serial.println("Wait 15 seconds");
  delay(15000);
}

// Send the HTTP GET request to the server
bool sendRequest() {
  Serial.println("GET Value");
  //tempFromSenzor = ( 5.0 * analogRead(0) * 100.0) / 1024.0;
  client.println("GET /api/livingrooom?tempFromSenzor=" + tempFromSenzor + " HTTP/1.1");
  client.println("Host: www.google.com");
  delay(1000);
  client.println("Connection: close");
  delay(1000);
  client.println();
  delay(1000);
  return true;
}

void setup() {
  pinMode(heating, OUTPUT);
  pinMode(air, OUTPUT);
  pinMode(serverControl, OUTPUT);
  pinMode(buttonPinControlServer, INPUT);
  pinMode(buttonPinControlHeating, INPUT);
  
  // Open serial communications and wait for port to open:
  Serial.begin(9600);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }

  // start the Ethernet connection:
  if (Ethernet.begin(mac) == 0) {
    Serial.println("Failed to configure Ethernet using DHCP");
    // try to congifure using IP address instead of DHCP:
    Ethernet.begin(mac, ip);
  }
  // give the Ethernet shield a second to initialize:
  delay(1000);
  Serial.println("Ethernet and Serial - Setup - OK");
}


void loop() {
  // if there are incoming bytes available
  // from the server, read them and print them:
  Serial.println("Getting data");
  String inputString = "";
  tempFromSenzor = ( 5.0 * analogRead(0) * 100.0) / 1024.0;
  if (connect(server)) {
    if (sendRequest()) {
      Serial.println("Request send");
      // give time to take response
      delay(3000);
      Serial.println(client.available());
      while (client.available()) {
        char c = client.read();
        inputString += c;
        Serial.print(c);
      }
    }
  }
  delay(1000);
  tempFromUser = (inputString.substring(163, 168)).toDouble();

  Serial.print("Current temp:");
  Serial.println(tempFromSenzor);
  Serial.print("User temp");
  Serial.println(tempFromUser);

 // Turn on / off server control
 buttonStateControlServer = digitalRead(buttonPinControlServer);
 // Turn on / off server heating
 buttonStateControlHeating = digitalRead(buttonPinControlHeating);
 
 Serial.println("Button state control server ----------");
 Serial.println(buttonStateControlServer);
 if(buttonStateControlServer == 1){
    variableControlServer++;
    if (variableControlServer == 3){
        variableControlServer = 1;
      }
  }

Serial.println("Button state control heating ----------");
Serial.println(buttonStateControlHeating);
if(buttonStateControlHeating == 1){
    variableControlHeating ++;
    if (variableControlHeating  == 3){
        variableControlHeating  = 1;
      }
  }
  
  if(variableControlServer == 1){
    digitalWrite(serverControl, HIGH);
    Serial.println("Control server ---------------------");
     if (tempFromUser > tempFromSenzor.toDouble()) {
        // turn on the heating
        Serial.println("Heating ON");
        digitalWrite(heating, HIGH);
        digitalWrite(air, LOW);
      }
      else if (tempFromUser < tempFromSenzor.toDouble()) {
        // turn the air conditioning on
        Serial.println("Air conditioning ON");
        digitalWrite(heating, LOW);
        digitalWrite(air, HIGH);
      }

  } else if(variableControlServer == 2){
      digitalWrite(serverControl, LOW);
      Serial.println("Control manual ----------------");
      if(variableControlHeating == 1){
        Serial.println("Heating ON");
        digitalWrite(heating, HIGH);
        digitalWrite(air, LOW);
      } else if (variableControlHeating == 2){
        Serial.println("Air conditioning ON");
        digitalWrite(heating, LOW);
        digitalWrite(air, HIGH);
        }
    }
  
  delay(1000);
  disconnect();
  wait();
  Serial.println("------------------------------------------");
  delay(500);
}


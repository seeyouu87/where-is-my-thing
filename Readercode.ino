/*
  read command will be sent over to RFID module
once tag is read, it will return back to serial port

 */
#include <SoftwareSerial.h>

SoftwareSerial mySerial(10, 11); // RX, TX

void setup() {
  // Open serial communications and wait for port to open:
  Serial.begin(9600);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for Leonardo only
  }

  // set the data rate for the SoftwareSerial port
  mySerial.begin(9600);
}

void loop() { // run over and over
  if (mySerial.available()) {
    //send the read command over to RFID module
    byte message[] = {0xA0,0x06,0x80,0x00,0x01,0x02,0x01,0xD6};
    mySerial.write(message, sizeof(message));
    //send the data back to serial
    if(Serial.available())
      Serial.write(mySerial.read());
  }
}
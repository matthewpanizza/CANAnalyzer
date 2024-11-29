/* 
 * Project myProject
 * Author: Your Name
 * Date: 
 * For comprehensive documentation and examples, please visit:
 * https://docs.particle.io/firmware/best-practices/firmware-template/
 */

// Include Particle Device OS APIs
#include "Particle.h"
#include "mcp_can.h"
#include "DecentralizedLV-Boards/DecentralizedLV-Boards.h"
#include "CANMessageDictionary.h"
#include <SPI.h>
#undef min
#undef max
#include <vector>
#include <algorithm>

SYSTEM_MODE(SEMI_AUTOMATIC);  //Disable Cloud Connectivity

//Info Macros
#define SOFTWARE_VERSION            "1.0"

//Software Macros
#define EEPROM_KEY1                 55              //Key 1 to identify if the EEPROM is marked for the CAN Analyzer
#define EEPROM_KEY2                 22              //Key 2 to identify if the EEPROM is marked for the CAN Analyzer
#define EEPROM_KEY1_LOCATION        0               //EEPROM index of Key 1
#define EEPROM_KEY2_LOCATION        1               //EEPROM index of Key 2
#define EEPROM_CANS_LOCATION        2               //Location in the EEPROM to hold the CAN bus speed
#define EEPROM_BAUD_LOCATION        3               //Location in the EEPROM to hold the UART Baud rate. Takes 4 bytes (addresses 3-6)
#define DEFAULT_CAN_SPEED           CAN_500KBPS     //Default CAN bus speed that is used if not able to fetch from EEPROM
#define DEFAULT_SERIAL_SPEED        115200          //Default Serial speed that is used if not able to fetch from EEPROM
#define BANK1_EMULATION_IDS         10
#define MAX_EMULATION_IDS           20


//Serial Commands
#define CAN_BAUD_COMMAND            'c'             //Takes 1 arg for new baud rate. Ex: "c 500000" sets baud rate to 500kbps
#define SERIAL_BAUD_COMMAND         'b'             //Takes 1 arg for new baud rate. EX: "b 115200" sets baud rate to 115200 baud
#define HELP_COMMAND                'h'             //Takes no args. Prints out available commands
#define FLUSH_COMMAND_1             'f'             //Takes no args. Clears all CAN IDs being emulated from Bank 1.
#define FLUSH_COMMAND_2             'g'             //Takes no args. Clears all CAN IDs being emulated from Bank 2.
#define PRINT_ALL_COMMAND           'a'             //Takes no args. Toggles printing all messages received from the CAN Bus. Will fill serial console...
#define PRINT_COMMAND               'p'             //Takes no args. Prints all CAN IDs that have been received. Has field to indicate if the values changed
#define DELTA_PRINT_COMMAND         'd'             //Takes no args. Prints all CAN IDs that have changed since the last time it was printed. Has field to indicate if the values changed
#define SINGLE_PACKET_COMMAND       's'             //Sends one CAN message. First arg is address (hex). Next 8 args are the data bytes (in hex) b0, b1, b2...
#define NEW_B1_MESSAGE_COMMAND      'm'             //Add continuously-sent message to Bank 1. First arg is address (hex). Next 8 args are the data bytes (in hex) b0, b1, b2...
#define NEW_B2_MESSAGE_COMMAND      'n'             //Add Continuously-sent message to Bank 2. First arg is address (hex). Next 8 args are the data bytes (in hex) b0, b1, b2...
#define COMBO_LOOP_COMMAND          'k'             //Sends messages across a range of CAN addresses and sends all data combinations on all bytes (0-255)
#define DATA_LOOP_COMMAND           'l'             //Sends messages on one address and ramps data value on masked bytes from 0-255.
#define ADDR_LOOP_COMMAND           'o'             //Sends messages with the same data across a range of CAN addresses.
#define VERSION_COMMAND             'v'             //Prints out the version of this firmware. Use this to determine supported commands
#define APPMODE_COMMAND             'i'             //Switch to App Mode, which makes reply messages have comma delimited codes
#define REGMODE_COMMAND             'j'             //Switch to Standard Mode, which makes reply messages more user-readable
#define QUERY_LIST_COMMAND          'q'             //Prints out all messages in the message bank

//Hardware Macros
#define CAN0_INT        A1                          // Set INT to pin x1
#define CAN0_CS         A2                          // Set CS to pin x2


//Function prototypes
bool compareID(CANTX id1, CANTX id2);
void readEEPROM();
void updateEEPROM();
void receiveCANMessages();
void updateRGBLED();
void tmr500ms();
void button_clicked(system_event_t event, int param);
void parseCommand();
void printHelpMenu();
void emulateCANPackets();
void printLoopMessages();
void canComboLoop(bool loopAddress, bool loopData, uint32_t startAddress, uint32_t endAddress, uint8_t dataStart, uint8_t dataEnd, uint8_t dataMask, uint32_t stepSize, char loopType);

// Run the application and system concurrently in separate threads
SYSTEM_THREAD(ENABLED);

//Software timers
Timer LEDTimer(500, tmr500ms);

//Global variables and class instances
CAN_Controller canController;                           //Create an instance of the platform-agnostic CAN Controller
CANMessageDictionary messageDictionary;                 //Create an instance of the message dictionary to hold received CAN Messages
uint32_t currentCANSpeed = DEFAULT_CAN_SPEED;           //Holds current CAN Bus baud rate. Initialized by the EEPROM if properly keyed
uint32_t currentBaudRate = DEFAULT_SERIAL_SPEED;        //Holds current UART baud rate. Initialized by the EEPROM if properly keyed
bool printedHelpOnStart = false;                        //Flag if the help menu has been printed automatically on this power cycle
bool serialConnected = false;                           //Flag indicating if the serial console is open on the connected PC
bool appConnected = false;                              //Flag to change messages to abbreviated codes for automated tools.
bool rgbUpdate = true;                                  //Flag set by timer to periodically update the RGB LED
bool resetParameters = false;                           //Flag set when resetting parameters via clicking the MODE button 3 times

bool printAllReceviedMessages = false;                  //Flag to print all messages received on the CAN Bus
bool canTransmitLoop = false;                           //Flag to enter CAN transmit loop where data is looped 0-255. Cleared upon reading next char.
uint32_t canLoopAddress;                                //When in CAN transmit loop, this is the address to loop on, or the starting address
uint8_t canLoopMask;                                    //When in CAN transmit loop, this is the bitmask for which bytes loop. Other bytes are held at 0.
bool canAddressLoop = false;                            //Flag to enter CAN address loop.Looped from addressLoopStart to addressLoopEnd. Cleared upon reading next char.
uint32_t addressLoopStart;                              //Starting address for CAN address loop mode.
uint32_t addressLoopEnd;                                //Ending address for CAN address loop mode.
uint8_t addressLoopData;                                //Data sent on all bytes when looping in address mode
uint8_t addressLoopStepSize;                            //Number of messages to emulate at a time during the address loop
uint32_t loopModeDelay;                                 //Amount of time between messages when in address or data loop mode (in milliseconds)
bool canAllComboLoop;                                   //Flag to enter a CAN trasnmit loop where all addresses in a range are looped, and all data values are sent on each byte
bool loopUseHex = true;                                 //Flag to print out data in loop mode in hex format

uint8_t emulationBank1Index = 0;                        //Bank 1 IDs are stored in indexes RETAINED_EMULATION_IDS through (MAX_EMULATION_IDS-1).
uint8_t emulationBank2Index = BANK1_EMULATION_IDS;      //Bank 2 IDs are stored in indexes 0 through (RETAINED_EMULATION_IDS-1).
LV_CANMessage emuluationMessages[MAX_EMULATION_IDS];    //Array to hold CAN messages being emulated. Circuilarly indexed using above two vars.


// setup() runs once, when the device is first turned on
void setup() {
    readEEPROM();   //Fetch values for the CAN and UART baud rate from EEPROM if EEPROM is set up. Otherwise use default values

    //while (!Serial.isConnected()) delay(100);
    
    Serial.begin(currentBaudRate);

    #if PLATFORM_ID == PLATFORM_PHOTON_PRODUCTION   //No need for chip select on Photon
    canController.begin(currentCANSpeed);
    #else
    canController.begin(currentCANSpeed, CAN0_CS);
    #endif

    RGB.control(true);

    LEDTimer.start();

    System.on(button_click, button_clicked);  //Handle button clicks to do reset
}

// loop() runs over and over again, as quickly as it can execute.
void loop() {
    parseCommand();
    receiveCANMessages();
    updateRGBLED();
    emulateCANPackets();
    if(serialConnected && !printedHelpOnStart){
        printHelpMenu();
        printedHelpOnStart = true;
    }
    if(resetParameters){
        updateEEPROM();
        System.reset();
        resetParameters = false;
    }
}

//Checks if a message was received over Serial and processes the command from the user.
void parseCommand(){
    if(Serial.available()){
        char inBuf[100];
        String inStr;
        inStr = Serial.readStringUntil('\n');
        inStr.toCharArray(inBuf,100);
        char receivedCommand;
        int d0=0, d1=0, d2=0, d3=0, d4=0, d5=0, d6=0, d7=0, d8=0;   //Hex integers for arguments in serial command. 9 arguments
        int x0=0, x1=0, x2=0, x3=0, x4=0, x5=0, x6=0, x7=0, x8=0;   //Hex integers for arguments in serial command. 9 arguments
        sscanf(inBuf, "%c %d %d %d %d %d %d %d %d %d", &receivedCommand, &d0, &d1, &d2, &d3, &d4, &d5, &d6, &d7, &d8);  //Pull out arguments in deicmal form.
        sscanf(inBuf, "%c %x %x %x %x %x %x %x %x %x", &receivedCommand, &x0, &x1, &x2, &x3, &x4, &x5, &x6, &x7, &x8);  //Pull out arguments in hex form.
        bool existingID;
        switch (receivedCommand){
        case CAN_BAUD_COMMAND:
            if(d0 > 0){                         //Take argument 0 as the new baud rate. Sanity check that it is positive
                canController.changeCANSpeed(d0);           //Take decimal representation
                if(appConnected) Serial.printlnf("CBR,%lu", convertBaudRateToParticle(canController.CurrentBaudRate()));
                else Serial.printlnf("Updated CAN baud rate to: %lu", convertBaudRateToParticle(canController.CurrentBaudRate()));   //Echo to user new baud rate
                updateEEPROM();                 //Write new serial config to have serial retained on next boot
            }
            else{                               //If baud rate is 0, then print out the current baud rate
                if(appConnected) Serial.printlnf("CBR,%lu", convertBaudRateToParticle(canController.CurrentBaudRate()));
                else Serial.printlnf("CAN baud rate: %lu", convertBaudRateToParticle(canController.CurrentBaudRate()));   //Echo to user current baud rate
            }
            break;

        case SERIAL_BAUD_COMMAND:               //Command to update the current baud rate of the Serial port
            if(d0 > 0){                         //Take argument 0 as the new baud rate. Sanity check that it is positive
                currentBaudRate = d0;           //Take decimal representation
                if(appConnected) Serial.printlnf("BR,%lu", convertBaudRateToParticle(canController.CurrentBaudRate()));
                else Serial.printlnf("Updating Serial baud rate to: %lu", currentBaudRate);   //Echo to user new baud rate
                delay(100);
                Serial.end();                   //End serial at current baud rate
                updateEEPROM();                 //Write new serial config to have serial retained on next boot
                Serial.begin(currentBaudRate);  //Start serial again at new baud rate
            }
            else{
                if(appConnected) Serial.printlnf("BR,ERR");
                else Serial.println("Error! Baud rate was negative!");   //Warn user of incorrect baud rate. Don't update baud.
            }
            break;

        case FLUSH_COMMAND_1:             //Command that flushes the buffer of addresses being emulated. Flushes Bank 1 IDs
            for(uint8_t k = 0; k < BANK1_EMULATION_IDS; k++){    //Loop over the set of non-retained messages and clear the address.
                emuluationMessages[k].addr = 0;                                     //Setting to 0 will tell the CanSend loop to not send this message.
            }
            if(appConnected) Serial.printlnf("CLR1");
            else Serial.println("Cleared All CAN Bus Emulation IDs");                    //Echo to user
            emulationBank1Index = 0;        //Reset circular buffer index
            break;

        case FLUSH_COMMAND_2:             //Command that flushes the buffer of addresses being emulated. Does not flush retained ids
            for(uint8_t k = BANK1_EMULATION_IDS; k < MAX_EMULATION_IDS; k++){    //Loop over the set of non-retained messages and clear the address.
                emuluationMessages[k].addr = 0;                                     //Setting to 0 will tell the CanSend loop to not send this message.
            }
            if(appConnected) Serial.printlnf("CLR2");
            else Serial.println("Cleared All CAN Bus Emulation IDs");                    //Echo to user
            emulationBank2Index = BANK1_EMULATION_IDS;        //Reset circular buffer index
            break;

        case PRINT_ALL_COMMAND:         //Command that enables printing of all messages received by the CAN controller. Toggles printing all
            if(printAllReceviedMessages){
                printAllReceviedMessages = false;
                if(appConnected) Serial.println("DPA");
                else Serial.println("Disabling Print All");
            }
            else{
                printAllReceviedMessages = true;
                if(appConnected) Serial.println("EPA");
                else Serial.println("Enabling Print All");
            }
            break;

        case PRINT_COMMAND:               //Command to print data of all messages with a unique address
            std::sort(messageDictionary.RXIDs.begin(),messageDictionary.RXIDs.end(),compareID); //Sort recevied messages by address, ascending
            if(!appConnected) Serial.println("============ DISCOVERED CAN IDS ==============");                   //Print header
            if(!appConnected) Serial.println("=     ID     x0 x1 x2 x3 x4 x5 x6 x7 Updated =");
            for(CANTX &msg: messageDictionary.RXIDs){                                           //Loop over all IDs and print address and data
                if(appConnected) Serial.printlnf("M,%lu,%d,%d,%d,%d,%d,%d,%d,%d,%c",msg.addr,msg.byte0,msg.byte1,msg.byte2,msg.byte3,msg.byte4,msg.byte5,msg.byte6,msg.byte7,(msg.latest)?'*':' ');
                else Serial.printlnf("= 0x%08lx %02x %02x %02x %02x %02x %02x %02x %02x    %c    =",msg.addr,msg.byte0,msg.byte1,msg.byte2,msg.byte3,msg.byte4,msg.byte5,msg.byte6,msg.byte7,(msg.latest)?'*':' ');
                msg.latest = false;                                                               //Set latest to false so next print will show change
            }
            if(appConnected) Serial.println("END");
            else Serial.println("==============================================");
            break;

        case DELTA_PRINT_COMMAND:
            std::sort(messageDictionary.RXIDs.begin(),messageDictionary.RXIDs.end(),compareID); //Sort recevied messages by address, ascending
            if(!appConnected) Serial.println("============ DISCOVERED CAN IDS ==============");                   //Print header
            if(!appConnected) Serial.println("=     ID     x0 x1 x2 x3 x4 x5 x6 x7 Updated =");
            for(CANTX &msg: messageDictionary.RXIDs){                                           //Loop over all IDs and print address and data
                if(msg.changed){
                    if(appConnected) Serial.printlnf("D,%lu,%d,%d,%d,%d,%d,%d,%d,%d",msg.addr,msg.byte0,msg.byte1,msg.byte2,msg.byte3,msg.byte4,msg.byte5,msg.byte6,msg.byte7);
                    else Serial.printlnf("= 0x%08lx %02x %02x %02x %02x %02x %02x %02x %02x         =",msg.addr,msg.byte0,msg.byte1,msg.byte2,msg.byte3,msg.byte4,msg.byte5,msg.byte6,msg.byte7);
                }
                msg.changed = false;                                                            //Set latest to false so next print will show change
                msg.latest = false;
            }
            if(appConnected) Serial.println("END");
            else Serial.println("==============================================");
            break;

        case SINGLE_PACKET_COMMAND:         //Sends a single CAN message with the given address and data
            if(appConnected) Serial.printlnf("SP,%d,%d,%d,%d,%d,%d,%d,%d,%d",x0,x1,x2,x3,x4,x5,x6,x7,x8);
            else Serial.printlnf("Send Packet: ID 0x%x Data: %x %x %x %x %x %x %x %x",x0,x1,x2,x3,x4,x5,x6,x7,x8);
            canController.CANSend(x0,x1,x2,x3,x4,x5,x6,x7,x8);
            break;

        case NEW_B1_MESSAGE_COMMAND:       //Creates an entry for a message that will be repeatedly sent. Allows for up to BANK1_EMULATION_IDS messages. Will be cleared by flush.
            existingID = false;
            if(x0 == 0){
                if(appConnected) Serial.println("ERR,NEGADDR");
                else Serial.println("Cannot create a message with address of 0");
                return;
            }
            //Loop over existing emulated messages and update the data fields if this one already exists
            for(uint8_t k = 0; k < BANK1_EMULATION_IDS; k++){    
                if(x0 == emuluationMessages[k].addr){   //Found an emulation message with the same address
                    emuluationMessages[k].update(x0,x1,x2,x3,x4,x5,x6,x7,x8);   //use this method to pass in all the new data from serial
                    LV_CANMessage p = emuluationMessages[k];
                    existingID = true;
                }
            }
            //If the message is not being emulated already, then create an entry for it. Loop back around if we're at the end of the circular buffer
            if(!existingID){
                emuluationMessages[emulationBank1Index++].update(x0,x1,x2,x3,x4,x5,x6,x7,x8);                   //Update data in array of messages being sent
                if(emulationBank1Index >= BANK1_EMULATION_IDS) emulationBank1Index = 0;    //Check circular buffer and reset if necessary
            }
            if(!appConnected) printLoopMessages();
            break;

        case NEW_B2_MESSAGE_COMMAND:      //Creates an entry for a retained message that will be repeatedly sent. Allows for up to (MAX_EMULATION_IDS - RETAINED_EMULATION_IDS) messages. Won't be flushed.
            existingID = false;
            if(x0 == 0){
                if(appConnected) Serial.println("ERR,NEGADDR");
                else Serial.println("Cannot create a message with address of 0");
                return;
            }
            //Loop over existing emulated messages and update the data fields if this one already exists
            for(uint8_t k = BANK1_EMULATION_IDS; k < MAX_EMULATION_IDS; k++){
                if(x0 == emuluationMessages[k].addr){   //Found an emulation message with the same address
                    emuluationMessages[k].update(x0,x1,x2,x3,x4,x5,x6,x7,x8);   //use this method to pass in all the new data from serial
                    existingID = true;
                }
            }
            //If the message is not being emulated already, then create an entry for it. Loop back around if we're at the end of the circular buffer
            if(!existingID){
                emuluationMessages[emulationBank2Index++].update(x0,x1,x2,x3,x4,x5,x6,x7,x8);                 //Update data in array of messages being sent
                if(emulationBank2Index >= MAX_EMULATION_IDS) emulationBank2Index = BANK1_EMULATION_IDS;            //Check circular buffer and reset if necessary
            }
            if(!appConnected) printLoopMessages();
            break;

        case DATA_LOOP_COMMAND:
            canTransmitLoop = true;
            canLoopAddress = x0;
            canLoopMask = x1;
            loopModeDelay = x2;
            loopUseHex = (x3 == 0);
            if(loopModeDelay < 25) loopModeDelay = 25;
            if(appConnected) Serial.printlnf("SDL,%lu,%d,%lu", canLoopAddress, canLoopMask, loopModeDelay);
            else Serial.printlnf("Starting data loop on 0x%lx with mask 0x%x and delay %lums", canLoopAddress, canLoopMask, loopModeDelay);
            break;

        case ADDR_LOOP_COMMAND:
            if(x0 > x1){
                if(appConnected) Serial.println("ERR,NEGADDR");
                else Serial.println("Error. Starting address is less than ending address.");
                return;
            }
            canAddressLoop = true;
            addressLoopStart = x0;
            addressLoopEnd = x1;
            addressLoopData = x2;
            loopModeDelay = x3;
            addressLoopStepSize = (x4 > 0) ? x4 : 1;
            loopUseHex = (x5 == 0);
            if(loopModeDelay < 25) loopModeDelay = 25;
            if(appConnected) Serial.printlnf("SAL,%lu,%lu,%lu", addressLoopStart, addressLoopEnd, loopModeDelay);
            else Serial.printlnf("Starting address loop from 0x%lx to 0x%lx and delay %lums", addressLoopStart, addressLoopEnd, loopModeDelay);
            break;

        case COMBO_LOOP_COMMAND:
            if(x0 > x1){
                if(appConnected) Serial.println("ERR,NEGADDR");
                else Serial.println("Error. Starting address is less than ending address.");
                return;
            }
            canAllComboLoop = true;
            addressLoopStart = x0;
            addressLoopEnd = x1;
            loopModeDelay = x2;
            addressLoopStepSize = (x3 > 0) ? x3 : 1;
            loopUseHex = (x4 == 0);
            if(loopModeDelay < 25) loopModeDelay = 25;
            if(appConnected) Serial.printlnf("SCL,%lu,%lu,%lu", addressLoopStart, addressLoopEnd, loopModeDelay);
            else Serial.printlnf("Starting combo loop from 0x%lx to 0x%lx and delay %lums", addressLoopStart, addressLoopEnd, loopModeDelay);
            break;
            
        case VERSION_COMMAND:
            Serial.println(SOFTWARE_VERSION);
            break;

        case APPMODE_COMMAND:
            appConnected = true;
            Serial.println("APPMODE");
            break;

        case REGMODE_COMMAND:
            appConnected = false;
            Serial.println("Standard Print Mode");
            break;

        case QUERY_LIST_COMMAND:
            printLoopMessages();
            break;

        default:
            printHelpMenu();
            break;
        }
    }
}

void emulateCANPackets(){
    if(canAddressLoop){
        //Comments will use the example of 'o 100 125 ff 64 a' - Loop from 0x100 to 0x125 with a step size of 10
        //Start loop with sweep for data
        canAddressLoop = false;
        canComboLoop(true, true, addressLoopStart, addressLoopEnd, addressLoopData, addressLoopData, 0xFF, addressLoopStepSize, 'A');
    }
    else if(canTransmitLoop){
        //Start loop with sweep for data
        canTransmitLoop = false;
        canComboLoop(true, true, canLoopAddress, canLoopAddress, 0, 255, canLoopMask, 1, 'D');
    }
    else if(canAllComboLoop){
        //Start loop with a sweep for both data and address
        canAllComboLoop = false;
        canComboLoop(true, true, addressLoopStart, addressLoopEnd, 0, 0xFF, 0xFF, addressLoopStepSize, 'C'); 
    }
    else{   //Not in a loop mode. Do standard message broadcast from 'm' and 'n' commands
        for(uint8_t k = 0; k < MAX_EMULATION_IDS; k++){         //Loop over all messages in the array
            if(emuluationMessages[k].addr){                     //Only transmit messages with non-zero addresses
                canController.CANSend(emuluationMessages[k]);   //Can just directly pass in the message object
                delayMicroseconds(50);                          //Wait some time to not overload the controller
            }
        }
    }
    
}

void printLoopMessages(){
    for(uint8_t k = 0; k < MAX_EMULATION_IDS; k++){
        LV_CANMessage p = emuluationMessages[k];
        if(k >= BANK1_EMULATION_IDS){
            if(appConnected) Serial.printlnf("B2,%lu,%d,%d,%d,%d,%d,%d,%d,%d",p.addr,p.byte0,p.byte1,p.byte2,p.byte3,p.byte4,p.byte5,p.byte6,p.byte7);
            else Serial.printlnf("Bank 2 ID 0x%lx: %x %x %x %x %x %x %x %x",p.addr,p.byte0,p.byte1,p.byte2,p.byte3,p.byte4,p.byte5,p.byte6,p.byte7);    //Echo back to user
        }
        else{
            if(appConnected) Serial.printlnf("B1,%lu,%d,%d,%d,%d,%d,%d,%d,%d",p.addr,p.byte0,p.byte1,p.byte2,p.byte3,p.byte4,p.byte5,p.byte6,p.byte7);
            else Serial.printlnf("Bank 1 ID 0x%lx: %x %x %x %x %x %x %x %x",p.addr,p.byte0,p.byte1,p.byte2,p.byte3,p.byte4,p.byte5,p.byte6,p.byte7);    //Echo back to user
        }
    }
    Serial.println("DONE");
}

//Prints out large list of available commands and their arguments
void printHelpMenu(){
    Serial.printlnf("======================================= WELCOME TO THE CAN BUS ANALYZER =========================================");
    Serial.printlnf("=      Version: %3s                  Current UART BAUD: %07lu                Current CAN BAUD: %07lu        =", SOFTWARE_VERSION, currentBaudRate, convertBaudRateToParticle(canController.CurrentBaudRate()));
    Serial.printlnf("=================================================================================================================");
    Serial.printlnf("= Avaliable Commands   | Arguments (d = dec, x = hex) | Info                                                    =");
    Serial.printlnf("= 'h' - Help           | h - - - - - - - - -          | Prints this window                                      =");
    Serial.printlnf("= 'b' - UART Baud Rate | b d - - - - - - - -          | Sets UART baud to value specified by d0                 =");
    Serial.printlnf("= 'c' - CAN Baud       | c d - - - - - - - -          | Sets CAN baud to value specified by d0                  =");
    Serial.printlnf("= 'f' - Flush Messages | f - - - - - - - - -          | Clears Bank 1 messages being sent                       =");
    Serial.printlnf("= 'g' - Flush Messages | g - - - - - - - - -          | Clears Bank 2 messages being sent                       =");
    Serial.printlnf("= 'a' - Print all      | a - - - - - - - - -          | Enables printing all received CAN messages (no filter)  =");
    Serial.printlnf("= 'p' - Print Unique   | p - - - - - - - - -          | Prints list of messages and newest data                 =");
    Serial.printlnf("= 'd' - Print Changed  | d - - - - - - - - -          | Same as 'p', but only messages that have changed        =");
    Serial.printlnf("= 's' - Send Message   | s x x x x x x x x x          | Sends one message on the CAN bus                        =");
    Serial.printlnf("= 'm' - Add Msg Bank 1 | m x x x x x x x x x          | Continuously sent message for Bank 1                    =");
    Serial.printlnf("= 'n' - Add Msg Bank 2 | n x x x x x x x x x          | Continuously sent message for Bank 2                    =");
    Serial.printlnf("= 'l' - Data Loop Mode | l x x x - - - - - -          | Loops data 0-255 on addr x0 on bytes x1 (delay x2)      =");
    Serial.printlnf("= 'o' - Addr Loop Mode | o x x x x x - - - -          | Loops from addr x0 to x1, data = x2, delay x3, step x4  =");
    Serial.printlnf("= 'k' - Combo Loop Md. | k x x x x - - - - -          | Loops from addr x0 to x1, delay x2, step x4, data 0-xFF =");
    Serial.printlnf("= 'v' - Version        | v - - - - - - - - -          | Prints version of the firmware on this device           =");
    Serial.printlnf("= 'i' - En. App Mode   | i - - - - - - - - -          | Switches format of messages to be easy to read by app   =");
    Serial.printlnf("= 'j' - Dis. App Mode  | j - - - - - - - - -          | Switches format of messages to be easy to read by human =");
    Serial.printlnf("= 'q' - Query Messages | q - - - - - - - - -          | Prints out all messages being emulate                   =");
    Serial.printlnf("=================================================================================================================");
}

//Recevies any CAN messages and pushes them into the CAN Message Dictionary.
void receiveCANMessages(){
    LV_CANMessage rxMessage;
    if(canController.receive(rxMessage)){                   //Check if the CAN controller has received a message
        if(printAllReceviedMessages){
            if(appConnected) Serial.printlnf("A,%lu,%lu,%d,%d,%d,%d,%d,%d,%d,%d",millis(), rxMessage.addr, rxMessage.byte0, rxMessage.byte1, rxMessage.byte2, rxMessage.byte3, rxMessage.byte4, rxMessage.byte5, rxMessage.byte6, rxMessage.byte7);
            else Serial.printlnf("%09ld Received ID 0x%07lx %02x %02x %02x %02x %02x %02x %02x %02x",millis(), rxMessage.addr, rxMessage.byte0, rxMessage.byte1, rxMessage.byte2, rxMessage.byte3, rxMessage.byte4, rxMessage.byte5, rxMessage.byte6, rxMessage.byte7);
        }
        messageDictionary.updateOrInsertMessage(rxMessage); //Update or push it into the dictionary if there was a message
    }
}

//Function to periodically update the LED. Changes color based on CAN baud rate and if Serial is connected
void updateRGBLED(){
    if(rgbUpdate){
        serialConnected = Serial.isConnected();
        uint8_t B_LED = 255 * serialConnected;

        switch (currentCANSpeed){
        case CAN_500KBPS:
            RGB.color(0,255,B_LED);
            break;
        case CAN_250KBPS:
            RGB.color(255,0,B_LED);
            break;
        default:
            RGB.color(255,255,B_LED);
            break;
        }
        rgbUpdate = false;
    }
}

//Comparator for CAN message type to allow for sorting based on the message address.
bool compareID(CANTX id1, CANTX id2){
  return (id1.addr < id2.addr);
}

//Reads from the EEPROM if it is properly formatted. Updates the EEPROM if formatting does not match
void readEEPROM(){
    //Check if the EEPROM has been configured for use by the CAN analyzer. Read memory items if the keys match
    if(EEPROM.read(EEPROM_KEY1_LOCATION) == EEPROM_KEY1 && EEPROM.read(EEPROM_KEY2_LOCATION) == EEPROM_KEY2){
        currentCANSpeed = EEPROM.read(EEPROM_CANS_LOCATION);
        EEPROM.get(EEPROM_BAUD_LOCATION, currentBaudRate);
    }
    //Otherwise, write the default values and the keys for the CAN Analyzer so the EEPROM is set up for the next time
    else{
        EEPROM.write(EEPROM_CANS_LOCATION, currentCANSpeed);
        EEPROM.put(EEPROM_BAUD_LOCATION, currentBaudRate);
        EEPROM.write(EEPROM_KEY1_LOCATION, EEPROM_KEY1);
        EEPROM.write(EEPROM_KEY2_LOCATION, EEPROM_KEY2);
    }
}

//Writes back CAN bus speed and baud rate to EEPROM
void updateEEPROM(){
    EEPROM.write(EEPROM_CANS_LOCATION, currentCANSpeed);
    EEPROM.put(EEPROM_BAUD_LOCATION, currentBaudRate);
}

//Starts a loop sequence that loops over a range of addresses and loops over all data values
void canComboLoop(bool loopAddress, bool loopData, uint32_t startAddress, uint32_t endAddress, uint8_t dataStart, uint8_t dataEnd, uint8_t dataMask, uint32_t stepSize, char loopType){
    LV_CANMessage lvm;
    uint16_t count = loopModeDelay / 10;  //Calculate the number of times to send each set of CAN messages
    lvm.update(startAddress, dataStart, dataStart, dataStart, dataStart, dataStart, dataStart, dataStart, dataStart);
    
    uint16_t j;
    for(j = startAddress; j <= endAddress; j+=stepSize){
        for(uint16_t data = dataStart; data <= dataEnd; data++){
            if(dataMask&1) lvm.byte0 = data;
            if(dataMask&2) lvm.byte1 = data;
            if(dataMask&4) lvm.byte2 = data;
            if(dataMask&8) lvm.byte3 = data;
            if(dataMask&16) lvm.byte4 = data;
            if(dataMask&32) lvm.byte5 = data;
            if(dataMask&64) lvm.byte6 = data;
            if(dataMask&128) lvm.byte7 = data;
            for(uint32_t k = 0; k < stepSize; k++){
                if(Serial.available()) return;            
                if(j+k > endAddress) continue;
                if(appConnected){
                    if(loopUseHex) Serial.printlnf("%cL,%07lx,%02x,%02x,%02x,%02x,%02x,%02x,%02x,%02x", loopType, j+k, lvm.byte0, lvm.byte1, lvm.byte2, lvm.byte3, lvm.byte4, lvm.byte5, lvm.byte6, lvm.byte7);
                    else Serial.printlnf("%cL,%ld,%d,%d,%d,%d,%d,%d,%d,%d", loopType, j+k, lvm.byte0, lvm.byte1, lvm.byte2, lvm.byte3, lvm.byte4, lvm.byte5, lvm.byte6, lvm.byte7);
                } 
                else{
                    Serial.printlnf("Sending ID 0x%07lx %02x %02x %02x %02x %02x %02x %02x %02x", j+k, lvm.byte0, lvm.byte1, lvm.byte2, lvm.byte3, lvm.byte4, lvm.byte5, lvm.byte6, lvm.byte7);
                }
            }
            for(uint16_t i = 0; i < count; i++){
                //Send the messages for the Address loop
                for(uint32_t k = 0; k < stepSize; k++){
                    if(j+k > endAddress) continue;
                    lvm.addr = j+k;
                    canController.CANSend(lvm);
                }
                //Send the messages from the Message Banks as well
                for(uint8_t k = 0; k < BANK1_EMULATION_IDS; k++){       //Only transmit Message Bank 1 messages while in a loop
                    if(emuluationMessages[k].addr){                     //Only transmit messages with non-zero addresses
                        canController.CANSend(emuluationMessages[k]);   //Can just directly pass in the message object
                        delayMicroseconds(50);                          //Wait some time to not overload the controller
                    }
                }
                delay(10);
            }
        }
    }
}

//Interrupt to perform regular update activities such as updating the RGB LED color
void tmr500ms(){
    rgbUpdate = true;
}


void button_clicked(system_event_t event, int param)
{
    int clicks = system_button_clicks(param);
    if(clicks < 3) return;
    RGB.color(0,0,255);
    currentBaudRate = 115200;
    currentCANSpeed = CAN_500KBPS;
    delay(250);
    RGB.color(0,0,0);
    delay(250);
    RGB.color(0,0,255);
    delay(250);
    RGB.color(0,0,0);
    delay(250);
}
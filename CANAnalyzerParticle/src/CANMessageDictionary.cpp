#include "Particle.h"
#include "CANMessageDictionary.h"
#include "DecentralizedLV-Boards/DecentralizedLV-Boards.h"
#undef min
#undef max
#include <vector>
#include <algorithm>

void CANTX::update(LV_CANMessage msg){
    addr = msg.addr;
    changed = changed || !(byte0 == msg.byte0 && byte1 == msg.byte1 && byte2 == msg.byte2 && byte3 == msg.byte3 && byte4 == msg.byte4 && byte5 == msg.byte5 && byte6 == msg.byte6 && byte7 == msg.byte7);
    byte0 = msg.byte0; byte1 = msg.byte1; byte2 = msg.byte2; byte3 = msg.byte3; byte4 = msg.byte4; byte5 = msg.byte5; byte6 = msg.byte6; byte7 = msg.byte7;
    latest = true;
}

//Searches all received messages to find one with the specified address. Returns the message type
CANTX CANMessageDictionary::findMessage(uint32_t address){
    CANTX canMessage;
    for(CANTX &msg: RXIDs) if(msg.addr == address) canMessage = msg;
    return canMessage;
}

//Checks if the vector contains a message with the same address. Creates a new entry in the vector if it doesn't exist.
//Returns boolean indicating if this was a new message
bool CANMessageDictionary::updateOrInsertMessage(LV_CANMessage newMsg){
    bool newAddress = true;
    for(CANTX &msg: RXIDs){             //Loop  over all messages in the dictionary and check if this address exists
        if(msg.addr == newMsg.addr){
            newAddress = false;         //Set flag if it does exist so we don't create another entry
            msg.update(newMsg);
        }
    }
    if(newAddress){                     //If this flag is still true, the vector does not currently contain the inputted address
        CANTX newMessage;               //Create a new message and populate the data
        newMessage.update(newMsg);
        RXIDs.push_back(newMessage);    //Push it onto the vector
    }
    return newAddress;
}

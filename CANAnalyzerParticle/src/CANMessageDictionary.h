#include "Particle.h"
#include "DecentralizedLV-Boards/DecentralizedLV-Boards.h"
#undef min
#undef max
#include <vector>
#include <algorithm>

//Class for a CAN message. Has methods to compare if the data has changed since the last time a message with the same address was received.
class CANTX{
  public:
  uint32_t addr = 0; 
  uint8_t byte0 = 0; uint8_t byte1 = 0; uint8_t byte2 = 0; uint8_t byte3 = 0; uint8_t byte4 = 0; uint8_t byte5 = 0; uint8_t byte6 = 0; uint8_t byte7 = 0;
  bool latest = true;
  bool changed = true;
  void update(LV_CANMessage msg);
};

class CANMessageDictionary{
    public:
    //Vector to hold all messages that have been received so far
    std::vector<CANTX> RXIDs;

    //Searches all received messages to find one with the specified address. Returns the message type
    CANTX findMessage(uint32_t address);

    //Checks if the vector contains a message with the same address. Creates a new entry in the vector if it doesn't exist.
    //Returns boolean indicating if this was a new message
    bool updateOrInsertMessage(LV_CANMessage newMsg);
};
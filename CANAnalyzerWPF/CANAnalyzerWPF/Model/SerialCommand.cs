using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANAnalyzerWPF.Model
{
    enum SerialCommand
    {
        CAN_BAUD_COMMAND = 'c',                 //Takes 1 arg for new baud rate. Ex: "c 500000" sets baud rate to 500kbps
        SERIAL_BAUD_COMMAND = 'b',              //Takes 1 arg for new baud rate. EX: "b 115200" sets baud rate to 115200 baud
        HELP_COMMAND = 'h',                     //Takes no args. Prints out available commands
        FLUSH_COMMAND_1 = 'f',                  //Takes no args. Clears all CAN IDs being emulated from Bank 1.
        FLUSH_COMMAND_2 = 'g',                  //Takes no args. Clears all CAN IDs being emulated from Bank 2.
        PRINT_ALL_COMMAND = 'a',                //Takes no args. Toggles printing all messages received from the CAN Bus. Will fill serial console...
        PRINT_COMMAND = 'p',                    //Takes no args. Prints all CAN IDs that have been received. Has field to indicate if the values changed
        DELTA_PRINT_COMMAND = 'd',              //Takes no args. Prints all CAN IDs that have changed since the last time it was printed. Has field to indicate if the values changed
        SINGLE_PACKET_COMMAND = 's',            //Sends one CAN message. First arg is address (hex). Next 8 args are the data bytes (in hex) b0, b1, b2...
        NEW_BANK1_COMMAND = 'm',                //Add continuously-sent message to Bank 1. First arg is address (hex). Next 8 args are the data bytes (in hex) b0, b1, b2...
        NEW_BANK2_COMMAND = 'n',                //Add Continuously-sent message to Bank 2. First arg is address (hex). Next 8 args are the data bytes (in hex) b0, b1, b2...
        DATA_LOOP_COMMAND = 'l',                //Sends messages on one address and ramps data value on masked bytes from 0-255.
        ADDR_LOOP_COMMAND = 'o',                //Sends messages with the same data across a range of CAN addresses.
        VERSION_COMMAND = 'v',                  //Prints out the version of this firmware. Use this to determine supported commands
        APPMODE_COMMAND = 'i',                  //Switch to App Mode, which makes reply messages have comma delimited codes
        QUERY_LIST_COMMAND = 'q'                //Switch to App Mode, which makes reply messages have comma delimited codes
    }
}

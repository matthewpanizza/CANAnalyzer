using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CANAnalyzerWPF.Model
{
    /// <summary>
    /// A class to represend the data in a CAN message. Contains fields and methods to easily update and access the data.
    /// </summary>
    public class CANMessage : INotifyPropertyChanged
    {
        private int[] data = new int[8];

        /// <summary>
        /// Constructor to create a CAN Message that has ID of 0 and Data of all 0's
        /// </summary>
        public CANMessage()
        {
            ID = 0;
            data = new int[8];
            this.Timestamp = 0;
        }

        /// <summary>
        /// Copy constructor for CAN Message
        /// </summary>
        /// <param name="oldMsg">Takes a CAN Message and returns a new object with the same address and data</param>
        public CANMessage(CANMessage oldMsg)
        {
            this.ID = oldMsg.ID;
            this.data = new int[8];
            for(int i = 0; i < 8; i++) { this.data[i] = oldMsg.data[i]; }
            this.Timestamp = oldMsg.Timestamp;
        }

        /// <summary>
        /// Constructor for a CAN message with each data field specified.
        /// </summary>
        /// <param name="canID">29-bit Address</param>
        /// <param name="d0">Data byte 0</param>
        /// <param name="d1">Data byte 1</param>
        /// <param name="d2">Data byte 2</param>
        /// <param name="d3">Data byte 3</param>
        /// <param name="d4">Data byte 4</param>
        /// <param name="d5">Data byte 5</param>
        /// <param name="d6">Data byte 6</param>
        /// <param name="d7">Data byte 7</param>
        /// <param name="timestamp">Optional timestamp since start of program (for print all mode)</param>
        public CANMessage(int canID, int d0, int d1, int d2, int d3, int d4, int d5, int d6, int d7, long timestamp = 0)
        {
            ID = canID;
            data = [d0, d1, d2, d3, d4, d5, d6, d7];
            Timestamp = timestamp;
        }

        /// <summary>
        /// Constructor for a CAN message with data specified as an array.
        /// </summary>
        /// <param name="canID">29-bit Address</param>
        /// <param name="dat">Data array</param>
        /// <param name="timestamp">Optional timestamp since start of program (for print all mode)</param>
        public CANMessage(int canID, int[] dat, long timestamp = 0)
        {
            ID = canID;
            for (int i = 0; i < 8; i++) { data[i] = dat[i]; }
            Timestamp = timestamp;
        }

        /// <summary>
        /// CAN Bus Address (29 bits)
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Hex representation of the CAN bus address
        /// </summary>
        public string IDHex
        {
            get
            {
                return ID.ToString("X");
            }
            set
            {
                int tmp = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 536870911) tmp = 536870911;
                ID = tmp;
            }
        }

        public int Byte0 { get => data[0]; set => data[0] = value; }

        public int Byte1 { get => data[1]; set => data[1] = value; }

        public int Byte2 { get => data[2]; set => data[2] = value; }

        public int Byte3 { get => data[3]; set => data[3] = value; }

        public int Byte4 { get => data[4]; set => data[4] = value; }

        public int Byte5 { get => data[5]; set => data[5] = value; }

        public int Byte6 { get => data[6]; set => data[6] = value; }

        public int Byte7 { get => data[7]; set => data[7] = value; }

        public string Byte0Hex { get => byteToHexString(0); set => hexStringToByte(0, value); }

        public string Byte1Hex { get => byteToHexString(1); set => hexStringToByte(1, value); }

        public string Byte2Hex { get => byteToHexString(2); set => hexStringToByte(2, value); }

        public string Byte3Hex { get => byteToHexString(3); set => hexStringToByte(3, value); }

        public string Byte4Hex { get => byteToHexString(4); set => hexStringToByte(4, value); }

        public string Byte5Hex { get => byteToHexString(5); set => hexStringToByte(5, value); }

        public string Byte6Hex { get => byteToHexString(6); set => hexStringToByte(6, value); }

        public string Byte7Hex { get => byteToHexString(7); set => hexStringToByte(7, value); }

        public long Timestamp { get; set; }

        /// <summary>
        /// Returns a string with the address and data bytes in a human-readable form. Used for ListBox representation of a message
        /// </summary>
        public string Message
        {
            get
            {
                return string.Format("Address 0x{0} | Data: 0x{1} 0x{2} 0x{3} 0x{4} 0x{5} 0x{6} 0x{7} 0x{8}", IDHex, Byte0Hex, Byte1Hex, Byte2Hex, Byte3Hex, Byte4Hex, Byte5Hex, Byte6Hex, Byte7Hex);
            }
        }

        /// <summary>
        /// Returns a string with the address and data bytes in a human-readable form. Used for ListBox representation of a message
        /// </summary>
        public string CSV
        {
            get
            {
                return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", Timestamp, IDHex, Byte0Hex, Byte1Hex, Byte2Hex, Byte3Hex, Byte4Hex, Byte5Hex, Byte6Hex, Byte7Hex);
            }
        }

        /// <summary>
        /// Converts the data at a given index to a hexadecimal string
        /// </summary>
        /// <param name="index">Data byte number (0-7)</param>
        /// <returns>Hex string representation of the underlying data</returns>
        private string byteToHexString(int index)
        {
            return data[index].ToString("X");
        }

        /// <summary>
        /// Take in a hex string and update the underlying data byte at index.
        /// </summary>
        /// <param name="index">Data byte to update</param>
        /// <param name="val">Hexadecimal integer string representation</param>
        private void hexStringToByte(int index, string val)
        {
            try
            {
                int tmp = int.Parse(val, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 255) tmp = 255;
                data[index] = tmp;
            }
            catch
            {

            }
        }

        //************************************ INotifyPropertyChanged ************************************//
        //************************************************************************************************//

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //************************************************************************************************//
        //************************************************************************************************//
    }
}

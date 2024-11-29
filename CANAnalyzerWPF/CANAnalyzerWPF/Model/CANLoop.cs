using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANAnalyzerWPF.Model
{
    public class CANDataLoop
    {
        private int address;
        public CANDataLoop() 
        {
            address = 0x100;
            Delay = 100;
            Byte0Enabled = true;
            Byte1Enabled = true;
            Byte2Enabled = true;
            Byte3Enabled = true;
            Byte4Enabled = true;
            Byte5Enabled = true;
            Byte6Enabled = true;
            Byte7Enabled = true;
        }

        public int Address { get; set; }
        public string AddressHex
        {
            get
            {
                return Address.ToString("X");
            }
            set
            {
                int tmp = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 536870911) tmp = 536870911;
                Address = tmp;
            }
        }
        public int Delay { get; set; }
        public string DelayHex
        {
            get
            {
                return Delay.ToString("X");
            }
            set
            {
                int tmp = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 536870911) tmp = 536870911;
                Delay = tmp;
            }
        }
        public bool Byte0Enabled { get; set; }
        public bool Byte1Enabled { get; set; }
        public bool Byte2Enabled { get; set; }
        public bool Byte3Enabled { get; set; }
        public bool Byte4Enabled { get; set; }
        public bool Byte5Enabled { get; set; }
        public bool Byte6Enabled { get; set; }
        public bool Byte7Enabled { get; set; }

        public int BitMask
        {
            get
            {
                return boolToBit(Byte0Enabled, 0) +
                    boolToBit(Byte1Enabled, 1) +
                    boolToBit(Byte2Enabled, 2) +
                    boolToBit(Byte3Enabled, 3) +
                    boolToBit(Byte4Enabled, 4) +
                    boolToBit(Byte5Enabled, 5) +
                    boolToBit(Byte6Enabled, 6) +
                    boolToBit(Byte7Enabled, 7);
            }
        }
        public string BitMaskHex
        {
            get
            {
                return BitMask.ToString("X");
            }
        }
        
        private int boolToBit(bool value, int shift)
        {
            int tmp = value ? 1 : 0;
            return tmp << shift;
        }
    }

    public class CANAddressLoop
    {
        private int dataValue;
        public CANAddressLoop()
        {
            StartAddress = 0x100;
            EndAddress = 0x200;
            Delay = 100;
            StepSize = 1;
            dataValue = 0;
        }

        public int StartAddress { get; set; }
        public string StartAddressHex
        {
            get
            {
                return StartAddress.ToString("X");
            }
            set
            {
                int tmp = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 536870911) tmp = 536870911;
                StartAddress = tmp;
            }
        }

        public int EndAddress { get; set; }
        public string EndAddressHex
        {
            get
            {
                return EndAddress.ToString("X");
            }
            set
            {
                int tmp = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 536870911) tmp = 536870911;
                EndAddress = tmp;
            }
        }
        public int Delay { get; set; }
        public string DelayHex
        {
            get
            {
                return Delay.ToString("X");
            }
            set
            {
                int tmp = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 536870911) tmp = 536870911;
                Delay = tmp;
            }
        }
        public int StepSize { get; set; }
        public string StepSizeHex
        {
            get
            {
                return StepSize.ToString("X");
            }
            set
            {
                int tmp = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 536870911) tmp = 536870911;
                StepSize = tmp;
            }
        }
        public int DataValue 
        { 
            get
            {
                return dataValue;
            }
            set
            {
                if (value <= 255 && value >= 0) dataValue = value;
                else dataValue = 0;
            }
        }
        public string DataValueHex
        {
            get
            {
                return DataValue.ToString("X");
            }
            set
            {
                int tmp = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                if (tmp < 0) tmp = 0;
                if (tmp > 255) tmp = 255;
                DataValue = tmp;
            }
        }
    }
}

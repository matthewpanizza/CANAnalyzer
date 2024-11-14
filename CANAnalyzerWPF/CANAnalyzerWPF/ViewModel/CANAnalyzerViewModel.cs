using System;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using CANAnalyzerWPF.ViewModel.Common;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using System.Windows.Data;
using CANAnalyzerWPF.Model;

namespace CANAnalyzerWPF.ViewModel
{
    public class CANAnalyzerViewModel : INotifyPropertyChanged
    {
        
        private string portName;
        private int baudRate;
        private bool comSelectorEnabled = true;
        private bool openPortEnabled = true;
        private bool closePortEnabled = false;
        private bool sendDataEnabled = false;
        private ObservableCollection<string> rxData;
        private ObservableCollection<string> availablePorts;
        private string txData;
        private readonly object _rxDataLock = false;

        /// <summary>
        /// Constructor for the view model. Initializes the data lists, serial port lists, and baud rate list.
        /// </summary>
        public CANAnalyzerViewModel()
        {
            RXData = new ObservableCollection<string>();
            TXData = "";
            Serial = new SerialPort();
            Serial.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
            DiscoverSerialPorts();
            BaudRate = SerialBaudRates.BaudRates.First();
        }
        //******************************************* Methods ********************************************//
        //************************************************************************************************//

        /// <summary>
        /// Discovers all COM ports available on the computer and populates list of strings with the port names
        /// </summary>
        private void DiscoverSerialPorts()
        {
            AvailablePorts = new ObservableCollection<string>(SerialPort.GetPortNames());
            PortName = AvailablePorts.First();
            
        }

        /// <summary>
        /// Tries to open the COM port with the settings chosen by the combo boxes. Switches button enabled states if connection is made.
        /// </summary>
        private void OpenSerialPort()
        {
            Serial.BaudRate = BaudRate;         //Set baud rate to the value in the drop down
            Serial.DataBits = 8;                //CAN Analyzer hardware is using 8N1
            Serial.StopBits = StopBits.One;     
            Serial.Parity = Parity.None;        
            Serial.PortName = PortName;         //Copy port name from combo box
            try
            {
                Serial.Open();
                COMSelectorEnabled = false;     //Disable COM port selector while connected to serial
                OpenPortEnabled = false;        //Disable open port button while connected to serial
                ClosePortEnabled = true;        //Enable close port button while connected to serial
                SendDataEnabled = true;         //Enable send data button while connected to serial
            }
            catch                               //If the port open didn't work, don't switch the button states.
            {
                MessageBox.Show("Could not open " + PortName + "!");    //Show pop-up to user
            }


        }

        /// <summary>
        /// Closes the open serial port and switches which buttons are enabled so a port connection could be made again.
        /// </summary>
        private void CloseSerialPort()
        { 
            Serial.Close();
            COMSelectorEnabled = true;
            OpenPortEnabled = true;
            ClosePortEnabled = false;
            SendDataEnabled = false;
        }

        /// <summary>
        /// Function that is triggered automatically when serial data is received. Takes received lines and adds it to list of recevied data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (Serial.BytesToRead > 0)
            {
                RXData.Add(Serial.ReadLine().Trim('\n').Trim('\r'));
            }
        }

        /// <summary>
        /// Writes the TX data from the text box to the serial port.
        /// </summary>
        public void SendData()
        {
            Serial.WriteLine(TXData);
            TXData = "";
        }

        //****************************************** Properties ******************************************//
        //************************************************************************************************//


        /// <summary>
        /// Returns which COM ports are available. Populated by the DiscoverSerialPorts function
        /// </summary>
        public ObservableCollection<string> AvailablePorts
        {
            get
            {
                return availablePorts;
            }
            private set
            {
                availablePorts = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Returns a list of baud rates the serial port can operate at. This is a fixed list set in SerialBaudRate.
        /// </summary>
        public static ObservableCollection<int> AvailableBaudRates
        {
            get
            {
                return SerialBaudRates.BaudRates;
            }
        }

        /// <summary>
        /// A list of all data that was received by the serial port
        /// </summary>
        public ObservableCollection<string> RXData { 
            get
            {
                return rxData;
            } 
            private set
            {
                rxData = value;
                BindingOperations.EnableCollectionSynchronization(rxData, _rxDataLock);
            }
        }

        /// <summary>
        /// A string of data the user wishes to transmit over serial. Populated by the text box on the UI.
        /// </summary>
        public string TXData
        {
            get
            {
                return txData;
            }
            set
            {
                txData = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Serial port object being used to do serial communication. Allows access to statistics on the UI.
        /// </summary>
        public SerialPort Serial { get; private set; }

        /// <summary>
        /// Bool indicating if the COM port selector should be enabled
        /// </summary>
        public bool COMSelectorEnabled 
        { 
            get
            {
                return comSelectorEnabled;
            }
            set
            {
                comSelectorEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Bool indicating if the Open Port Button should be enabled
        /// </summary>
        public bool OpenPortEnabled
        {
            get
            {
                return openPortEnabled;
            }
            set
            {
                openPortEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Bool indicating if the Close Port Button should be enabled
        /// </summary>
        public bool ClosePortEnabled
        {
            get
            {
                return closePortEnabled;
            }
            set
            {
                closePortEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Bool indicating if the Send Data Button should be enabled
        /// </summary>
        public bool SendDataEnabled
        {
            get
            {
                return sendDataEnabled;
            }
            set
            {
                sendDataEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the name of the serial port that should be used (i.e. "COM4")
        /// </summary>
        public string PortName
        {
            get { return portName; }
            set
            {
                portName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the baud rate the serial port operates at
        /// </summary>
        public int BaudRate
        {
            get { return baudRate; }
            set
            {
                baudRate = value;
                OnPropertyChanged();
            }
        }

        // Command to open the serial port
        private RelayCommand _openPortButtonCommand;
        public RelayCommand OpenPortButtonCommand
        {
            get
            {
                return _openPortButtonCommand ?? (_openPortButtonCommand = new RelayCommand(obj =>
                {
                    OpenSerialPort();
                }));
            }
        }

        // Command to close the serial port
        private RelayCommand _closePortButtonCommand;
        public RelayCommand ClosePortButtonCommand
        {
            get
            {
                return _closePortButtonCommand ?? (_closePortButtonCommand = new RelayCommand(obj =>
                {
                    CloseSerialPort();
                }));
            }
        }

        // Command to close the serial port
        private RelayCommand _refreshPortButtonCommand;
        public RelayCommand RefreshPortButtonCommand
        {
            get
            {
                return _refreshPortButtonCommand ?? (_refreshPortButtonCommand = new RelayCommand(obj =>
                {
                    DiscoverSerialPorts();
                }));
            }
        }

        // Command to save file
        private RelayCommand _sendDataCommand;
        public RelayCommand SendDataCommand
        {
            get
            {
                return _sendDataCommand ?? (_sendDataCommand = new RelayCommand(obj =>
                {
                    SendData();
                }));
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

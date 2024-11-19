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
using System.Windows.Interop;
using System.IO;

namespace CANAnalyzerWPF.ViewModel
{
    public class CANAnalyzerViewModel : INotifyPropertyChanged
    {
        private string canAnalyzerInfo;
        private string portName;
        private int baudRate;
        private int canBaudRate;
        private int defaultCANBaudRate = 500000;
        private bool comSelectorEnabled = true;
        private bool openPortEnabled = true;
        private bool closePortEnabled = false;
        private bool sendDataEnabled = false;
        private ObservableCollection<string> rxData;
        private ObservableCollection<string> availablePorts;
        private ObservableCollection<CANMessage> allCANMessages;
        private ObservableCollection<CANMessage> distinctCANMessages;
        private string txData;
        private readonly object _rxDataLock = false;
        private readonly object _allDataLock = false;
        private CANMessage uiMessage;
        private int bank1MessageIndex;
        private int bank2MessageIndex;
        volatile bool receivedBackMessages;

        /// <summary>
        /// Constructor for the view model. Initializes the data lists, serial port lists, and baud rate list.
        /// </summary>
        public CANAnalyzerViewModel()
        {

            InitConstants();
            DiscoverSerialPorts();
        }
        //******************************************* Methods ********************************************//
        //************************************************************************************************//

        /// <summary>
        /// Initializes all variables to a default value. Clears lists.
        /// </summary>
        private void InitConstants()
        {
            TXData = "";
            RXData = new ObservableCollection<string>();

            allCANMessages = new ObservableCollection<CANMessage>();
            distinctCANMessages = new ObservableCollection<CANMessage>();

            receivedBackMessages = false;

            uiMessage = new CANMessage();
            bank1MessageIndex = 0;
            bank2MessageIndex = 0;

            MessageDictionary = new CANMessageDictionary(10, 10);

            Serial = new SerialPort();
            Serial.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);

            BaudRate = SerialBaudRates.BaudRates.First();
            CANBaudRate = CANBaudRates.BaudRates.Contains(defaultCANBaudRate) ? defaultCANBaudRate : CANBaudRates.BaudRates.First();    //By default set 500000

            CANAnalyzerInfo = generateInfoString();
        }

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
            TXData = "i";
            SendData();
            Thread.Sleep(30);
            TXData = (char)SerialCommand.CAN_BAUD_COMMAND + " " + CANBaudRate.ToString();
            SendData();
            CANAnalyzerInfo = generateInfoString();
        }

        /// <summary>
        /// Closes the open serial port and switches which buttons are enabled so a port connection could be made again.
        /// </summary>
        private void CloseSerialPort()
        { 
            Serial.Close();
            COMSelectorEnabled = true;     //Enable COM port selector while not connected to serial
            OpenPortEnabled = true;        //Enable open port button while not connected to serial
            ClosePortEnabled = false;        //Disable close port button while not connected to serial
            SendDataEnabled = false;         //Disable send data button while not connected to serial
            CANAnalyzerInfo = generateInfoString();
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
                string rxData = Serial.ReadLine().Trim('\n').Trim('\r');
                RXData.Add(rxData);
                processSerialCommand(rxData);
            }
        }

        /// <summary>
        /// Writes the TX data from the text box to the serial port.
        /// </summary>
        public void SendData()
        {
            try
            {
                Serial.WriteLine(TXData);
                TXData = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not send data on " + PortName + "! " + ex.ToString());    //Show pop-up to user
            }
            
        }

        /// <summary>
        /// Clears all messages being emulated. Separated by bank.
        /// </summary>
        /// <param name="bankSet">Which bank to clear messages from. False = Bank 1, True = Bank 2.</param>
        void flushMessageBank(bool bankSet)
        {
            if(bankSet) TXData = string.Format("{0}", (char)SerialCommand.FLUSH_COMMAND_2);
            else TXData = string.Format("{0}", (char)SerialCommand.FLUSH_COMMAND_1);
            SendData();
        }

        /// <summary>
        /// Queries all messages being emulated by the Analyzer
        /// </summary>
        void queryMessages()
        {
            TXData = string.Format("{0}", (char)SerialCommand.QUERY_LIST_COMMAND);
            SendData();
        }

        /// <summary>
        /// Transmits a CAN Message one time
        /// </summary>
        /// <param name="msg">CANMessage that should be transmitted one time</param>
        void sendMessageOnce(CANMessage msg)
        {
            char command = (char)SerialCommand.SINGLE_PACKET_COMMAND;
            if (msg.ID == 0) return;
            TXData = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", command, msg.IDHex, msg.Byte0Hex, msg.Byte1Hex, msg.Byte2Hex, msg.Byte3Hex, msg.Byte4Hex, msg.Byte5Hex, msg.Byte6Hex, msg.Byte7Hex);
            SendData();
        }

        /// <summary>
        /// Inserts a message into the message list, then updates the Analyzer list with serial commands
        /// </summary>
        /// <param name="bankSet">Indicates which bank to update. False for Bank 1, True for Bank 2</param>
        /// <param name="newMsg">CANMessage that should be added to the MessageDictionary</param>
        void sendNewMessage(bool bankSet, CANMessage newMsg)
        {
            if (bankSet) MessageDictionary.AddOrUpdateMessageBank2(newMsg);      //Add the new message to the recpective bank in MessageDictionary
            else MessageDictionary.AddOrUpdateMessageBank1(newMsg);
            updateMessages(bankSet);
        }

        /// <summary>
        /// Updates the CAN Analyzer message list to match the MessageDictionary in this program. Flushes messages and then sends all non-zero messages in ObservableCollection
        /// </summary>
        /// <param name="bankSet">Indicates which bank to update. False for Bank 1, True for Bank 2</param>
        void updateMessages(bool bankSet)
        {
            //Clear all messages on the Analyzer. These will get replaces by the messages in MessageDictionary
            flushMessageBank(bankSet);
            ObservableCollection<CANMessage> msgBank;
            char command;
            if (bankSet)
            {
                msgBank = MessageDictionary.MessageBank2;
                command = (char)SerialCommand.NEW_BANK2_COMMAND;
            }
            else
            {
                msgBank = MessageDictionary.MessageBank1;
                command = (char)SerialCommand.NEW_BANK1_COMMAND;
            }
            foreach (CANMessage msg in msgBank)                         //Send the entire list from this bank to the analyzer
            {
                if (msg.ID != 0) TXData = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", command, msg.IDHex, msg.Byte0Hex, msg.Byte1Hex, msg.Byte2Hex, msg.Byte3Hex, msg.Byte4Hex, msg.Byte5Hex, msg.Byte6Hex, msg.Byte7Hex);
                SendData();
                Thread.Sleep(30);
            }
        }

        void updateDistinctMessages(CANMessage newMsg)
        {
            for (int k = 0; k < DistinctCANMessages.Count; k++)
            {
                if (DistinctCANMessages[k].ID == newMsg.ID)
                {
                    DistinctCANMessages[k] = new CANMessage(newMsg);
                    return;
                }
            }
            DistinctCANMessages.Add(newMsg);
            
            //Re-ordder by address when adding a new message
            DistinctCANMessages = new ObservableCollection<CANMessage>(DistinctCANMessages.OrderBy(s => s.ID));
        }

        private string generateInfoString()
        {
            string info = "";
            if (Serial.IsOpen)
            {
                info = " (Port: " + Serial.PortName + ", Baud Rate: " + CANBaudRate.ToString() + ")";
            }
            return "CAN Analyzer" + info;
        }

        /// <summary>
        /// Takes a string received over serial and parses out data based on the AppMode-formatted responses
        /// </summary>
        /// <param name="command"></param>
        void processSerialCommand(string command)
        {
            string[] r = command.Split(',');
            if (r.Length == 0) return;
            string responseCode = r.First();
            if (responseCode.Contains("END"))           //Response from Message Query of all emulated messages that all messages have been printed
            {
                receivedBackMessages = true;
            }
            else if(responseCode.Contains("AL"))        //Response from Address Loop
            {

            }
            else if(responseCode.Contains("DL"))        //Response from Data Loop
            {

            }
            else if(responseCode.Contains("A"))         //CAN Message sent while in "print all" mode
            {
                if (r.Length < 10) return;
                CANMessage rx = new CANMessage(int.Parse(r[2]), int.Parse(r[3]), int.Parse(r[4]), int.Parse(r[5]), int.Parse(r[6]), int.Parse(r[7]), int.Parse(r[8]), int.Parse(r[9]), int.Parse(r[10]), int.Parse(r[1]));
                RXData.Add(rx.Message);
                Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => {
                    AllCANMessages.Add(rx);
                    updateDistinctMessages(rx);
                }));
                
            }
        }

        //****************************************** Properties ******************************************//
        //************************************************************************************************//

        /// <summary>
        /// String containing info about the CAN analyzer for display as the header
        /// </summary>
        public string CANAnalyzerInfo
        {
            get
            {
                return canAnalyzerInfo;
            }
            set
            {
                canAnalyzerInfo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// All received CAN messages. Populated when running in receive all mode
        /// </summary>
        public ObservableCollection<CANMessage> AllCANMessages
        {
            get
            {
                return allCANMessages;
            }
            private set
            {
                allCANMessages = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// All received CAN messages with a unique address. Populated when running in receive all mode
        /// </summary>
        public ObservableCollection<CANMessage> DistinctCANMessages
        {
            get
            {
                return distinctCANMessages;
            }
            private set
            {
                distinctCANMessages = value;
                OnPropertyChanged();
            }
        }

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
        /// Returns a list of baud rates the CAN Bus can operate at. This is a fixed list set in SerialBaudRate.
        /// </summary>
        public static ObservableCollection<int> AvailableCANBaudRates
        {
            get
            {
                return CANBaudRates.BaudRates;
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
        /// Dictionary of CAN Bus messages that are repeatedly sent. There are two separate banks for messages to allow flushing one set at a time.
        /// </summary>
        public CANMessageDictionary MessageDictionary { get; set; }

        /// <summary>
        /// Message values set by the UI. Can be then added to the dictionary using a button
        /// </summary>
        public CANMessage UICANMessage
        {
            get
            {
                return uiMessage;
            }
            set
            {
                uiMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// On the Messages tab, this is the message currently selected by the user. Corresponds to the MessageDictionary Bank1 location.
        /// </summary>
        public int SelectedBank1Index
        {
            get
            {
                return bank1MessageIndex;
            }
            set
            {
                bank1MessageIndex = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// On the Messages tab, this is the message currently selected by the user. Corresponds to the MessageDictionary Bank2 location.
        /// </summary>
        public int SelectedBank2Index
        {
            get
            {
                return bank2MessageIndex;
            }
            set
            {
                bank2MessageIndex = value;
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

        /// <summary>
        /// Gets the baud rate the CAN Bus operates at
        /// </summary>
        public int CANBaudRate
        {
            get { return canBaudRate; }
            set
            {
                canBaudRate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command to open the serial port
        /// </summary>
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

        /// <summary>
        /// Command to close the serial port
        /// </summary>
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

        /// <summary>
        /// Command to refresh the list of available serial ports
        /// </summary>
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

        /// <summary>
        /// Command to send data from the serial console tab
        /// </summary>
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

        /// <summary>
        /// Command to add a CAN message to message bank 1
        /// </summary> 
        private RelayCommand _sendMessageOnceCommand;
        public RelayCommand SendMessageOnceCommand
        {
            get
            {
                return _sendMessageOnceCommand ?? (_sendMessageOnceCommand = new RelayCommand(obj =>
                {
                    sendMessageOnce(UICANMessage);
                }));
            }
        }

        /// <summary>
        /// Command to add a CAN message to message bank 1
        /// </summary> 
        private RelayCommand _addBank1MessageCommand;
        public RelayCommand AddBank1MessageCommand
        {
            get
            {
                return _addBank1MessageCommand ?? (_addBank1MessageCommand = new RelayCommand(obj =>
                {
                    sendNewMessage(false, UICANMessage);
                }));
            }
        }

        /// <summary>
        /// Command to add a CAN message to message bank 2
        /// </summary>
        private RelayCommand _addBank2MessageCommand;
        public RelayCommand AddBank2MessageCommand
        {
            get
            {
                return _addBank2MessageCommand ?? (_addBank2MessageCommand = new RelayCommand(obj =>
                {
                    sendNewMessage(true, UICANMessage);
                }));
            }
        }

        /// <summary>
        /// Command to remove the selected message from message bank 1. Takes index selected by the list box
        /// </summary>
        private RelayCommand _removeBank1MessageCommand;
        public RelayCommand RemoveBank1MessageCommand
        {
            get
            {
                return _removeBank1MessageCommand ?? (_removeBank1MessageCommand = new RelayCommand(obj =>
                {
                    MessageDictionary.RemoveMessageBank1(SelectedBank1Index);
                    updateMessages(false);
                }));
            }
        }

        /// <summary>
        /// Command to remove the selected message from message bank 2. Takes index selected by the list box
        /// </summary>
        private RelayCommand _removeBank2MessageCommand;
        public RelayCommand RemoveBank2MessageCommand
        {
            get
            {
                return _removeBank2MessageCommand ?? (_removeBank2MessageCommand = new RelayCommand(obj =>
                {
                    MessageDictionary.RemoveMessageBank2(SelectedBank2Index);
                    updateMessages(true);
                }));
            }
        }

        /// <summary>
        /// Command to remove the selected message from message bank 2. Takes index selected by the list box
        /// </summary>
        private RelayCommand _clearReceivedMessages;
        public RelayCommand ClearReceivedMessages
        {
            get
            {
                return _clearReceivedMessages ?? (_clearReceivedMessages = new RelayCommand(obj =>
                {
                    AllCANMessages.Clear();
                }));
            }
        }

        /// <summary>
        /// Command to remove the selected message from message bank 2. Takes index selected by the list box
        /// </summary>
        private RelayCommand _clearFilterMessages;
        public RelayCommand ClearFilterMessages
        {
            get
            {
                return _clearFilterMessages ?? (_clearFilterMessages = new RelayCommand(obj =>
                {
                    DistinctCANMessages.Clear();
                }));
            }
        }

        /// <summary>
        /// Command to remove the selected message from message bank 2. Takes index selected by the list box
        /// </summary>
        private RelayCommand _orderReceivedMessages;
        public RelayCommand OrderReceivedMessages
        {
            get
            {
                return _orderReceivedMessages ?? (_orderReceivedMessages = new RelayCommand(obj =>
                {
                    AllCANMessages = new ObservableCollection<CANMessage>(AllCANMessages.OrderBy(s => s.ID));
                }));
            }
        }

        /// <summary>
        /// Command to remove the selected message from message bank 2. Takes index selected by the list box
        /// </summary>
        private RelayCommand _saveMessagesToFile;
        public RelayCommand SaveMessagesToFile
        {
            get
            {
                return _saveMessagesToFile ?? (_saveMessagesToFile = new RelayCommand(obj =>
                {
                    // Configure save file dialog box
                    var dialog = new Microsoft.Win32.SaveFileDialog();
                    dialog.FileName = "AllTraces_" + DateTime.Now.ToString("M_d_yyyy"); // Default file name
                    dialog.DefaultExt = ".txt"; // Default file extension
                    dialog.Filter = "CSV file (*.csv)|*.csv| Text documents (.txt)|*.txt| All Files (.)|."; // Filter files by extension

                    // Show save file dialog box
                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        using (var w = new StreamWriter(dialog.FileName))
                        {
                            w.WriteLine("TimeStamp (ms),Address,Byte0,Byte1,Byte2,Byte3,Byte4,Byte5,Byte6,Byte7");
                            w.Flush();
                            foreach (CANMessage message in AllCANMessages)
                            {
                                w.WriteLine(message.CSV);
                                w.Flush();
                            }
                        }
                    }
                }));
            }
        }

        /// <summary>
        /// Command to remove the selected message from message bank 2. Takes index selected by the list box
        /// </summary>
        private RelayCommand _saveFilterMessagesToFile;
        public RelayCommand SaveFilterMessagesToFile
        {
            get
            {
                return _saveFilterMessagesToFile ?? (_saveFilterMessagesToFile = new RelayCommand(obj =>
                {
                    // Configure save file dialog box
                    var dialog = new Microsoft.Win32.SaveFileDialog();
                    dialog.FileName = "FilterTraces_" + DateTime.Now.ToString("M_d_yyyy"); // Default file name
                    dialog.DefaultExt = ".txt"; // Default file extension
                    dialog.Filter = "CSV file (*.csv)|*.csv| Text documents (.txt)|*.txt| All Files (.)|."; // Filter files by extension

                    // Show save file dialog box
                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        using (var w = new StreamWriter(dialog.FileName))
                        {
                            w.WriteLine("TimeStamp (ms),Address,Byte0,Byte1,Byte2,Byte3,Byte4,Byte5,Byte6,Byte7");
                            w.Flush();
                            foreach (CANMessage message in DistinctCANMessages)
                            {
                                w.WriteLine(message.CSV);
                                w.Flush();
                            }
                        }
                    }
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

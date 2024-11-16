using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CANAnalyzerWPF.Model
{
    public class CANMessageDictionary : INotifyPropertyChanged
    {
        private ObservableCollection<CANMessage> _messageBank1;     //List of Bank 1 messages the UI holds. May not be reflective of Analyzer until the first message is sent
        private ObservableCollection<CANMessage> _messageBank2;     //List of Bank 2 messages the UI holds. May not be reflective of Analyzer until the first message is sent
        private int bank1Size;                                      //Number of messages Bank 1 can hold at a time
        private int bank2Size;                                      //Number of messages Bank 2 can hold at a time

        /// <summary>
        /// Constructor to initialize the message dictionary. Erases any previously stored messages
        /// </summary>
        /// <param name="bank1Size"></param>
        /// <param name="bank2Size"></param>
        public CANMessageDictionary(int bank1Size, int bank2Size)
        {
            this.bank1Size = bank1Size;
            this.bank2Size = bank2Size;

            _messageBank1 = new ObservableCollection<CANMessage>();
            _messageBank2 = new ObservableCollection<CANMessage>();
        }

        //******************************************* Methods ********************************************//
        //************************************************************************************************//

        /// <summary>
        /// Adds or updates a message in the bank. If the bank is full, then the oldest (first) message is deleted.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>Boolean indicating if item was added (true) or updated (false). ID of 0 will also return false</returns>
        public bool AddOrUpdateMessageBank1(CANMessage msg)
        {
            if(msg.ID == 0) return false;                   //Don't populate array with messages that have ID of 0
            bool newCANID = true;                           //Flag to indicate if this message will be newly added or updated
            for (int k = 0; k < MessageBank1.Count; k++)    //Loop over all messages in the dicionary and look for matching CAN ID
            {
                if (MessageBank1[k].ID == msg.ID)           //Found existing ID in the message bank. Update itss data
                {
                    newCANID = false;                       //Set flag false so a new ID is not made
                    MessageBank1[k] = new CANMessage(msg);  //Need to make a new message or the data will remain the same in the UI
                }
            }
            if (newCANID)                                   //If we didn't find a message with the same ID, then create a new message
            {
                //Check how many items we have in the collection. Delete the first item and then append if we already have too many.
                if(MessageBank1.Count >= bank1Size) RemoveMessageBank1(0);
                MessageBank1.Add(new CANMessage(msg));
            }
            return newCANID;                                //Indicate to the caller which operation was done
        }

        /// <summary>
        /// Removes a CAN Message from the collection if it has the same ID as the input message
        /// </summary>
        /// <param name="msg">Message you wish to remove. Any msg matching the ID will be removed</param>
        /// <returns>Boolean indicating if any items were removed</returns>
        public bool RemoveMessageBank1(int index)
        {
            //Create new collection that non-removed IDs will be moved to
            ObservableCollection<CANMessage> tmp = new ObservableCollection<CANMessage>();

            if (MessageBank1.Count <= index) return false;  //Sanity check for out of range

            CANMessage msg = MessageBank1[index];

            foreach (CANMessage existing in MessageBank1)   //Loop over all existing IDs and copy ones that should not be removed
            {
                if(existing.ID != msg.ID)
                {
                    tmp.Add(existing);                      //Add to our temp collection
                }
            }
            MessageBank1 = tmp;                             //Then replace the original collection
            return tmp.Count == MessageBank1.Count;         //Check if the count of the collections are the same (no items were removed)
        }

        /// <summary>
        /// Adds or updates a message in the bank. If the bank is full, then the oldest (first) message is deleted.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>Boolean indicating if item was added (true) or updated (false)</returns>
        public bool AddOrUpdateMessageBank2(CANMessage msg)
        {
            if (msg.ID == 0) return false;                  //Don't populate array with messages that have ID of 0
            bool newCANID = true;                           //Flag to indicate if this message will be newly added or updated
            for (int k = 0; k < MessageBank2.Count; k++)    //Loop over all messages in the dicionary and look for matching CAN ID
            {
                if (MessageBank2[k].ID == msg.ID)           //Found existing ID in the message bank. Update itss data
                {
                    newCANID = false;                       //Set flag false so a new ID is not made
                    MessageBank2[k] = new CANMessage(msg);  //Need to make a new message or the data will remain the same in the UI
                }
            }
            if (newCANID)                                   //If we didn't find a message with the same ID, then create a new message
            {
                //Check how many items we have in the collection. Delete the first item and then append if we already have too many.
                if (MessageBank2.Count >= bank2Size) RemoveMessageBank2(0);
                MessageBank2.Add(new CANMessage(msg));

            }
            return newCANID;                                //Indicate to the caller which operation was done
        }

        /// <summary>
        /// Removes a CAN Message from the collection if it has the same ID as the input message
        /// </summary>
        /// <param name="msg">Message you wish to remove. Any msg matching the ID will be removed</param>
        /// <returns>Boolean indicating if any items were removed</returns>
        public bool RemoveMessageBank2(int index)
        {
            //Create new collection that non-removed IDs will be moved to
            ObservableCollection<CANMessage> tmp = new ObservableCollection<CANMessage>();

            if (MessageBank1.Count <= index) return false;  //Sanity check for out of range

            CANMessage msg = MessageBank2[index];

            foreach (CANMessage existing in MessageBank2)   //Loop over all existing IDs and copy ones that should not be removed
            {
                if (existing.ID != msg.ID)
                {
                    tmp.Add(existing);                      //Add to our temp collection
                }
            }
            MessageBank2 = tmp;                             //Then replace the original collection
            return tmp.Count == MessageBank2.Count;         //Check if the count of the collections are the same (no items were removed)
        }

        /// <summary>
        /// Clears the colletion of all CAN Messages
        /// </summary>
        public void ClearMessageBank1()
        {
            MessageBank1.Clear();
        }

        /// <summary>
        /// Clears the colletion of all CAN Messages
        /// </summary>
        public void ClearMessageBank2()
        {
            MessageBank2.Clear();
        }

        //****************************************** Properties ******************************************//
        //************************************************************************************************//

        /// <summary>
        /// List of messages being held in the CAN Analyzer's Message Bank 1
        /// </summary>
        public ObservableCollection<CANMessage> MessageBank1
        { 
            get
            {
                return _messageBank1;
            }
            private set
            {
                _messageBank1 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// List of messages being held in the CAN Analyzer's Message Bank 2
        /// </summary>
        public ObservableCollection<CANMessage> MessageBank2
        {
            get
            {
                return _messageBank2;
            }
            private set
            {
                _messageBank2 = value;
                OnPropertyChanged();
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

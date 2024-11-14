using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANAnalyzerWPF.Model
{
    public static class SerialBaudRates
    {
        public static ObservableCollection<int> BaudRates
        {
            get
            {
                List<int> bauds = new List<int>();
                bauds.Add(115200);
                bauds.Add(57600);
                bauds.Add(38400);
                bauds.Add(19200);
                bauds.Add(14400);
                bauds.Add(9600);
                return new ObservableCollection<int>(bauds);
            }
        }
    }
}

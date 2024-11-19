using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANAnalyzerWPF.Model
{
    public static class CANBaudRates
    {
        public static ObservableCollection<int> BaudRates
        {
            get
            {
                List<int> bauds = new List<int>();
                bauds.Add(1000000);
                bauds.Add(500000);
                bauds.Add(250000);
                bauds.Add(200000);
                bauds.Add(125000);
                bauds.Add(100000);
                bauds.Add(50000);
                return new ObservableCollection<int>(bauds);
            }
        }
    }
}

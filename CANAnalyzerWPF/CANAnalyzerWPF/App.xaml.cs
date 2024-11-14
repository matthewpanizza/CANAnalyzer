using CANAnalyzerWPF.ViewModel;
using System;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CANAnalyzerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //New mainviewmodel

            CANAnalyzerViewModel cavm = new CANAnalyzerViewModel();

            MainWindow mw = new MainWindow() { DataContext = cavm };
            mw.Show();
        }
        
    }

}

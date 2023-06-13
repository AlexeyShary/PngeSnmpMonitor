using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PngeSnmpMonitor
{
    public partial class DeviceMonitorView : UserControl, INotifyPropertyChanged
    {
        private Device associatedDevice;

        public DeviceMonitorView()
        {
            InitializeComponent();
        }

        public DeviceMonitorView(Device device)
        {
            InitializeComponent();

            AssociatedDevice = device;
        }

        public Device AssociatedDevice
        {
            get => associatedDevice;
            private set
            {
                associatedDevice = value;
                OnPropertyChanged();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                e.Handled = true;
            }
            catch
            {
            }
        }

        #region Commands

        private BaseCommand acknowledgeAlarmsCommand;

        public BaseCommand AcknowledgeAlarmsCommand
        {
            get
            {
                return acknowledgeAlarmsCommand ??
                       (acknowledgeAlarmsCommand = new BaseCommand(obj => { AssociatedDevice.AcknowledgeAlarms(); }));
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
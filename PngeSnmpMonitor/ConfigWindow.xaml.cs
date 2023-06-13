using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace PngeSnmpMonitor
{
    public partial class ConfigWindow : Window, INotifyPropertyChanged
    {
        private Visibility devicePanelVisibility = Visibility.Hidden;

        private Device selectedDevice;

        private DeviceConfigView selectedDeviceView;

        public ConfigWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        public Device SelectedDevice
        {
            get => selectedDevice;
            set
            {
                selectedDevice = value;
                OnPropertyChanged();

                if (selectedDevice == null)
                    DevicePanelVisibility = Visibility.Hidden;
                else
                    DevicePanelVisibility = Visibility.Visible;
            }
        }

        public Visibility DevicePanelVisibility
        {
            get => devicePanelVisibility;
            private set
            {
                devicePanelVisibility = value;
                OnPropertyChanged();
            }
        }

        public DeviceConfigView SelectedDeviceView
        {
            get => selectedDeviceView;
            private set
            {
                selectedDeviceView = value;
                OnPropertyChanged();
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedDevice == null)
                return;

            SelectedDeviceView = new DeviceConfigView(SelectedDevice);
        }

        #region Commands

        private BaseCommand addNewDeviceCommand;

        public BaseCommand AddNewDeviceCommand
        {
            get
            {
                return addNewDeviceCommand ??
                       (addNewDeviceCommand = new BaseCommand(obj =>
                       {
                           var newDevice = new Device();
                           SelectedDevice = newDevice;
                       }));
            }
        }

        private BaseCommand deleteSelectedDevice;

        public BaseCommand DeleteSelectedDevice
        {
            get
            {
                return deleteSelectedDevice ??
                       (deleteSelectedDevice = new BaseCommand(obj =>
                           {
                               var device = obj as Device;
                               if (device != null)
                                   Device.DeviceList.Remove(device);
                           },
                           obj => SelectedDevice != null));
            }
        }

        private BaseCommand duplicateSelectedDevice;

        public BaseCommand DuplicateSelectedDevice
        {
            get
            {
                return duplicateSelectedDevice ??
                       (duplicateSelectedDevice = new BaseCommand(obj =>
                           {
                               var device = obj as Device;
                               if (device != null)
                               {
                                   var newDevice = device.Clone(device);
                                   SelectedDevice = newDevice;
                               }
                           },
                           obj => SelectedDevice != null));
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
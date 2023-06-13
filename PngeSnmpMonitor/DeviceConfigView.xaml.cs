using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace PngeSnmpMonitor
{
    public partial class DeviceConfigView : UserControl, INotifyPropertyChanged
    {
        private Device associatedDevice;

        private ControlParameter selectedParameter;

        public DeviceConfigView()
        {
            InitializeComponent();
        }

        public DeviceConfigView(Device device)
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

        public ControlParameter SelectedParameter
        {
            get => selectedParameter;
            set
            {
                selectedParameter = value;
                OnPropertyChanged();
            }
        }

        #region Commands

        private BaseCommand addNewControlParameterCommand;

        public BaseCommand AddNewControlParameterCommand
        {
            get
            {
                return addNewControlParameterCommand ??
                       (addNewControlParameterCommand = new BaseCommand(obj =>
                       {
                           var newControlParameter = new ControlParameter(AssociatedDevice);
                           SelectedParameter = newControlParameter;
                       }));
            }
        }

        private BaseCommand deleteControlParameter;

        public BaseCommand DeleteControlParameter
        {
            get
            {
                return deleteControlParameter ??
                       (deleteControlParameter = new BaseCommand(obj =>
                           {
                               var controlParameter = obj as ControlParameter;
                               if (controlParameter != null)
                                   AssociatedDevice.ParameterList.Remove(controlParameter);
                           },
                           obj => SelectedParameter != null));
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
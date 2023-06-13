using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Data;

namespace PngeSnmpMonitor
{
    [DataContract(IsReference = true)]
    public class ControlParameter : INotifyPropertyChanged
    {
        public enum ParameterTypeEnum
        {
            [Description("Без преобразования")] Raw,
            [Description("Числовой с уставками")] Decimal,
            [Description("Список")] List
        }

        private BaseCommand toggleConfigVisibilityCommand;

        public ControlParameter(Device device)
        {
            ParameterName = "New control parameter";
            ParentDevice = device;
            ParentDevice.ParameterList.Add(this);

            Initialize();
        }

        public ControlParameter(Device device, ControlParameter controlParameter)
        {
            ParameterName = controlParameter.ParameterName;
            ParentDevice = device;
            ParentDevice.ParameterList.Add(this);

            ParameterOID = controlParameter.ParameterOID;

            ParameterHHEnabled = controlParameter.ParameterHHEnabled;
            ParameterHHValue = controlParameter.ParameterHHValue;

            ParameterLLEnabled = controlParameter.ParameterLLEnabled;
            ParameterLLValue = controlParameter.ParameterLLValue;

            ParameterHEnabled = controlParameter.ParameterHEnabled;
            ParameterHValue = controlParameter.ParameterHValue;

            ParameterLEnabled = controlParameter.ParameterLEnabled;
            ParameterLValue = controlParameter.ParameterLValue;

            SelectedParameterType = controlParameter.SelectedParameterType;

            StringChangePairsList = new ObservableCollection<StringChangePair>();
            foreach (var changePair in controlParameter.StringChangePairsList)
                StringChangePairsList.Add(new StringChangePair
                {
                    StringFrom = changePair.StringFrom, StringTo = changePair.StringTo, Warning = changePair.Warning,
                    Alarm = changePair.Alarm
                });

            Initialize();
        }

        [DataMember] public Device ParentDevice { get; set; }

        public BaseCommand ToggleConfigVisibilityCommand
        {
            get
            {
                return toggleConfigVisibilityCommand ??
                       (toggleConfigVisibilityCommand = new BaseCommand(obj =>
                       {
                           ConfigAreaVisible = !ConfigAreaVisible;
                       }));
            }
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            LastUpdateTimeString = "-";

            if (StringChangePairsList == null)
                StringChangePairsList = new ObservableCollection<StringChangePair>();
        }

        public class StringChangePair : INotifyPropertyChanged
        {
            private bool alarm;
            private string stringFrom;

            private string stringTo;

            private bool warning;

            public string StringFrom
            {
                get => stringFrom;
                set
                {
                    stringFrom = value;
                    OnPropertyChanged();
                }
            }

            public string StringTo
            {
                get => stringTo;
                set
                {
                    stringTo = value;
                    OnPropertyChanged();
                }
            }

            public bool Warning
            {
                get => warning;
                set
                {
                    warning = value;
                    OnPropertyChanged();
                }
            }

            public bool Alarm
            {
                get => alarm;
                set
                {
                    alarm = value;
                    OnPropertyChanged();
                }
            }

            #region INotifyPropertyChanged

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }

            #endregion
        }

        public class ParameterTypeEnumDescriptionValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                try
                {
                    var type = typeof(ParameterTypeEnum);
                    var name = Enum.GetName(type, value);
                    var fi = type.GetField(name);
                    var descriptionAttrib = (DescriptionAttribute)
                        Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));

                    return descriptionAttrib.Description;
                }
                catch
                {
                    return null;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        #region Properties

        public IEnumerable<ParameterTypeEnum> ParameterTypes =>
            Enum.GetValues(typeof(ParameterTypeEnum)).Cast<ParameterTypeEnum>();

        private ParameterTypeEnum selectedParameterType;

        [DataMember]
        public ParameterTypeEnum SelectedParameterType
        {
            get => selectedParameterType;
            set
            {
                selectedParameterType = value;
                OnPropertyChanged();
                SetPanelsVisibility();
            }
        }

        private bool configAreaVisible;

        public bool ConfigAreaVisible
        {
            get => configAreaVisible;
            set
            {
                configAreaVisible = value;
                OnPropertyChanged();
                SetPanelsVisibility();
            }
        }

        private bool limitsAreaVisible;

        public bool LimitsAreaVisible
        {
            get => limitsAreaVisible;
            set
            {
                limitsAreaVisible = value;
                OnPropertyChanged();
            }
        }

        private bool listAreaVisible;

        public bool ListAreaVisible
        {
            get => listAreaVisible;
            set
            {
                listAreaVisible = value;
                OnPropertyChanged();
            }
        }

        private void SetPanelsVisibility()
        {
            if (!ConfigAreaVisible)
            {
                LimitsAreaVisible = false;
                ListAreaVisible = false;
            }

            if (ConfigAreaVisible)
                switch (SelectedParameterType)
                {
                    case ParameterTypeEnum.Raw:
                        LimitsAreaVisible = false;
                        ListAreaVisible = false;
                        break;

                    case ParameterTypeEnum.Decimal:
                        LimitsAreaVisible = true;
                        ListAreaVisible = false;
                        break;

                    case ParameterTypeEnum.List:
                        LimitsAreaVisible = false;
                        ListAreaVisible = true;
                        break;
                }
        }

        private string parameterName;

        [DataMember]
        public string ParameterName
        {
            get => parameterName;
            set
            {
                parameterName = value;
                OnPropertyChanged();
            }
        }

        private string parameterOID;

        [DataMember]
        public string ParameterOID
        {
            get => parameterOID;
            set
            {
                parameterOID = value;
                OnPropertyChanged();
            }
        }

        private string parameterValue;

        public string ParameterValue
        {
            get => parameterValue;
            set
            {
                parameterValue = value;
                OnPropertyChanged();

                var valueProcessed = value;

                foreach (var changePair in StringChangePairsList)
                    if (value == changePair.StringFrom)
                    {
                        valueProcessed = changePair.StringTo;
                        break;
                    }

                ParameterValueProcessed = valueProcessed;
            }
        }

        private string parameterValueProcessed;

        public string ParameterValueProcessed
        {
            get => parameterValueProcessed;
            set
            {
                parameterValueProcessed = value;
                OnPropertyChanged();
            }
        }

        #region Decimal limits

        private bool parameterHEnabled;

        [DataMember]
        public bool ParameterHEnabled
        {
            get => parameterHEnabled;
            set
            {
                parameterHEnabled = value;
                OnPropertyChanged();
            }
        }

        private string parameterHValue;

        [DataMember]
        public string ParameterHValue
        {
            get => parameterHValue;
            set
            {
                parameterHValue = value;
                OnPropertyChanged();
            }
        }

        private bool parameterLEnabled;

        [DataMember]
        public bool ParameterLEnabled
        {
            get => parameterLEnabled;
            set
            {
                parameterLEnabled = value;
                OnPropertyChanged();
            }
        }

        private string parameterLValue;

        [DataMember]
        public string ParameterLValue
        {
            get => parameterLValue;
            set
            {
                parameterLValue = value;
                OnPropertyChanged();
            }
        }


        private bool parameterHHEnabled;

        [DataMember]
        public bool ParameterHHEnabled
        {
            get => parameterHHEnabled;
            set
            {
                parameterHHEnabled = value;
                OnPropertyChanged();
            }
        }

        private string parameterHHValue;

        [DataMember]
        public string ParameterHHValue
        {
            get => parameterHHValue;
            set
            {
                parameterHHValue = value;
                OnPropertyChanged();
            }
        }

        private bool parameterLLEnabled;

        [DataMember]
        public bool ParameterLLEnabled
        {
            get => parameterLLEnabled;
            set
            {
                parameterLLEnabled = value;
                OnPropertyChanged();
            }
        }

        private string parameterLLValue;

        [DataMember]
        public string ParameterLLValue
        {
            get => parameterLLValue;
            set
            {
                parameterLLValue = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private bool parameterAlarmUnacknowledgment;

        public bool ParameterAlarmUnacknowledgment
        {
            get => parameterAlarmUnacknowledgment;
            set
            {
                parameterAlarmUnacknowledgment = value;
                OnPropertyChanged();
            }
        }

        private DateTime lastUpdateTime;

        public DateTime LastUpdateTime
        {
            get => lastUpdateTime;
            set
            {
                lastUpdateTime = value;
                LastUpdateTimeString = lastUpdateTime.ToShortDateString() + " " + lastUpdateTime.ToLongTimeString();
                OnPropertyChanged();
            }
        }

        private string lastUpdateTimeString;

        public string LastUpdateTimeString
        {
            get => lastUpdateTimeString;
            set
            {
                lastUpdateTimeString = value;
                OnPropertyChanged();
            }
        }

        private bool parameterAlarm;

        public bool ParameterAlarm
        {
            get => parameterAlarm;
            set
            {
                if (parameterAlarm == false && value)
                {
                    ParameterAlarmUnacknowledgment = true;
                    Logger.Instance.AddMessage(ParentDevice, this, "Аварийное значение параметра.");
                }

                if (parameterAlarm && value == false)
                {
                    ParameterAlarmUnacknowledgment = false;
                    Logger.Instance.AddMessage(ParentDevice, this, "Аварийное значение параметра снято.");
                }

                if (value == false)
                    ParameterAlarmUnacknowledgment = false;

                parameterAlarm = value;
                OnPropertyChanged();
            }
        }

        private bool parameterWarning;

        public bool ParameterWarning
        {
            get => parameterWarning;
            set
            {
                if (parameterWarning == false && value)
                    Logger.Instance.AddMessage(ParentDevice, this, "Предупредительное значение параметра.");

                if (parameterWarning && value == false)
                    Logger.Instance.AddMessage(ParentDevice, this, "Предупредительное значение параметра снято.");

                parameterWarning = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<StringChangePair> stringChangePairsList;

        [DataMember]
        public ObservableCollection<StringChangePair> StringChangePairsList
        {
            get => stringChangePairsList;
            set
            {
                stringChangePairsList = value;
                OnPropertyChanged();
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
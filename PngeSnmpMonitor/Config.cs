using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace PngeSnmpMonitor
{
    [DataContract]
    public class Config : INotifyPropertyChanged
    {
        private static Config instance;

        private bool activateWindowOnAlarm = true;

        private string activeConfigFile = "";

        private bool alarmSoundEnabledGlobal = true;

        private int threadPause = 25;

        public static Config Instance
        {
            get
            {
                if (instance == null)
                    instance = new Config();

                return instance;
            }
        }

        [DataMember]
        public bool AlarmSoundEnabledGlobal
        {
            get => alarmSoundEnabledGlobal;
            set
            {
                alarmSoundEnabledGlobal = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public bool ActivateWindowOnAlarm
        {
            get => activateWindowOnAlarm;
            set
            {
                activateWindowOnAlarm = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public int ThreadPause
        {
            get => threadPause;
            set
            {
                threadPause = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        public string ActiveConfigFile
        {
            get => activeConfigFile;
            set
            {
                activeConfigFile = value;
                OnPropertyChanged();
            }
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context)
        {
            Initialize();
        }

        private void Initialize()
        {
            instance = this;
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
}
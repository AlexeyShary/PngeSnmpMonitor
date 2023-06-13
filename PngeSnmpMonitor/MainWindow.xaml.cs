using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace PngeSnmpMonitor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string activeConfigFile;

        private Visibility devicePanelVisibility = Visibility.Hidden;

        private Device selectedDevice;

        private DeviceMonitorView selectedDeviceView;

        private readonly SynchronizationContext uiContext;

        public MainWindow()
        {
            // ActiveConfigFile = "StartUp";

            InitializeComponent();
            DataContext = this;
            uiContext = SynchronizationContext.Current;

            Stream str = Properties.Resources.AlarmSound;
            player = new SoundPlayer(str);

            ConfigFileService.LoadCommonConfig();

            LoadDeviceList();

            SetSoundIconsVisibility();
            SetActivateWindowIconsVisibility();

            soundThread = new BackgroundWorker();
            soundThread.DoWork += soundThread_DoWork;
            soundThread.RunWorkerAsync();
        }

        public string ActiveConfigFile
        {
            get => activeConfigFile;
            set
            {
                activeConfigFile = "PNGE SNMP Monitor - " + value;
                OnPropertyChanged();
            }
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

        public DeviceMonitorView SelectedDeviceView
        {
            get => selectedDeviceView;
            private set
            {
                selectedDeviceView = value;
                OnPropertyChanged();
            }
        }

        private void LoadDeviceList()
        {
            Device.DeviceList.Clear();

            if (Config.Instance.ActiveConfigFile == null || Config.Instance.ActiveConfigFile == string.Empty)
            {
                ActiveConfigFile = "No config";
                return;
            }

            if (!File.Exists(Config.Instance.ActiveConfigFile))
            {
                MessageBox.Show(
                    "Не удалось загрузить список устройств - последний используемый конфигурационный файл " +
                    ActiveConfigFile + " был перемещен или удален.");
                Config.Instance.ActiveConfigFile = string.Empty;
                ActiveConfigFile = "No config";
                return;
            }

            try
            {
                foreach (var d in ConfigFileService.LoadDeviceList(Config.Instance.ActiveConfigFile))
                    Device.DeviceList.Add(d);

                ActiveConfigFile = Config.Instance.ActiveConfigFile;
            }
            catch
            {
                MessageBox.Show("Не удалось прочитать конфигурационный файл " + ActiveConfigFile);
                Config.Instance.ActiveConfigFile = string.Empty;
                ActiveConfigFile = "No config";
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedDevice == null)
                return;

            SelectedDeviceView = new DeviceMonitorView(SelectedDevice);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ConfigFileService.SaveCommonConfig();

            if (System.Windows.MessageBox.Show("Сохранить конфигурацию?", "PNGE SNMP Monitor", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                ConfigFileService.SaveDeviceList(Device.DeviceList.ToList(), Config.Instance.ActiveConfigFile);
        }

        #region ActivateOnAlarm

        private Visibility activateWindowOnAlarmIconVisibility;

        public Visibility ActivateWindowOnAlarmIconVisibility
        {
            get => activateWindowOnAlarmIconVisibility;
            set
            {
                activateWindowOnAlarmIconVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility activateWindowOnAlarmDisabledIconVisibility;

        public Visibility ActivateWindowOnAlarmDisabledIconVisibility
        {
            get => activateWindowOnAlarmDisabledIconVisibility;
            set
            {
                activateWindowOnAlarmDisabledIconVisibility = value;
                OnPropertyChanged();
            }
        }

        private void SetActivateWindowIconsVisibility()
        {
            if (Config.Instance.ActivateWindowOnAlarm)
            {
                ActivateWindowOnAlarmIconVisibility = Visibility.Visible;
                ActivateWindowOnAlarmDisabledIconVisibility = Visibility.Hidden;

                if (SoundAlarmIsOn)
                    Topmost = true;
            }
            else
            {
                ActivateWindowOnAlarmIconVisibility = Visibility.Hidden;
                ActivateWindowOnAlarmDisabledIconVisibility = Visibility.Visible;

                Topmost = false;
            }
        }

        #endregion

        #region Sound

        private readonly SoundPlayer player;

        private bool soundAlarmIsOn;

        public bool SoundAlarmIsOn
        {
            get => soundAlarmIsOn;
            set
            {
                soundAlarmIsOn = value;
                OnPropertyChanged();

                SetSoundIconsVisibility();
            }
        }

        private void StartSoundAlarm()
        {
            player.PlayLooping();
            SoundAlarmIsOn = true;

            if (Config.Instance.ActivateWindowOnAlarm)
            {
                if (!IsVisible)
                    Show();

                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;

                Activate();
                Topmost = true;
                Focus();
            }
        }

        private void StopSoundAlarm()
        {
            player.Stop();
            SoundAlarmIsOn = false;

            Topmost = false;
        }

        private Visibility soundOnIconVisibility;

        public Visibility SoundOnIconVisibility
        {
            get => soundOnIconVisibility;
            set
            {
                soundOnIconVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility soundOffIconVisibility;

        public Visibility SoundOffIconVisibility
        {
            get => soundOffIconVisibility;
            set
            {
                soundOffIconVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility soundDisabledIconVisibility;

        public Visibility SoundDisabledIconVisibility
        {
            get => soundDisabledIconVisibility;
            set
            {
                soundDisabledIconVisibility = value;
                OnPropertyChanged();
            }
        }

        private void SetSoundIconsVisibility()
        {
            if (Config.Instance.AlarmSoundEnabledGlobal)
            {
                if (soundAlarmIsOn)
                {
                    SoundOnIconVisibility = Visibility.Visible;
                    SoundOffIconVisibility = Visibility.Hidden;
                    SoundDisabledIconVisibility = Visibility.Hidden;
                }
                else
                {
                    SoundOnIconVisibility = Visibility.Hidden;
                    SoundOffIconVisibility = Visibility.Visible;
                    SoundDisabledIconVisibility = Visibility.Hidden;
                }
            }
            else
            {
                SoundOnIconVisibility = Visibility.Hidden;
                SoundOffIconVisibility = Visibility.Hidden;
                SoundDisabledIconVisibility = Visibility.Visible;
            }
        }

        private readonly BackgroundWorker soundThread;

        private void soundThread_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(200);

                try
                {
                    var alarm = false;

                    if (!Config.Instance.AlarmSoundEnabledGlobal)
                    {
                        if (soundAlarmIsOn)
                            uiContext.Send(x => StopSoundAlarm(), null);

                        continue;
                    }

                    foreach (var device in Device.DeviceList)
                    {
                        if (device.DevicePollAlarmUnacknowledgment)
                            alarm = true;

                        foreach (var controlParameter in device.ParameterList)
                            if (controlParameter.ParameterAlarmUnacknowledgment)
                                alarm = true;
                    }

                    if (alarm)
                        if (!soundAlarmIsOn)
                            uiContext.Send(x => StartSoundAlarm(), null);

                    if (!alarm)
                        if (soundAlarmIsOn)
                            uiContext.Send(x => StopSoundAlarm(), null);
                }
                catch
                {
                }
            }
        }

        #endregion

        #region Menu

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            var openFileDlg = new OpenFileDialog();

            openFileDlg.DefaultExt = ".data";
            openFileDlg.Filter = "SNMP Device List (.data)|*.data";

            var result = openFileDlg.ShowDialog();

            if (result == true)
            {
                Config.Instance.ActiveConfigFile = openFileDlg.FileName;
                ConfigFileService.SaveCommonConfig();

                ActiveConfigFile = Config.Instance.ActiveConfigFile;

                LoadDeviceList();
            }
        }

        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            if (Config.Instance.ActiveConfigFile == null || Config.Instance.ActiveConfigFile == string.Empty)
            {
                var saveFileDlg = new SaveFileDialog();

                saveFileDlg.DefaultExt = ".data";
                saveFileDlg.Filter = "SNMP Device List (.data)|*.data";

                saveFileDlg.InitialDirectory = Application.StartupPath;

                var result = saveFileDlg.ShowDialog();

                if (result == true)
                {
                    Config.Instance.ActiveConfigFile = saveFileDlg.FileName;
                    ConfigFileService.SaveCommonConfig();

                    ActiveConfigFile = Config.Instance.ActiveConfigFile;
                }
                else
                {
                    return;
                }
            }

            ConfigFileService.SaveDeviceList(Device.DeviceList.ToList(), Config.Instance.ActiveConfigFile);
        }

        private void MenuItemSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDlg = new SaveFileDialog();

            saveFileDlg.DefaultExt = ".data";
            saveFileDlg.Filter = "SNMP Device List (.data)|*.data";

            saveFileDlg.InitialDirectory = Application.StartupPath;

            var result = saveFileDlg.ShowDialog();

            if (result == true)
            {
                Config.Instance.ActiveConfigFile = saveFileDlg.FileName;
                ConfigFileService.SaveCommonConfig();

                ActiveConfigFile = Config.Instance.ActiveConfigFile;
            }
            else
            {
                return;
            }

            ConfigFileService.SaveDeviceList(Device.DeviceList.ToList(), Config.Instance.ActiveConfigFile);
        }

        private void MenuItemConfig_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new ConfigWindow { Owner = this };
            configWindow.Show();
        }

        private void MenuItemLogs_Click(object sender, RoutedEventArgs e)
        {
            var logWindow = new LogWindow { Owner = this };
            logWindow.Show();
        }

        private void MenuItemInfo_Click(object sender, RoutedEventArgs e)
        {
            var programInfoWindow = new ProgramInfoWindow { Owner = this };
            programInfoWindow.Show();
        }

        private void MenuItemToggleSound_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.AlarmSoundEnabledGlobal = !Config.Instance.AlarmSoundEnabledGlobal;
            SetSoundIconsVisibility();
        }

        private void MenuItemToggleActivateOnAlarm_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.ActivateWindowOnAlarm = !Config.Instance.ActivateWindowOnAlarm;
            SetActivateWindowIconsVisibility();
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
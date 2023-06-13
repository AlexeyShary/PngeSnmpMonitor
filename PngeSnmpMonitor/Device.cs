using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using SnmpSharpNet;

namespace PngeSnmpMonitor
{
    [DataContract(IsReference = true)]
    public class Device : INotifyPropertyChanged
    {
        private static SynchronizationContext uiContext;

        public Device()
        {
            DeviceName = "New device";
            DeviceIP = "127.0.0.1";
            DeviceCommunity = "public";
            DeviceURL = "";

            DeviceList.Add(this);

            Initialize();
        }

        public Device(Device device)
        {
            DeviceName = device.DeviceName + " - копия";
            DeviceIP = device.DeviceIP;
            DeviceCommunity = device.DeviceCommunity;
            DeviceURL = device.DeviceURL;

            DeviceList.Add(this);

            Initialize();
        }

        public static ObservableCollection<Device> DeviceList { get; } = new ObservableCollection<Device>();

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context)
        {
            Initialize();
        }

        public Device Clone(Device device)
        {
            var newDevice = new Device(device);

            foreach (var controlParameter in device.ParameterList)
            {
                var newControlParameter = new ControlParameter(newDevice, controlParameter);
            }

            return newDevice;
        }

        private void Initialize()
        {
            uiContext = SynchronizationContext.Current;

            DeviceAlarm = false;
            DeviceWarning = false;

            AlarmMessageVisibility = Visibility.Hidden;

            if (devicePollingThread == null)
            {
                devicePollingThread = new BackgroundWorker();
                devicePollingThread.DoWork += devicePollingThread_DoWork;
                devicePollingThread.RunWorkerAsync();
            }
        }

        #region Properties

        private string deviceName;

        [DataMember]
        public string DeviceName
        {
            get => deviceName;
            set
            {
                deviceName = value;
                OnPropertyChanged();
            }
        }

        private string deviceIP;

        [DataMember]
        public string DeviceIP
        {
            get => deviceIP;
            set
            {
                deviceIP = value;
                OnPropertyChanged();
            }
        }

        private string deviceCommunity;

        [DataMember]
        public string DeviceCommunity
        {
            get => deviceCommunity;
            set
            {
                deviceCommunity = value;
                OnPropertyChanged();
            }
        }

        private string deviceURL;

        [DataMember]
        public string DeviceURL
        {
            get => deviceURL;
            set
            {
                deviceURL = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<ControlParameter> parameterList;

        [DataMember]
        public ObservableCollection<ControlParameter> ParameterList
        {
            get
            {
                if (parameterList == null)
                    parameterList = new ObservableCollection<ControlParameter>();

                return parameterList;
            }

            set
            {
                parameterList = value;
                OnPropertyChanged();
            }
        }

        private bool deviceAlarm;

        public bool DeviceAlarm
        {
            get => deviceAlarm;
            set
            {
                deviceAlarm = value;
                OnPropertyChanged();

                if (deviceAlarm)
                    AlarmVisibility = Visibility.Visible;
                else
                    AlarmVisibility = Visibility.Hidden;
            }
        }

        private Visibility alarmVisibility;

        public Visibility AlarmVisibility
        {
            get => alarmVisibility;
            private set
            {
                alarmVisibility = value;
                OnPropertyChanged();
            }
        }

        private bool devicePollAlarm;

        public bool DevicePollAlarm
        {
            get => devicePollAlarm;
            set
            {
                if (devicePollAlarm == false && value)
                {
                    DevicePollAlarmUnacknowledgment = true;
                    Logger.Instance.AddMessage(this, "Ошибка опроса устройства.");
                }

                if (devicePollAlarm && value == false)
                {
                    DevicePollAlarmUnacknowledgment = false;
                    Logger.Instance.AddMessage(this, "Опрос устройства возобновлен.");
                }

                if (value == false)
                    DevicePollAlarmUnacknowledgment = false;

                devicePollAlarm = value;
                OnPropertyChanged();

                if (devicePollAlarm)
                    DevicePollAlarmVisibility = Visibility.Visible;
                else
                    DevicePollAlarmVisibility = Visibility.Hidden;
            }
        }

        private bool devicePollAlarmUnacknowledgment;

        public bool DevicePollAlarmUnacknowledgment
        {
            get => devicePollAlarmUnacknowledgment;
            set
            {
                devicePollAlarmUnacknowledgment = value;
                OnPropertyChanged();
            }
        }

        private Visibility devicePollAlarmVisibility;

        public Visibility DevicePollAlarmVisibility
        {
            get => devicePollAlarmVisibility;
            private set
            {
                devicePollAlarmVisibility = value;
                OnPropertyChanged();
            }
        }

        private string alarmMessage;

        public string AlarmMessage
        {
            get => alarmMessage;
            set
            {
                alarmMessage = value;
                OnPropertyChanged();
            }
        }

        private Visibility alarmMessageVisibility;

        public Visibility AlarmMessageVisibility
        {
            get => alarmMessageVisibility;
            private set
            {
                alarmMessageVisibility = value;
                OnPropertyChanged();
            }
        }

        private bool deviceWarning;

        public bool DeviceWarning
        {
            get => deviceWarning;
            set
            {
                deviceWarning = value;
                OnPropertyChanged();

                if (deviceWarning)
                    WarningVisibility = Visibility.Visible;
                else
                    WarningVisibility = Visibility.Hidden;
            }
        }

        private Visibility warningVisibility;

        public Visibility WarningVisibility
        {
            get => warningVisibility;
            private set
            {
                warningVisibility = value;
                OnPropertyChanged();
            }
        }

        public void AcknowledgeAlarms()
        {
            DevicePollAlarmUnacknowledgment = false;

            foreach (var controlParameter in ParameterList)
                controlParameter.ParameterAlarmUnacknowledgment = false;
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

        #region DevicePollingThread

        private static BackgroundWorker devicePollingThread;

        private static void devicePollingThread_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                try
                {
                    foreach (var device in DeviceList)
                    {
                        DevicePoll(device);

                        var controlParametersLocalList = new List<ControlParameter>(device.ParameterList);

                        var parameterAlarm = false;
                        var parameterWarning = false;

                        foreach (var controlParameter in controlParametersLocalList)
                        {
                            if (controlParameter.ParameterAlarm)
                                parameterAlarm = true;

                            if (controlParameter.ParameterWarning)
                                parameterWarning = true;
                        }

                        uiContext.Send(x => device.DeviceAlarm = parameterAlarm || device.DevicePollAlarm, null);
                        uiContext.Send(x => device.DeviceWarning = parameterWarning, null);

                        var sleepTime = 25;

                        try
                        {
                            if (Config.Instance.ThreadPause >= 10)
                                sleepTime = Config.Instance.ThreadPause;
                        }
                        catch
                        {
                        }

                        Thread.Sleep(sleepTime);
                    }
                }
                catch
                {
                    Console.WriteLine("crush");
                }

                Thread.Sleep(200);
            }
        }

        // Каждый параметр в отдельности
        private static void DevicePoll(Device device)
        {
            OctetString community = null;
            try
            {
                community = new OctetString(device.DeviceCommunity);
            }
            catch
            {
                uiContext.Send(x =>
                {
                    device.AlarmMessage = "Некорректно задано Community name.";
                    device.AlarmMessageVisibility = Visibility.Visible;
                    device.DevicePollAlarm = true;
                }, null);

                return;
            }

            var param = new AgentParameters(community);
            param.Version = SnmpVersion.Ver2;

            IpAddress agent = null;
            try
            {
                agent = new IpAddress(device.DeviceIP);
            }
            catch
            {
                uiContext.Send(x =>
                {
                    device.AlarmMessage = "Некорректно задано IP адрес.";
                    device.AlarmMessageVisibility = Visibility.Visible;
                    device.DevicePollAlarm = true;
                }, null);

                return;
            }

            var target = new UdpTarget((IPAddress)agent, 161, 500, 0);

            foreach (var controlParameter in device.ParameterList)
            {
                var pdu = new Pdu(PduType.Get);

                try
                {
                    pdu.VbList.Add(controlParameter.ParameterOID);
                }
                catch
                {
                    uiContext.Send(x => controlParameter.ParameterValue = "Некорректный OID", null);
                    uiContext.Send(x => controlParameter.ParameterWarning = true, null);
                    uiContext.Send(x => controlParameter.LastUpdateTime = DateTime.Now, null);
                    continue;
                }

                SnmpV2Packet result = null;
                try
                {
                    result = (SnmpV2Packet)target.Request(pdu, param);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);

                    if (e.Message == "Invalid Object Identifier")
                    {
                        uiContext.Send(x =>
                        {
                            controlParameter.ParameterValue = "Некорректный OID";
                            controlParameter.ParameterWarning = true;
                            controlParameter.LastUpdateTime = DateTime.Now;
                        }, null);
                    }
                    else
                    {
                        uiContext.Send(x =>
                        {
                            device.AlarmMessage = "Ошибка опроса устройства.";
                            device.AlarmMessageVisibility = Visibility.Visible;
                            device.DevicePollAlarm = true;
                        }, null);

                        return;
                    }

                    continue;
                }

                if (result != null)
                {
                    var parameterWarning = false;
                    var limitAlarm = false;
                    var limitWarning = false;

                    var vbValue = result.Pdu.VbList[0].Value.ToString();

                    if (vbValue == "SNMP No-Such-Object" || vbValue == "SNMP No-Such-Instance" || vbValue == "Null")
                    {
                        vbValue = "Некорректный OID";
                        parameterWarning = true;
                    }

                    if (controlParameter.SelectedParameterType == ControlParameter.ParameterTypeEnum.Decimal)
                        if (controlParameter.ParameterHHEnabled || controlParameter.ParameterLLEnabled ||
                            controlParameter.ParameterHEnabled || controlParameter.ParameterLEnabled)
                            if (vbValue != "Некорректный OID")
                                try
                                {
                                    var floatValue = Convert.ToSingle(vbValue);
                                    var floatHHValue = Convert.ToSingle(controlParameter.ParameterHHValue);
                                    var floatLLValue = Convert.ToSingle(controlParameter.ParameterLLValue);
                                    var floatHValue = Convert.ToSingle(controlParameter.ParameterHValue);
                                    var floatLValue = Convert.ToSingle(controlParameter.ParameterLValue);

                                    if (controlParameter.ParameterHHEnabled)
                                        if (floatValue >= floatHHValue)
                                            limitAlarm = true;

                                    if (controlParameter.ParameterLLEnabled)
                                        if (floatValue <= floatLLValue)
                                            limitAlarm = true;

                                    if (controlParameter.ParameterHEnabled)
                                        if (floatValue >= floatHValue)
                                            limitWarning = true;

                                    if (controlParameter.ParameterLEnabled)
                                        if (floatValue <= floatLValue)
                                            limitWarning = true;
                                }
                                catch
                                {
                                    vbValue = "Ошибка преобразования";
                                    parameterWarning = true;
                                }

                    if (controlParameter.SelectedParameterType == ControlParameter.ParameterTypeEnum.List)
                        if (vbValue != "Некорректный OID")
                            foreach (var changePair in controlParameter.StringChangePairsList)
                                if (vbValue == changePair.StringFrom)
                                {
                                    if (changePair.Warning)
                                        limitWarning = true;

                                    if (changePair.Alarm)
                                        limitAlarm = true;
                                }

                    uiContext.Send(x => controlParameter.ParameterValue = vbValue, null);
                    uiContext.Send(x => controlParameter.ParameterWarning = parameterWarning || limitWarning, null);
                    uiContext.Send(x => controlParameter.ParameterAlarm = limitAlarm, null);
                    uiContext.Send(x => controlParameter.LastUpdateTime = DateTime.Now, null);
                }
                else
                {
                    Console.WriteLine("Device poll fault");
                    uiContext.Send(x => device.DevicePollAlarm = true, null);
                    return;
                }
            }

            uiContext.Send(x => device.AlarmMessageVisibility = Visibility.Hidden, null);
            uiContext.Send(x => device.DevicePollAlarm = false, null);
        }

        #endregion
    }
}
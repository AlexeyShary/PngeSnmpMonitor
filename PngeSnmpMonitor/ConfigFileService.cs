using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace PngeSnmpMonitor
{
    public static class ConfigFileService
    {
        #region DeviceList

        // const string DEVICE_LIST_FILENAME = "DeviceList.data";

        public static List<Device> LoadDeviceList(string deviceListPath)
        {
            var deviceList = new List<Device>();

            var dataFormatter = new DataContractSerializer(typeof(List<Device>));

            try
            {
                using (var fs = new FileStream(deviceListPath, FileMode.OpenOrCreate))
                {
                    try
                    {
                        deviceList = dataFormatter.ReadObject(fs) as List<Device>;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                Console.WriteLine("Fail open file " + deviceListPath);
            }

            return deviceList;
        }

        public static void SaveDeviceList(List<Device> deviceList, string deviceListPath)
        {
            var dataFormatter = new DataContractSerializer(typeof(List<Device>));

            try
            {
                using (var fileStream = new FileStream(deviceListPath, FileMode.Create))
                {
                    dataFormatter.WriteObject(fileStream, deviceList);
                }
            }
            catch
            {
                Console.WriteLine("Fail save file " + deviceListPath);
            }
        }

        #endregion

        #region CommonConfig

        private const string COMMON_CONFIG_FILENAME = "Config.data";

        public static void LoadCommonConfig()
        {
            var dataFormatter = new DataContractSerializer(typeof(Config));

            using (var fs = new FileStream(COMMON_CONFIG_FILENAME, FileMode.OpenOrCreate))
            {
                try
                {
                    dataFormatter.ReadObject(fs);
                }
                catch
                {
                }
            }
        }

        public static void SaveCommonConfig()
        {
            var dataFormatter = new DataContractSerializer(typeof(Config));

            using (var fileStream = new FileStream(COMMON_CONFIG_FILENAME, FileMode.Create))
            {
                dataFormatter.WriteObject(fileStream, Config.Instance);
            }
        }

        #endregion
    }
}
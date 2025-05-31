using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AovClass
{
    class DeviceSupportWrapper
    {
        public List<string> deviceList;

        public DeviceSupportWrapper()
        {
            deviceList = new List<string>();
        }

        public DeviceSupportWrapper(byte[] bytes)
        {
            deviceList = new List<string>();
            int start;
            if (bytes[0] == 'M' && bytes[1] == 'S' && bytes[2] == 'E' && bytes[3] == 'S')
                start = BitConverter.ToInt32(bytes, 132);
            else
                start = 0;
            if (start == bytes.Length)
                return;
            int count;
            while (start < bytes.Length)
            {
                count = BitConverter.ToInt32(bytes, start) + 4;
                string device = Encoding.UTF8.GetString(bytes[(start + 12)..(start + count + 2)]);
                deviceList.Add(device);
                start += count;
            }
        }

        public void AddNewDevice(string deviceCode)
        {
            deviceList.Add(deviceCode);
        }

        public byte[] getBytes()
        {
            byte[] bytes = new byte[] { 77, 83, 69, 83, 7, 0, 0, 0, 25, 0, 0, 0, 3, 0, 0, 0, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 85, 84, 70, 45, 56, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 49, 49, 48, 102, 102, 102, 51, 57, 54, 102, 102, 52, 52, 99, 97, 57, 99, 55, 51, 98, 97, 101, 100, 52, 57, 100, 56, 101, 48, 48, 50, 50, 0, 0, 0, 0, 140, 0, 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < deviceList.Count; i++)
            {
                bytes = ArrayExtension.MergeArray(bytes, BitConverter.GetBytes(deviceList[i].Length + 10),
                        BitConverter.GetBytes(i + 1),
                        BitConverter.GetBytes(deviceList[i].Length + 1),
                        Encoding.UTF8.GetBytes(deviceList[i]), new byte[] { 0, 0 });
            }
            bytes = bytes.ReplaceSubArray(12, 16, BitConverter.GetBytes(deviceList.Count));
            return bytes;
        }
    }
}

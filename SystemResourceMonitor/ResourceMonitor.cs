using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;


namespace SystemResourceMonitor
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class ResourceMonitor
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private PerformanceCounter diskCounter;
        private PerformanceCounterCategory networkCategory;
        private string[] networkInterfaces;

        public ResourceMonitor()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
            networkCategory = new PerformanceCounterCategory("Network Interface");
            networkInterfaces = networkCategory.GetInstanceNames();
        }

        public float GetCpuUsage()
        {
            // Первый вызов NextValue() может вернуть 0, поэтому вызываем дважды с задержкой
            cpuCounter.NextValue();
            Thread.Sleep(1000);
            return cpuCounter.NextValue();
        }

        public float GetAvailableMemory()
        {
            return ramCounter.NextValue();
        }

        public float GetDiskUsage()
        {
            diskCounter.NextValue();
            Thread.Sleep(1000);
            return diskCounter.NextValue();
        }

        public float GetNetworkUsage()
        {
            float totalBytesSent = 0;
            float totalBytesReceived = 0;

            foreach (var nic in networkInterfaces)
            {
                using (var bytesSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", nic))
                using (var bytesReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", nic))
                {
                    totalBytesSent += bytesSentCounter.NextValue();
                    totalBytesReceived += bytesReceivedCounter.NextValue();
                }
            }

            // Возвращаем суммарную сетевую активность в байтах за секунду
            return totalBytesSent + totalBytesReceived;
        }
    }

}

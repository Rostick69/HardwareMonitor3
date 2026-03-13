using System;
using System.Collections.Generic;
using System.Management;
using HardwareMonitor.Models;

namespace HardwareMonitor.Services
{
    /// <summary>
    /// Сервис для получения информации о сетевых адаптерах через WMI
    /// </summary>
    public class NetworkMonitor
    {
        /// <summary>
        /// Получает список сетевых адаптеров (только физические)
        /// </summary>
        /// <returns>Список NetworkAdapterInfo</returns>
        public List<NetworkAdapterInfo> GetNetworkAdapters()
        {
            List<NetworkAdapterInfo> adapters = new List<NetworkAdapterInfo>();

            try
            {
                string query = "SELECT Name, MACAddress, NetEnabled, Speed FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject adapter in searcher.Get())
                    {
                        NetworkAdapterInfo info = new NetworkAdapterInfo
                        {
                            Name = adapter["Name"]?.ToString() ?? "Неизвестно",
                            MacAddress = adapter["MACAddress"]?.ToString() ?? "Неизвестно",
                            IsConnected = Convert.ToBoolean(adapter["NetEnabled"] ?? false),
                            SpeedBitsPerSec = Convert.ToInt64(adapter["Speed"] ?? 0)
                        };
                        adapters.Add(info);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении информации о сетевых адаптерах: {ex.Message}", ex);
            }

            return adapters;
        }
    }
}
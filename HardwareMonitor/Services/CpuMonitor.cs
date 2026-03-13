using System;
using System.Management;
using HardwareMonitor.Models;

namespace HardwareMonitor.Services
{
    /// <summary>
    /// Сервис для получения информации о процессоре через WMI
    /// </summary>
    public class CpuMonitor
    {
        /// <summary>
        /// Получает полную информацию о процессоре
        /// </summary>
        /// <returns>Объект CpuInfo с данными о процессоре</returns>
        public CpuInfo GetCpuInfo()
        {
            CpuInfo cpuInfo = new CpuInfo();
            string query = "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, " +
                           "MaxClockSpeed, Manufacturer, Architecture FROM Win32_Processor";

            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject processor in searcher.Get())
                    {
                        cpuInfo.Name = processor["Name"]?.ToString() ?? "Неизвестно";
                        cpuInfo.CoreCount = Convert.ToInt32(processor["NumberOfCores"] ?? 0);
                        cpuInfo.ThreadCount = Convert.ToInt32(processor["NumberOfLogicalProcessors"] ?? 0);
                        cpuInfo.BaseFrequency = Convert.ToInt32(processor["MaxClockSpeed"] ?? 0);
                        cpuInfo.Manufacturer = processor["Manufacturer"]?.ToString() ?? "Неизвестно";

                        // Архитектура: 0 = x86, 9 = x64
                        int arch = Convert.ToInt32(processor["Architecture"] ?? 0);
                        cpuInfo.Architecture = (arch == 9) ? "x64" : "x86";
                        break; // Берём только первый процессор
                    }
                }

                cpuInfo.LoadPercentage = GetCpuLoad();
            }
            catch (ManagementException ex)
            {
                throw new Exception($"Ошибка при получении информации о процессоре: {ex.Message}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception("Недостаточно прав для доступа к WMI.", ex);
            }

            return cpuInfo;
        }

        /// <summary>
        /// Получает текущую загрузку процессора
        /// </summary>
        /// <returns>Процент загрузки процессора</returns>
        private double GetCpuLoad()
        {
            try
            {
                string query = "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject perfData in searcher.Get())
                    {
                        return Convert.ToDouble(perfData["PercentProcessorTime"] ?? 0);
                    }
                }
            }
            catch
            {
                return 0;
            }
            return 0;
        }
    }
}
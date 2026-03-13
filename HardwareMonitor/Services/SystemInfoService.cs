using System;
using System.Management;
using HardwareMonitor.Models;

namespace HardwareMonitor.Services
{
    /// <summary>
    /// Сервис для получения системной информации через WMI
    /// </summary>
    public class SystemInfoService
    {
        /// <summary>
        /// Получает системную информацию
        /// </summary>
        /// <returns>Объект SystemInfo с данными о системе</returns>
        public SystemInfo GetSystemInfo()
        {
            SystemInfo systemInfo = new SystemInfo();

            try
            {
                string osQuery = "SELECT Caption, Version, OSArchitecture, CSName FROM Win32_OperatingSystem";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(osQuery))
                {
                    foreach (ManagementObject operatingSystem in searcher.Get())
                    {
                        systemInfo.OsName = operatingSystem["Caption"]?.ToString() ?? "Неизвестно";
                        systemInfo.OsVersion = operatingSystem["Version"]?.ToString() ?? "Неизвестно";
                        systemInfo.Architecture = operatingSystem["OSArchitecture"]?.ToString() ?? "Неизвестно";
                        systemInfo.ComputerName = operatingSystem["CSName"]?.ToString() ?? "Неизвестно";
                        break;
                    }
                }

                // Получаем имя текущего пользователя из Environment
                systemInfo.UserName = Environment.UserName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении системной информации: {ex.Message}", ex);
            }

            return systemInfo;
        }
    }
}
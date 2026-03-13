using System;
using System.Collections.Generic;
using System.Management;
using HardwareMonitor.Models;

namespace HardwareMonitor.Services
{
    /// <summary>
    /// Сервис для получения информации о дисках через WMI
    /// </summary>
    public class DiskMonitor
    {
        /// <summary>
        /// Получает информацию о всех дисках системы
        /// </summary>
        /// <returns>Объект DiskInfo с данными о физических и логических дисках</returns>
        public DiskInfo GetDiskInfo()
        {
            DiskInfo diskInfo = new DiskInfo();

            try
            {
                diskInfo.PhysicalDisks = GetPhysicalDisks();
                diskInfo.LogicalDisks = GetLogicalDisks();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении информации о дисках: {ex.Message}", ex);
            }

            return diskInfo;
        }

        /// <summary>
        /// Получает список физических дисков
        /// </summary>
        private List<PhysicalDiskInfo> GetPhysicalDisks()
        {
            List<PhysicalDiskInfo> disks = new List<PhysicalDiskInfo>();

            try
            {
                string query = "SELECT Model, Size, MediaType FROM Win32_DiskDrive";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject physicalDisk in searcher.Get())
                    {
                        PhysicalDiskInfo disk = new PhysicalDiskInfo
                        {
                            Model = physicalDisk["Model"]?.ToString() ?? "Неизвестно",
                            SizeBytes = Convert.ToInt64(physicalDisk["Size"] ?? 0),
                            MediaType = physicalDisk["MediaType"]?.ToString() ?? "Неизвестно"
                        };
                        disks.Add(disk);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении физических дисков: {ex.Message}", ex);
            }

            return disks;
        }

        /// <summary>
        /// Получает список логических дисков (разделов)
        /// </summary>
        private List<LogicalDiskInfo> GetLogicalDisks()
        {
            List<LogicalDiskInfo> disks = new List<LogicalDiskInfo>();

            try
            {
                // DriveType=3 означает локальные жёсткие диски (HDD/SSD)
                string query = "SELECT DeviceID, Size, FreeSpace, FileSystem " +
                               "FROM Win32_LogicalDisk WHERE DriveType=3";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject logicalDisk in searcher.Get())
                    {
                        LogicalDiskInfo disk = new LogicalDiskInfo
                        {
                            DriveLetter = logicalDisk["DeviceID"]?.ToString() ?? "",
                            TotalSizeBytes = Convert.ToInt64(logicalDisk["Size"] ?? 0),
                            FreeSizeBytes = Convert.ToInt64(logicalDisk["FreeSpace"] ?? 0),
                            FileSystem = logicalDisk["FileSystem"]?.ToString() ?? "Неизвестно"
                        };
                        disks.Add(disk);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении логических дисков: {ex.Message}", ex);
            }

            return disks;
        }
    }
}
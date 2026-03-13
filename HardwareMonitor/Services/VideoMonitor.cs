using System;
using System.Management;
using HardwareMonitor.Models;

namespace HardwareMonitor.Services
{
    /// <summary>
    /// Сервис для получения информации о видеокарте через WMI
    /// </summary>
    public class VideoMonitor
    {
        /// <summary>
        /// Получает информацию о видеокарте
        /// </summary>
        /// <returns>Объект VideoControllerInfo с данными о видеокарте</returns>
        public VideoControllerInfo GetVideoInfo()
        {
            VideoControllerInfo videoInfo = new VideoControllerInfo();

            try
            {
                string query = "SELECT Name, AdapterRAM, DriverVersion, CurrentRefreshRate, VideoModeDescription FROM Win32_VideoController";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject videoController in searcher.Get())
                    {
                        videoInfo.Name = videoController["Name"]?.ToString() ?? "Неизвестно";
                        videoInfo.VideoMemoryBytes = Convert.ToInt64(videoController["AdapterRAM"] ?? 0);
                        videoInfo.DriverVersion = videoController["DriverVersion"]?.ToString() ?? "Неизвестно";
                        videoInfo.CurrentRefreshRate = Convert.ToInt32(videoController["CurrentRefreshRate"] ?? 0);
                        videoInfo.VideoModeDescription = videoController["VideoModeDescription"]?.ToString() ?? "Неизвестно";
                        break; // Берём первую видеокарту (основную)
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении информации о видеокарте: {ex.Message}", ex);
            }

            return videoInfo;
        }
    }
}
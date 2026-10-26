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
                string query = "SELECT Name, AdapterRAM, DriverVersion, CurrentRefreshRate, VideoModeDescription, CurrentHorizontalResolution, CurrentVerticalResolution FROM Win32_VideoController";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject videoController in searcher.Get())
                    {
                        videoInfo.Name = videoController["Name"]?.ToString() ?? "Неизвестно";
                        // AdapterRAM — 32-битное поле, поэтому для карт больше 4 ГБ WMI показывает максимум 4 ГБ.
                        // Пробуем взять точный объём из реестра драйвера, если не получилось — оставляем значение WMI
                        long adapterRam = Convert.ToInt64(videoController["AdapterRAM"] ?? 0);
                        long registryRam = GetVideoMemoryFromRegistry();
                        videoInfo.VideoMemoryBytes = registryRam > adapterRam ? registryRam : adapterRam;
                        videoInfo.DriverVersion = videoController["DriverVersion"]?.ToString() ?? "Неизвестно";
                        videoInfo.CurrentRefreshRate = Convert.ToInt32(videoController["CurrentRefreshRate"] ?? 0);
                        // Разрешение собираем из ширины и высоты экрана, а если их нет — берём описание режима
                        long width = Convert.ToInt64(videoController["CurrentHorizontalResolution"] ?? 0);
                        long height = Convert.ToInt64(videoController["CurrentVerticalResolution"] ?? 0);
                        videoInfo.VideoModeDescription = width > 0 && height > 0
                            ? $"{width} × {height}"
                            : videoController["VideoModeDescription"]?.ToString() ?? "Неизвестно";
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

        /// <summary>
        /// Читает объём видеопамяти из реестра (HardwareInformation.qwMemorySize).
        /// Возвращает 0, если значение недоступно
        /// </summary>
        private long GetVideoMemoryFromRegistry()
        {
            const string classKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
            long max = 0;
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(classKey))
                {
                    if (key == null) return 0;
                    foreach (string sub in key.GetSubKeyNames())
                    {
                        if (sub.Length != 4) continue; // подразделы видеоадаптеров: 0000, 0001, ...
                        try
                        {
                            using (var adapter = key.OpenSubKey(sub))
                            {
                                object value = adapter?.GetValue("HardwareInformation.qwMemorySize");
                                long bytes = 0;
                                if (value is long l) bytes = l;
                                else if (value is int i) bytes = (uint)i;
                                else if (value is byte[] raw && raw.Length >= 8) bytes = BitConverter.ToInt64(raw, 0);
                                if (bytes > max) max = bytes;
                            }
                        }
                        catch (System.Security.SecurityException)
                        {
                            // Нет прав на этот подраздел — пропускаем
                        }
                    }
                }
            }
            catch
            {
                return 0;
            }
            return max;
        }
    }
}
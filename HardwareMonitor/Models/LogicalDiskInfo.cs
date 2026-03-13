namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения информации о логическом диске
    /// </summary>
    public class LogicalDiskInfo
    {
        // Буква диска (например, "C:")
        public string DriveLetter { get; set; }

        // Общий размер раздела в байтах
        public long TotalSizeBytes { get; set; }

        // Свободное место на разделе в байтах
        public long FreeSizeBytes { get; set; }

        // Файловая система (NTFS, FAT32, exFAT)
        public string FileSystem { get; set; }

        /// <summary>
        /// Вычисляемое свойство - процент использования диска
        /// </summary>
        public double UsagePercent
        {
            get
            {
                if (TotalSizeBytes <= 0)
                {
                    return 0.0;
                }
                return (double)(TotalSizeBytes - FreeSizeBytes) / TotalSizeBytes * 100.0;
            }
        }

        public LogicalDiskInfo()
        {
            DriveLetter = "";
            FileSystem = "Неизвестно";
        }
    }
}
namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения информации о физическом диске
    /// </summary>
    public class PhysicalDiskInfo
    {
        // Модель диска
        public string Model { get; set; }

        // Размер диска в байтах
        public long SizeBytes { get; set; }

        // Тип носителя
        public string MediaType { get; set; }

        public PhysicalDiskInfo()
        {
            Model = "Неизвестно";
            MediaType = "Неизвестно";
        }
    }
}
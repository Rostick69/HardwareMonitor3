namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения информации об отдельном модуле памяти (планке RAM)
    /// </summary>
    public class MemoryModule
    {
        // Производитель модуля памяти
        public string Manufacturer { get; set; }

        // Объем модуля в байтах
        public long CapacityBytes { get; set; }

        // Частота модуля в МГц
        public int SpeedMHz { get; set; }

        public MemoryModule()
        {
            Manufacturer = "Неизвестно";
        }
    }
}
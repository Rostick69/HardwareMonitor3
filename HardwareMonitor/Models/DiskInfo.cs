using System.Collections.Generic;

namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения информации о дисках системы
    /// </summary>
    public class DiskInfo
    {
        // Список физических дисков
        public List<PhysicalDiskInfo> PhysicalDisks { get; set; }

        // Список логических дисков
        public List<LogicalDiskInfo> LogicalDisks { get; set; }

        /// <summary>
        /// Конструктор - инициализирует коллекции
        /// </summary>
        public DiskInfo()
        {
            PhysicalDisks = new List<PhysicalDiskInfo>();
            LogicalDisks = new List<LogicalDiskInfo>();
        }
    }
}
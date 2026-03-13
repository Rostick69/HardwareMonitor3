namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения системной информации
    /// </summary>
    public class SystemInfo
    {
        // Название операционной системы (например, "Microsoft Windows 11 Pro")
        public string OsName { get; set; }

        // Версия ОС (например, "10.0.22000")
        public string OsVersion { get; set; }

        // Архитектура системы (например, "64-разрядная")
        public string Architecture { get; set; }

        // Имя компьютера в сети
        public string ComputerName { get; set; }

        // Имя текущего пользователя
        public string UserName { get; set; }

        public SystemInfo()
        {
            OsName = "Неизвестно";
            OsVersion = "Неизвестно";
            Architecture = "Неизвестно";
            ComputerName = "Неизвестно";
            UserName = "Неизвестно";
        }
    }
}
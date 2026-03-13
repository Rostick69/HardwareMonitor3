namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения информации о видеокарте
    /// </summary>
    public class VideoControllerInfo
    {
        // Название видеокарты
        public string Name { get; set; }

        // Объём видеопамяти в байтах
        public long VideoMemoryBytes { get; set; }

        // Версия драйвера
        public string DriverVersion { get; set; }

        // Частота обновления (Гц)
        public int CurrentRefreshRate { get; set; }

        // Разрешение экрана (например, 1920x1080)
        public string VideoModeDescription { get; set; }

        public VideoControllerInfo()
        {
            Name = "Неизвестно";
            DriverVersion = "Неизвестно";
            VideoModeDescription = "Неизвестно";
        }
    }
}
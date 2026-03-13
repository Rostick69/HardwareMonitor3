namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения информации о сетевом адаптере
    /// </summary>
    public class NetworkAdapterInfo
    {
        // Имя адаптера
        public string Name { get; set; }

        // MAC-адрес
        public string MacAddress { get; set; }

        // Подключен ли адаптер к сети
        public bool IsConnected { get; set; }

        // Скорость в бит/с
        public long SpeedBitsPerSec { get; set; }

        public NetworkAdapterInfo()
        {
            Name = "Неизвестно";
            MacAddress = "Неизвестно";
        }
    }
}
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

        // Состояние подключения в виде текста для интерфейса
        public string ConnectedText => IsConnected ? "Да" : "Нет";

        // Скорость в бит/с (как возвращает WMI)
        public long SpeedBitsPerSec { get; set; }

        // Скорость в Мбит/с — для отображения в интерфейсе
        public double SpeedMbps => SpeedBitsPerSec / 1000000.0;

        public NetworkAdapterInfo()
        {
            Name = "Неизвестно";
            MacAddress = "Неизвестно";
        }
    }
}
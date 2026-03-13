using System;

namespace HardwareMonitor.Models
{
    /// <summary>
    /// Класс для хранения информации о процессоре
    /// </summary>
    public class CpuInfo
    {
        // Название процессора
        public string Name { get; set; }

        // Количество физических ядер
        public int CoreCount { get; set; }

        // Количество логических потоков
        public int ThreadCount { get; set; }

        // Базовая частота процессора в МГц
        public int BaseFrequency { get; set; }

        // Текущая загрузка процессора в процентах
        public double LoadPercentage { get; set; }

        // Производитель процессора
        public string Manufacturer { get; set; }

        // Архитектура процессора
        public string Architecture { get; set; }

        public CpuInfo()
        {
            Name = "Неизвестно";
            Manufacturer = "Неизвестно";
            Architecture = "Неизвестно";
        }
    }
}
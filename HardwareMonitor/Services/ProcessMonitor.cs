using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HardwareMonitor.Models;

namespace HardwareMonitor.Services
{
    /// <summary>
    /// Сервис для мониторинга процессов с вычислением загрузки CPU
    /// </summary>
    public class ProcessMonitor
    {
        // Словарь для хранения предыдущего значения процессорного времени каждого процесса
        private Dictionary<int, TimeSpan> _previousCpuTimes = new Dictionary<int, TimeSpan>();
        private DateTime _lastCheckTime = DateTime.UtcNow;

        /// <summary>
        /// Получает список процессов с текущей загрузкой CPU и использованием памяти
        /// </summary>
        public List<ProcessInfoModel> GetProcesses()
        {
            List<ProcessInfoModel> processes = new List<ProcessInfoModel>();
            DateTime now = DateTime.UtcNow;
            double elapsedSeconds = (now - _lastCheckTime).TotalSeconds;
            if (elapsedSeconds < 0.01) elapsedSeconds = 0.01; // защита от деления на ноль
            _lastCheckTime = now;

            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    TimeSpan currentCpuTime = proc.TotalProcessorTime;
                    double cpuUsage = 0;

                    if (_previousCpuTimes.TryGetValue(proc.Id, out TimeSpan previousCpuTime))
                    {
                        double cpuUsedMs = (currentCpuTime - previousCpuTime).TotalMilliseconds;
                        // Формула: загрузка = (использованное процессорное время) / (кол-во ядер * прошедшее время в секундах * 1000) * 100
                        cpuUsage = cpuUsedMs / (Environment.ProcessorCount * elapsedSeconds * 1000) * 100;
                        if (cpuUsage > 100) cpuUsage = 100; // ограничим
                    }

                    _previousCpuTimes[proc.Id] = currentCpuTime;

                    processes.Add(new ProcessInfoModel
                    {
                        Id = proc.Id,
                        Name = proc.ProcessName,
                        Status = proc.Responding ? "Запущен" : "Не отвечает",
                        CpuUsage = Math.Round(cpuUsage, 1),
                        MemoryBytes = proc.WorkingSet64
                    });
                }
                catch
                {
                    // Пропускаем процессы, к которым нет доступа
                    continue;
                }
            }

            // Очищаем словарь от завершённых процессов
            var currentIds = processes.Select(p => p.Id).ToHashSet();
            var toRemove = _previousCpuTimes.Keys.Where(id => !currentIds.Contains(id)).ToList();
            foreach (var id in toRemove)
                _previousCpuTimes.Remove(id);

            return processes.OrderByDescending(p => p.CpuUsage).ToList();
        }
    }
}
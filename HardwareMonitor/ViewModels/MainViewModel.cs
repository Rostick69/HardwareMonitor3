using HardwareMonitor.Models;
using HardwareMonitor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;          // для диалогов сохранения
using Newtonsoft.Json;          // для работы с JSON
using System.IO;                // для работы с файлами
using System.Text;              // для кодировки UTF-8

namespace HardwareMonitor.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly CpuMonitor _cpuMonitor;
        private readonly MemoryMonitor _memoryMonitor;
        private readonly DiskMonitor _diskMonitor;
        private readonly VideoMonitor _videoMonitor;
        private readonly NetworkMonitor _networkMonitor;
        private readonly SystemInfoService _systemInfoService;
        private readonly ProcessMonitor _processMonitor;
        private DispatcherTimer _updateTimer;
        private bool _isRefreshing;
        public ICommand ExportTxtCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportJsonCommand { get; }



        private CpuInfo _cpuInfo;
        public CpuInfo CpuInfo
        {
            get => _cpuInfo;
            set => SetProperty(ref _cpuInfo, value);
        }

        private MemoryInfo _memoryInfo;
        public MemoryInfo MemoryInfo
        {
            get => _memoryInfo;
            set => SetProperty(ref _memoryInfo, value);
        }

        private DiskInfo _diskInfo;
        public DiskInfo DiskInfo
        {
            get => _diskInfo;
            set => SetProperty(ref _diskInfo, value);
        }

        private VideoControllerInfo _videoInfo;
        public VideoControllerInfo VideoInfo
        {
            get => _videoInfo;
            set => SetProperty(ref _videoInfo, value);
        }

        private List<NetworkAdapterInfo> _networkAdapters;
        public List<NetworkAdapterInfo> NetworkAdapters
        {
            get => _networkAdapters;
            set => SetProperty(ref _networkAdapters, value);
        }

        private SystemInfo _systemInfo;
        public SystemInfo SystemInfo
        {
            get => _systemInfo;
            set => SetProperty(ref _systemInfo, value);
        }

        private ObservableCollection<ProcessInfoModel> _processes = new ObservableCollection<ProcessInfoModel>();
        public ObservableCollection<ProcessInfoModel> Processes
        {
            get => _processes;
            set => SetProperty(ref _processes, value);
        }

        private ProcessInfoModel _selectedProcess;
        public ProcessInfoModel SelectedProcess
        {
            get => _selectedProcess;
            set => SetProperty(ref _selectedProcess, value);
        }

        // Команды
        public ICommand RefreshCommand { get; }
        public ICommand KillProcessCommand { get; }

        public MainViewModel()
        {
            _cpuMonitor = new CpuMonitor();
            _memoryMonitor = new MemoryMonitor();
            _diskMonitor = new DiskMonitor();
            _videoMonitor = new VideoMonitor();
            _networkMonitor = new NetworkMonitor();
            _systemInfoService = new SystemInfoService();
            _processMonitor = new ProcessMonitor();

            RefreshCommand = new RelayCommand(async () => await RefreshDataAsync());
            KillProcessCommand = new RelayCommand(KillSelectedProcess, () => SelectedProcess != null);

            ExportTxtCommand = new RelayCommand(ExportToTxt);
            ExportCsvCommand = new RelayCommand(ExportToCsv);
            ExportJsonCommand = new RelayCommand(ExportToJson);

            // Таймер для автообновления каждые 3 секунды
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(3);
            _updateTimer.Tick += async (s, e) =>
            {
                if (!_isRefreshing)
                {
                    await RefreshDataAsync();
                }
            };
            _updateTimer.Start();
            // Сразу обновляем данные
            _ = RefreshDataAsync();
        }

        // Обновление всех данных
        private async Task RefreshDataAsync()
        {
            if (_isRefreshing) return; // защита от повторного входа
            _isRefreshing = true;

            try
            {
                var cpuTask = Task.Run(() => _cpuMonitor.GetCpuInfo());
                var memTask = Task.Run(() => _memoryMonitor.GetMemoryInfo());
                var diskTask = Task.Run(() => _diskMonitor.GetDiskInfo());
                var videoTask = Task.Run(() => _videoMonitor.GetVideoInfo());
                var netTask = Task.Run(() => _networkMonitor.GetNetworkAdapters());
                var sysTask = Task.Run(() => _systemInfoService.GetSystemInfo());
                var procTask = Task.Run(() => _processMonitor.GetProcesses());

                await Task.WhenAll(cpuTask, memTask, diskTask, videoTask, netTask, sysTask, procTask);

                CpuInfo = await cpuTask;
                MemoryInfo = await memTask;
                DiskInfo = await diskTask;
                VideoInfo = await videoTask;
                NetworkAdapters = await netTask;
                SystemInfo = await sysTask;

                var newProcesses = await procTask;

                // Обновляем коллекцию процессов в UI-потоке
                Application.Current.Dispatcher.Invoke(() =>
                {
                    int? selectedId = SelectedProcess?.Id;
                    Processes.Clear();
                    foreach (var p in newProcesses)
                        Processes.Add(p);
                    if (selectedId.HasValue)
                        SelectedProcess = Processes.FirstOrDefault(p => p.Id == selectedId.Value);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        // Завершение выбранного процесса
        private void KillSelectedProcess()
        {
            if (SelectedProcess == null)
            {
                MessageBox.Show("Выберите процесс из списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string processName = SelectedProcess.Name;
            int processId = SelectedProcess.Id;

            MessageBoxResult result = MessageBox.Show(
                $"Завершить процесс \"{processName}\" (ID: {processId})?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                Process proc = Process.GetProcessById(processId);
                proc.Kill();
                MessageBox.Show($"Процесс \"{processName}\" завершён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = RefreshDataAsync(); // Обновить список
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось завершить процесс: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExportToTxt()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FileName = $"HardwareReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using (var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8))
                {
                    writer.WriteLine("=== ОТЧЁТ О СОСТОЯНИИ СИСТЕМЫ ===");
                    writer.WriteLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine();

                    writer.WriteLine("--- ПРОЦЕССОР ---");
                    writer.WriteLine($"Название: {CpuInfo?.Name}");
                    writer.WriteLine($"Производитель: {CpuInfo?.Manufacturer}");
                    writer.WriteLine($"Архитектура: {CpuInfo?.Architecture}");
                    writer.WriteLine($"Ядер: {CpuInfo?.CoreCount}");
                    writer.WriteLine($"Потоков: {CpuInfo?.ThreadCount}");
                    writer.WriteLine($"Базовая частота: {CpuInfo?.BaseFrequency} МГц");
                    writer.WriteLine($"Загрузка: {CpuInfo?.LoadPercentage:F1}%");
                    writer.WriteLine();

                    writer.WriteLine("--- ПАМЯТЬ ---");
                    writer.WriteLine($"Всего: {MemoryInfo?.TotalMemoryBytes / (1024 * 1024):N0} МБ");
                    writer.WriteLine($"Доступно: {MemoryInfo?.AvailableMemoryBytes / (1024 * 1024):N0} МБ");
                    writer.WriteLine($"Использовано: {MemoryInfo?.UsagePercentage:F1}%");
                    writer.WriteLine("Модули памяти:");
                    if (MemoryInfo?.Modules != null && MemoryInfo.Modules.Count > 0)
                    {
                        foreach (var module in MemoryInfo.Modules)
                        {
                            writer.WriteLine($"  {module.Manufacturer} - {module.CapacityBytes / (1024 * 1024):N0} МБ - {module.SpeedMHz} МГц");
                        }
                    }
                    else writer.WriteLine("  Нет данных");
                    writer.WriteLine();

                    writer.WriteLine("--- ДИСКИ ---");
                    writer.WriteLine("Физические диски:");
                    foreach (var disk in DiskInfo?.PhysicalDisks ?? new List<PhysicalDiskInfo>())
                    {
                        writer.WriteLine($"  {disk.Model} - {disk.SizeBytes / (1024 * 1024 * 1024):N2} ГБ - {disk.MediaType}");
                    }
                    writer.WriteLine("Логические диски:");
                    foreach (var disk in DiskInfo?.LogicalDisks ?? new List<LogicalDiskInfo>())
                    {
                        writer.WriteLine($"  {disk.DriveLetter} - {disk.TotalSizeBytes / (1024 * 1024 * 1024):N2} ГБ всего, {disk.FreeSizeBytes / (1024 * 1024 * 1024):N2} ГБ свободно ({disk.UsagePercent:F1}% занято) - {disk.FileSystem}");
                    }
                    writer.WriteLine();

                    writer.WriteLine("--- ВИДЕОКАРТА ---");
                    writer.WriteLine($"Название: {VideoInfo?.Name}");
                    writer.WriteLine($"Память: {VideoInfo?.VideoMemoryBytes / (1024 * 1024):N0} МБ");
                    writer.WriteLine($"Драйвер: {VideoInfo?.DriverVersion}");
                    writer.WriteLine($"Разрешение: {VideoInfo?.VideoModeDescription}");
                    writer.WriteLine();

                    writer.WriteLine("--- СЕТЕВЫЕ АДАПТЕРЫ ---");
                    foreach (var net in NetworkAdapters ?? new List<NetworkAdapterInfo>())
                    {
                        writer.WriteLine($"  {net.Name} - MAC: {net.MacAddress} - Подключен: {net.IsConnected} - Скорость: {net.SpeedBitsPerSec / 1_000_000} Мбит/с");
                    }
                    writer.WriteLine();

                    writer.WriteLine("--- СИСТЕМА ---");
                    writer.WriteLine($"ОС: {SystemInfo?.OsName}");
                    writer.WriteLine($"Версия: {SystemInfo?.OsVersion}");
                    writer.WriteLine($"Архитектура: {SystemInfo?.Architecture}");
                    writer.WriteLine($"Компьютер: {SystemInfo?.ComputerName}");
                    writer.WriteLine($"Пользователь: {SystemInfo?.UserName}");
                }

                MessageBox.Show($"Отчёт успешно сохранён: {dialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExportToCsv()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"Processes_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                using (var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8))
                {
                    // Заголовки
                    writer.WriteLine("ID;Имя;Статус;CPU %;Память (МБ)");

                    // Данные
                    foreach (var proc in Processes)
                    {
                        writer.WriteLine($"{proc.Id};{proc.Name};{proc.Status};{proc.CpuUsage:F1};{proc.MemoryMB:F2}");
                    }
                }

                MessageBox.Show($"Данные процессов сохранены в CSV: {dialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте CSV: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExportToJson()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = $"HardwareReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                // Создаём объект для сериализации
                var exportData = new
                {
                    ExportDate = DateTime.Now,
                    Cpu = CpuInfo,
                    Memory = new
                    {
                        TotalMemoryMB = MemoryInfo?.TotalMemoryBytes / (1024 * 1024),
                        AvailableMemoryMB = MemoryInfo?.AvailableMemoryBytes / (1024 * 1024),
                        UsagePercent = MemoryInfo?.UsagePercentage,
                        Modules = MemoryInfo?.Modules
                    },
                    Disks = new
                    {
                        Physical = DiskInfo?.PhysicalDisks,
                        Logical = DiskInfo?.LogicalDisks
                    },
                    Video = VideoInfo,
                    Network = NetworkAdapters,
                    System = SystemInfo,
                    Processes = Processes.Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Status,
                        p.CpuUsage,
                        MemoryMB = p.MemoryMB
                    })
                };

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    Culture = System.Globalization.CultureInfo.InvariantCulture
                };
                string json = JsonConvert.SerializeObject(exportData, settings);
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);

                MessageBox.Show($"Данные сохранены в JSON: {dialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте JSON: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }


}
using HardwareMonitor.Models;
using HardwareMonitor.Services;
using Microsoft.Win32;          // для диалогов сохранения
using Newtonsoft.Json;          // для работы с JSON
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;                // для работы с файлами
using System.Linq;
using System.Management;
using System.Text;              // для кодировки UTF-8
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;     // PointCollection для графика
using System.Windows.Threading;

namespace HardwareMonitor.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // Размеры области графика загрузки CPU (в единицах WPF) и число хранимых точек
        public const double ChartWidth = 600;
        public const double ChartHeight = 150;
        private const int ChartPoints = 60;

        private readonly CpuMonitor _cpuMonitor;
        private readonly MemoryMonitor _memoryMonitor;
        private readonly DiskMonitor _diskMonitor;
        private readonly VideoMonitor _videoMonitor;
        private readonly NetworkMonitor _networkMonitor;
        private readonly SystemInfoService _systemInfoService;
        private readonly ProcessMonitor _processMonitor;
        private readonly DispatcherTimer _updateTimer;
        private bool _isRefreshing;

        // История загрузки CPU и время последних обновлений (для графика и таблицы производительности)
        private readonly List<double> _cpuHistory = new List<double>();
        private readonly List<long> _refreshTimes = new List<long>();

        public ICommand RefreshCommand { get; }
        public ICommand KillProcessCommand { get; }
        public ICommand ExportTxtCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportJsonCommand { get; }

        #region Данные для отображения

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

        public ObservableCollection<ProcessInfoModel> Processes { get; } = new ObservableCollection<ProcessInfoModel>();

        private ProcessInfoModel _selectedProcess;
        public ProcessInfoModel SelectedProcess
        {
            get => _selectedProcess;
            set => SetProperty(ref _selectedProcess, value);
        }

        // Точки линии графика загрузки CPU
        private PointCollection _cpuChartPoints = new PointCollection();
        public PointCollection CpuChartPoints
        {
            get => _cpuChartPoints;
            set => SetProperty(ref _cpuChartPoints, value);
        }

        #endregion

        #region Настройки обновления и статус

        // Варианты интервала автообновления, секунд (WMI не рассчитан на частый опрос, поэтому не меньше 1 с)
        public List<int> IntervalOptions { get; } = new List<int> { 1, 2, 3, 5, 10, 30 };

        private int _selectedInterval = 3;
        public int SelectedInterval
        {
            get => _selectedInterval;
            set
            {
                if (SetProperty(ref _selectedInterval, value))
                    _updateTimer.Interval = TimeSpan.FromSeconds(value);
            }
        }

        private bool _isAutoRefresh = true;
        public bool IsAutoRefresh
        {
            get => _isAutoRefresh;
            set
            {
                if (SetProperty(ref _isAutoRefresh, value))
                {
                    if (value) _updateTimer.Start();
                    else _updateTimer.Stop();
                }
            }
        }

        private string _statusText = "Загрузка данных...";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private bool _hasErrors;
        public bool HasErrors
        {
            get => _hasErrors;
            set => SetProperty(ref _hasErrors, value);
        }

        private string _refreshStats = "";
        // Статистика времени обновления: последнее, среднее, минимум и максимум
        public string RefreshStats
        {
            get => _refreshStats;
            set => SetProperty(ref _refreshStats, value);
        }

        #endregion

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

            // Таймер автообновления. DispatcherTimer срабатывает в UI-потоке,
            // а сам опрос WMI выполняется в фоне через Task.Run
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_selectedInterval) };
            _updateTimer.Tick += async (s, e) => await RefreshDataAsync();
            _updateTimer.Start();

            _ = RefreshDataAsync();
        }

        /// <summary>
        /// Безопасный вызов одного сервиса в фоновом потоке.
        /// Ошибка одного компонента не мешает обновлению остальных — она попадает в список errors
        /// </summary>
        private static Task<T> RunSafe<T>(Func<T> func, string component, List<string> errors) where T : class
        {
            return Task.Run(() =>
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                    // Отдельно сообщаем о нехватке прав — некоторые WMI-классы требуют администратора
                    if (ex is UnauthorizedAccessException || ex.InnerException is UnauthorizedAccessException ||
                        (ex.InnerException is ManagementException me && me.ErrorCode == ManagementStatus.AccessDenied))
                        message = "недостаточно прав (запустите программу от имени администратора)";
                    lock (errors) errors.Add($"{component}: {message}");
                    return null;
                }
            });
        }

        // Обновление всех данных
        private async Task RefreshDataAsync()
        {
            if (_isRefreshing) return; // защита от повторного входа, если предыдущий опрос ещё идёт
            _isRefreshing = true;
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<string>();

            try
            {
                var cpuTask = RunSafe(() => _cpuMonitor.GetCpuInfo(), "Процессор", errors);
                var memTask = RunSafe(() => _memoryMonitor.GetMemoryInfo(), "Память", errors);
                var diskTask = RunSafe(() => _diskMonitor.GetDiskInfo(), "Диски", errors);
                var videoTask = RunSafe(() => _videoMonitor.GetVideoInfo(), "Видеокарта", errors);
                var netTask = RunSafe(() => _networkMonitor.GetNetworkAdapters(), "Сеть", errors);
                var sysTask = RunSafe(() => _systemInfoService.GetSystemInfo(), "Система", errors);
                var procTask = RunSafe(() => _processMonitor.GetProcesses(), "Процессы", errors);

                await Task.WhenAll(cpuTask, memTask, diskTask, videoTask, netTask, sysTask, procTask);
                stopwatch.Stop();

                // После await мы снова в UI-потоке, поэтому свойства можно менять напрямую.
                // Если компонент не удалось обновить, оставляем предыдущие данные
                CpuInfo = cpuTask.Result ?? CpuInfo;
                MemoryInfo = memTask.Result ?? MemoryInfo;
                DiskInfo = diskTask.Result ?? DiskInfo;
                VideoInfo = videoTask.Result ?? VideoInfo;
                NetworkAdapters = netTask.Result ?? NetworkAdapters;
                SystemInfo = sysTask.Result ?? SystemInfo;

                if (procTask.Result != null)
                    UpdateProcesses(procTask.Result);

                if (cpuTask.Result != null)
                    AddCpuPoint(cpuTask.Result.LoadPercentage);

                UpdateRefreshStats(stopwatch.ElapsedMilliseconds);

                HasErrors = errors.Count > 0;
                StatusText = HasErrors
                    ? "Ошибки: " + string.Join("; ", errors)
                    : $"Обновлено в {DateTime.Now:HH:mm:ss}";
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        // Обновление списка процессов с сохранением выделенного процесса
        private void UpdateProcesses(List<ProcessInfoModel> newProcesses)
        {
            int? selectedId = SelectedProcess?.Id;
            Processes.Clear();
            foreach (var p in newProcesses)
                Processes.Add(p);
            if (selectedId.HasValue)
                SelectedProcess = Processes.FirstOrDefault(p => p.Id == selectedId.Value);
        }

        // Добавление точки на график загрузки CPU (храним последние ChartPoints значений)
        private void AddCpuPoint(double load)
        {
            _cpuHistory.Add(load);
            if (_cpuHistory.Count > ChartPoints)
                _cpuHistory.RemoveAt(0);

            var points = new PointCollection();
            double step = ChartWidth / (ChartPoints - 1);
            // Последняя точка всегда у правого края, старые сдвигаются влево
            int offset = ChartPoints - _cpuHistory.Count;
            for (int i = 0; i < _cpuHistory.Count; i++)
            {
                double x = (offset + i) * step;
                double y = ChartHeight - _cpuHistory[i] / 100.0 * ChartHeight;
                points.Add(new Point(x, y));
            }
            CpuChartPoints = points;
        }

        // Статистика времени полного цикла обновления (нужна для таблицы производительности в отчёте)
        private void UpdateRefreshStats(long elapsedMs)
        {
            _refreshTimes.Add(elapsedMs);
            if (_refreshTimes.Count > 50)
                _refreshTimes.RemoveAt(0);

            RefreshStats = $"Обновление: {elapsedMs} мс (среднее {_refreshTimes.Average():F0} мс, " +
                           $"мин {_refreshTimes.Min()} мс, макс {_refreshTimes.Max()} мс, замеров: {_refreshTimes.Count})";
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
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                Process proc = Process.GetProcessById(processId);
                proc.Kill();
                MessageBox.Show($"Процесс \"{processName}\" завершён.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось завершить процесс: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Экспорт

        private static string Mb(long bytes) => (bytes / 1024.0 / 1024.0).ToString("N0");
        private static string Gb(long bytes) => (bytes / 1024.0 / 1024.0 / 1024.0).ToString("N2");

        // Все данные о системе в виде строк «раздел — параметр — значение».
        // Используется и для TXT, и для CSV, чтобы содержимое отчётов совпадало
        private List<string[]> BuildReportRows()
        {
            var rows = new List<string[]>();
            void Add(string section, string name, object value) => rows.Add(new[] { section, name, value?.ToString() ?? "" });

            Add("Процессор", "Название", CpuInfo?.Name);
            Add("Процессор", "Производитель", CpuInfo?.Manufacturer);
            Add("Процессор", "Архитектура", CpuInfo?.Architecture);
            Add("Процессор", "Ядер", CpuInfo?.CoreCount);
            Add("Процессор", "Потоков", CpuInfo?.ThreadCount);
            Add("Процессор", "Базовая частота, МГц", CpuInfo?.BaseFrequency);
            Add("Процессор", "Текущая частота, МГц", CpuInfo?.CurrentFrequency);
            Add("Процессор", "Загрузка, %", CpuInfo?.LoadPercentage.ToString("F1"));

            if (MemoryInfo != null)
            {
                Add("Память", "Всего, МБ", Mb(MemoryInfo.TotalMemoryBytes));
                Add("Память", "Используется, МБ", Mb(MemoryInfo.UsedMemoryBytes));
                Add("Память", "Доступно, МБ", Mb(MemoryInfo.AvailableMemoryBytes));
                Add("Память", "Использование, %", MemoryInfo.UsagePercentage.ToString("F1"));
                int n = 1;
                foreach (var m in MemoryInfo.Modules)
                    Add("Память", $"Модуль {n++}", $"{m.Manufacturer}, {Mb(m.CapacityBytes)} МБ, {m.SpeedMHz} МГц");
            }

            foreach (var d in DiskInfo?.PhysicalDisks ?? new List<PhysicalDiskInfo>())
                Add("Физический диск", d.Model, $"{Gb(d.SizeBytes)} ГБ, {d.MediaType}");
            foreach (var d in DiskInfo?.LogicalDisks ?? new List<LogicalDiskInfo>())
                Add("Логический диск", d.DriveLetter,
                    $"всего {Gb(d.TotalSizeBytes)} ГБ, свободно {Gb(d.FreeSizeBytes)} ГБ, занято {d.UsagePercent:F1}%, {d.FileSystem}");

            Add("Видеокарта", "Название", VideoInfo?.Name);
            Add("Видеокарта", "Видеопамять, МБ", VideoInfo == null ? "" : Mb(VideoInfo.VideoMemoryBytes));
            Add("Видеокарта", "Версия драйвера", VideoInfo?.DriverVersion);
            Add("Видеокарта", "Разрешение", VideoInfo?.VideoModeDescription);
            Add("Видеокарта", "Частота обновления, Гц", VideoInfo?.CurrentRefreshRate);

            foreach (var a in NetworkAdapters ?? new List<NetworkAdapterInfo>())
                Add("Сеть", a.Name, $"MAC {a.MacAddress}, {(a.IsConnected ? "подключён" : "отключён")}, {a.SpeedMbps:N0} Мбит/с");

            Add("Система", "ОС", SystemInfo?.OsName);
            Add("Система", "Версия", SystemInfo?.OsVersion);
            Add("Система", "Архитектура", SystemInfo?.Architecture);
            Add("Система", "Имя компьютера", SystemInfo?.ComputerName);
            Add("Система", "Пользователь", SystemInfo?.UserName);
            return rows;
        }

        private static string AskFileName(string filter, string defaultName)
        {
            var dialog = new SaveFileDialog { Filter = filter, FileName = defaultName };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        private void ExportToTxt()
        {
            string file = AskFileName("Текстовый файл (*.txt)|*.txt", $"HardwareReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            if (file == null) return;
            try
            {
                using (var writer = new StreamWriter(file, false, Encoding.UTF8))
                {
                    writer.WriteLine("=== ОТЧЁТ О СОСТОЯНИИ СИСТЕМЫ ===");
                    writer.WriteLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    string section = null;
                    foreach (var row in BuildReportRows())
                    {
                        if (row[0] != section)
                        {
                            section = row[0];
                            writer.WriteLine();
                            writer.WriteLine($"--- {section.ToUpper()} ---");
                        }
                        writer.WriteLine($"{row[1].TrimEnd(':')}: {row[2]}");
                    }
                }
                MessageBox.Show($"Отчёт сохранён: {file}", "Экспорт TXT", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // CSV с разделителем «;» — так его правильно открывает русский Excel.
        // Кодировка UTF-8 с BOM, чтобы Excel не испортил кириллицу
        private void ExportToCsv()
        {
            string file = AskFileName("CSV для Excel (*.csv)|*.csv", $"HardwareReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
            if (file == null) return;
            try
            {
                using (var writer = new StreamWriter(file, false, new UTF8Encoding(true)))
                {
                    writer.WriteLine("Раздел;Параметр;Значение");
                    foreach (var row in BuildReportRows())
                        writer.WriteLine(string.Join(";", row.Select(CsvEscape)));

                    writer.WriteLine();
                    writer.WriteLine("Процессы");
                    writer.WriteLine("ID;Имя;Статус;CPU %;Память (МБ)");
                    foreach (var p in Processes)
                        writer.WriteLine($"{p.Id};{CsvEscape(p.Name)};{p.Status};{p.CpuUsage:F1};{p.MemoryMB:F1}");
                }
                MessageBox.Show($"Данные сохранены в CSV: {file}", "Экспорт CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте CSV: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Экранирование значения для CSV: если есть ; или кавычки — берём в кавычки
        private static string CsvEscape(string value)
        {
            if (value == null) return "";
            if (value.Contains(";") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private void ExportToJson()
        {
            string file = AskFileName("JSON (*.json)|*.json", $"HardwareReport_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json");
            if (file == null) return;
            try
            {
                var exportData = new
                {
                    ExportDate = DateTime.Now,
                    Cpu = CpuInfo,
                    Memory = new
                    {
                        TotalMemoryMB = MemoryInfo?.TotalMemoryBytes / (1024 * 1024),
                        UsedMemoryMB = MemoryInfo?.UsedMemoryBytes / (1024 * 1024),
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
                    Processes = Processes.Select(p => new { p.Id, p.Name, p.Status, p.CpuUsage, p.MemoryMB })
                };

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    Culture = System.Globalization.CultureInfo.InvariantCulture
                };
                File.WriteAllText(file, JsonConvert.SerializeObject(exportData, settings), Encoding.UTF8);
                MessageBox.Show($"Данные сохранены в JSON: {file}", "Экспорт JSON", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте JSON: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}

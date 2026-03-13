using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HardwareMonitor.Models
{
    /// <summary>
    /// Модель данных процесса с поддержкой INotifyPropertyChanged
    /// </summary>
    public class ProcessInfoModel : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _status;
        private double _cpuUsage;
        private long _memoryBytes;

        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        public double CpuUsage
        {
            get { return _cpuUsage; }
            set { SetProperty(ref _cpuUsage, value); }
        }

        public long MemoryBytes
        {
            get { return _memoryBytes; }
            set { SetProperty(ref _memoryBytes, value); }
        }

        public double MemoryMB
        {
            get { return _memoryBytes / 1024.0 / 1024.0; }
        }

        public ProcessInfoModel()
        {
            _name = "Неизвестно";
            _status = "Неизвестно";
            _cpuUsage = 0;
            _memoryBytes = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
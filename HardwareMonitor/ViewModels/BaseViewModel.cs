using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HardwareMonitor.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        // Событие, которое срабатывает при изменении свойства
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для вызова события PropertyChanged
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Вспомогательный метод для установки значения свойства с автоматическим уведомлением
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            // Если значение не изменилось, не вызываем событие
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
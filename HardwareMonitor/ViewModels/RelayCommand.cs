using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HardwareMonitor.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        // Конструктор для команды БЕЗ параметра
        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(
                execute != null ? new Action<object>(p => execute()) : null,
                canExecute != null ? new Func<object, bool>(p => canExecute()) : null)
        {
        }

        // Конструктор для команды С параметром
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Событие изменения возможности выполнения команды
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Проверка возможности выполнения
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // Выполнение команды
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
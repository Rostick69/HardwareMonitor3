using System;
using System.Windows;

namespace HardwareMonitor
{
    public partial class App : Application
    {
        public App()
        {
            // Обработка исключений в UI-потоке
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            // Обработка исключений в других потоках
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Ошибка в интерфейсе: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Чтобы приложение не закрылось
        }

        private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"Критическая ошибка: {ex?.Message}\n\n{ex?.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
using System;
using System.IO;
using System.Windows;
using BayeganyApp.Database;

namespace BayeganyApp
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, args) =>
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), $"[Dispatcher] {DateTime.Now}: {args.Exception}\n");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), $"[AppDomain] {DateTime.Now}: {args.ExceptionObject}\n");
            };

            try
            {
                // ۱. مقداردهی اولیه پایگاه‌داده و جداول
                DatabaseHelper.InitializeDatabase();

                // ۲. باز کردن پنجره اصلی
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), $"[Startup Catch] {DateTime.Now}: {ex}\n");
                MessageBox.Show($"خطا در راه‌اندازی اولیه نرم‌افزار:\n{ex.Message}", "خطای سیستمی", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}

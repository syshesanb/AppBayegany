using System;
using System.IO;
using System.Windows;
using BayeganyApp.Database;
using BayeganyApp.Models;

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
                // جلوگیری از بسته شدن خودکار برنامه پس از بسته شدن پنجره لاگین
                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // ۱. مقداردهی اولیه پایگاه‌داده و جداول
                DatabaseHelper.InitializeDatabase();

                // ۲. نمایش پنجره احراز هویت و لاگین
                var loginWindow = new Windows.LoginWindow();
                bool? result = loginWindow.ShowDialog();

                if (result == true && UserSession.IsLoggedIn)
                {
                    // ۳. باز کردن پنجره اصلی در صورت احراز هویت موفق
                    var mainWindow = new MainWindow();
                    MainWindow = mainWindow;
                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                    mainWindow.Show();
                }
                else
                {
                    // در صورت بستن یا انصراف از لاگین، برنامه خاتمه می‌یابد
                    Shutdown();
                }
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

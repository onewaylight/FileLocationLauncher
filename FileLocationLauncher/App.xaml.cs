using FileLocationLauncher.Services;
using FileLocationLauncher.ViewModels;
using FileLocationLauncher.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.Windows;

namespace FileLocationLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    services.AddSingleton<IFileLocationService, FileLocationService>();

                    // Choose between dialog service implementations:
                    // Option 1: Pure WPF approach (recommended)
                    services.AddSingleton<IDialogService, DialogService>();

                    // Option 2: Windows Forms fallback (if you need more reliable folder browsing)
                    // services.AddSingleton<IDialogService, DialogServiceWithWindowsApi>();

                    services.AddSingleton<IFileOperationService, FileOperationService>();

                    // Register ViewModels
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<AddEditFileLocationViewModel>();

                    // Register Views
                    services.AddTransient<MainWindow>();
                    services.AddTransient<AddEditFileLocationWindow>();
                })
                .Build();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}

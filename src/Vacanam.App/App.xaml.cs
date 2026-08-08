using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;
using Application = System.Windows.Application;
using Vacanam.App.Services;
using Vacanam.App.ViewModels;
using Vacanam.Audio.DependencyInjection;
using Vacanam.Core.Models;
using Vacanam.Infrastructure.Configuration;
using Vacanam.Infrastructure.DependencyInjection;
using Vacanam.Input.DependencyInjection;
using Vacanam.Speech.DependencyInjection;
using Vacanam.Windows.DependencyInjection;

namespace Vacanam.App;

/// <summary>
/// WPF Application entry point.
/// Bootstraps the Generic Host, wires DI, logging, and the application lifecycle.
/// No window is shown on startup — Vacanam runs as a tray application.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog early so startup messages are captured
        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vacanam", "Logs");
        Directory.CreateDirectory(logsDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(logsDir, "vacanam-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
#if DEBUG
            .WriteTo.Debug()
#endif
            .CreateLogger();

        try
        {
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    // Ensure local data directories exist
                    services.AddVacanamDirectories();

                    // Phase 1: register null stubs for all interfaces
                    services.AddVacanamServices();

                    // Phase 2: replace hotkey + foreground window stubs with Win32 implementations
                    services.AddVacanamWindowsServices();

                    // Phase 3: replace audio recorder stub with WASAPI capture
                    services.AddVacanamAudioServices();

                    // Phase 4: replace speech recognizer stub with Whisper.net
                    services.AddVacanamSpeechServices();

                    // Phase 5: replace text injector stub with CompositeTextInjector
                    services.AddVacanamInputServices();

                    // Register typed settings options
                    services.AddOptions<AppSettings>()
                        .Configure<SettingsManager>((opts, mgr) =>
                        {
                            var loaded = mgr.Load();
                            opts.General = loaded.General;
                            opts.Hotkeys = loaded.Hotkeys;
                            opts.Audio   = loaded.Audio;
                            opts.Speech  = loaded.Speech;
                            opts.Ai      = loaded.Ai;
                            opts.Privacy = loaded.Privacy;
                        });

                    // Settings manager
                    services.AddSingleton<SettingsManager>();

                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<SettingsViewModel>();
                    services.AddSingleton<RecordingOverlayViewModel>();

                    // Hosted services
                    services.AddHostedService<ApplicationLifetimeService>();
                })
                .Build();

            // Wire host stopping to WPF shutdown
            _host.Services.GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopped.Register(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        Log.Information("Host stopped — shutting down WPF application.");
                        Log.CloseAndFlush();
                        Shutdown();
                    });
                });

            await _host.StartAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during Vacanam startup.");
            Log.CloseAndFlush();
            MessageBox.Show(
                $"Vacanam failed to start:\n\n{ex.Message}\n\nCheck the log file at:\n{logsDir}",
                "Vacanam — Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            try
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during host shutdown.");
            }
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

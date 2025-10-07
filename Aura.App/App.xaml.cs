using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Aura.Core.Hardware;
using Aura.Core.Dependencies;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Aura.Providers.Tts;
using Aura.Providers.Video;
using Aura.Core.Orchestrator;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Aura.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private IHost _host;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            // Build the host with DI services
            _host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                    logging.AddConsole();
                    
                    // Add file logging (would use Serilog in a real implementation)
                    string logDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "AuraVideoStudio", "Logs");
                    
                    Directory.CreateDirectory(logDir);
                })
                .ConfigureServices((context, services) =>
                {
                    // Configuration
                    services.AddSingleton<AppSettings>(sp =>
                    {
                        // Load settings from appsettings.json
                        return new AppSettings();
                    });
                    
                    // Core services
                    services.AddSingleton<HardwareDetector>();
                    services.AddSingleton<DependencyManager>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<DependencyManager>>();
                        var httpClient = sp.GetRequiredService<HttpClient>();
                        
                        string manifestPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "AuraVideoStudio", "manifest.json");
                        
                        string downloadDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "AuraVideoStudio", "Downloads");
                        
                        return new DependencyManager(logger, httpClient, manifestPath, downloadDir);
                    });
                    
                    services.AddSingleton<VideoOrchestrator>();
                    
                    // HTTP client
                    services.AddHttpClient();
                    
                    // Provider registrations - register all implementations
                    
                    // LLM providers
                    services.AddTransient<RuleBasedLlmProvider>();
                    services.AddTransient<ILlmProvider>(sp =>
                    {
                        // In a real implementation, we would check settings to decide which provider to use
                        return sp.GetRequiredService<RuleBasedLlmProvider>();
                    });
                    
                    // TTS providers
                    services.AddTransient<WindowsTtsProvider>();
                    services.AddTransient<ITtsProvider>(sp =>
                    {
                        // In a real implementation, we would check settings to decide which provider to use
                        return sp.GetRequiredService<WindowsTtsProvider>();
                    });
                    
                    // Video composer
                    services.AddTransient<FfmpegVideoComposer>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<FfmpegVideoComposer>>();
                        
                        // In a real implementation, we would get this path from settings
                        string ffmpegPath = Path.Combine(
                            AppContext.BaseDirectory, "scripts", "ffmpeg", "ffmpeg.exe");
                        
                        return new FfmpegVideoComposer(logger, ffmpegPath);
                    });
                    services.AddTransient<IVideoComposer>(sp => sp.GetRequiredService<FfmpegVideoComposer>());
                    
                    // View models
                    services.AddTransient<ViewModels.CreateViewModel>();
                    services.AddTransient<ViewModels.StoryboardViewModel>();
                    services.AddTransient<ViewModels.RenderViewModel>();
                    services.AddTransient<ViewModels.PublishViewModel>();
                    services.AddTransient<ViewModels.SettingsViewModel>();
                    services.AddTransient<ViewModels.HardwareProfileViewModel>();
                    
                    // Main window
                    services.AddSingleton<MainWindow>();
                })
                .Build();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Start the app
            _host.Start();

            // Create and activate the main window
            m_window = _host.Services.GetRequiredService<MainWindow>();
            m_window.Activate();
        }

        private Window m_window;
    }

    public class AppSettings
    {
        // Settings properties go here
        // These would be loaded from appsettings.json in a real implementation
    }
}
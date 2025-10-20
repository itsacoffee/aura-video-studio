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
                    
                    // Register smart orchestration services
                    services.AddSingleton<Aura.Core.Services.Generation.ResourceMonitor>();
                    services.AddSingleton<Aura.Core.Services.Generation.StrategySelector>();
                    services.AddSingleton<Aura.Core.Services.Generation.VideoGenerationOrchestrator>();
                    services.AddSingleton<VideoOrchestrator>();
                    
                    // HTTP client
                    services.AddHttpClient();
                    
                    // Register FFmpeg locator for centralized FFmpeg path resolution
                    services.AddSingleton<Aura.Core.Dependencies.IFfmpegLocator>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILogger<Aura.Core.Dependencies.FfmpegLocator>>();
                        return new Aura.Core.Dependencies.FfmpegLocator(logger);
                    });
                    
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
                        var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
                        
                        // In a real implementation, we would get this path from settings
                        string configuredFfmpegPath = Path.Combine(
                            AppContext.BaseDirectory, "scripts", "ffmpeg", "ffmpeg.exe");
                        
                        return new FfmpegVideoComposer(logger, ffmpegLocator, configuredFfmpegPath);
                    });
                    services.AddTransient<IVideoComposer>(sp => sp.GetRequiredService<FfmpegVideoComposer>());
                    
                    // View models
                    services.AddTransient<ViewModels.CreateViewModel>();
                    services.AddTransient<ViewModels.StoryboardViewModel>();
                    services.AddTransient<ViewModels.RenderViewModel>();
                    services.AddTransient<ViewModels.PublishViewModel>();
                    services.AddTransient<ViewModels.SettingsViewModel>();
                    services.AddTransient<ViewModels.HardwareProfileViewModel>();
                    
                    // Views
                    services.AddTransient<Views.CreateView>();
                    services.AddTransient<Views.StoryboardView>();
                    services.AddTransient<Views.RenderView>();
                    services.AddTransient<Views.PublishView>();
                    services.AddTransient<Views.SettingsView>();
                    services.AddTransient<Views.HardwareProfileView>();
                    
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
        // Provider settings
        public string StableDiffusionUrl { get; set; } = "http://127.0.0.1:7860";
        public string OllamaUrl { get; set; } = "http://127.0.0.1:11434";
        public string FfmpegPath { get; set; } = "";
        public string FfprobePath { get; set; } = "";
        public string OutputDirectory { get; set; } = "";
        
        // API Keys (encrypted in production)
        public string OpenAiKey { get; set; } = "";
        public string ElevenLabsKey { get; set; } = "";
        public string PexelsKey { get; set; } = "";
        public string StabilityAiKey { get; set; } = "";
        
        // System settings
        public bool OfflineMode { get; set; } = false;
        public int UiScale { get; set; } = 100;
        public bool CompactMode { get; set; } = false;
    }
}
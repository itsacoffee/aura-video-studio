using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Microsoft.Extensions.Logging;
using Aura.App.Views;
using Aura.App.ViewModels;

namespace Aura.App
{
    public sealed partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MainWindow> _logger;
        private readonly HardwareDetector _hardwareDetector;
        private bool _isFirstRun = true;

        public MainWindow(
            IServiceProvider serviceProvider,
            ILogger<MainWindow> logger,
            HardwareDetector hardwareDetector)
        {
            this.InitializeComponent();
            
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hardwareDetector = hardwareDetector;
            
            Title = "Aura Video Studio";
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Main window loaded");
            
            // Check if this is first run
            if (_isFirstRun)
            {
                _isFirstRun = false;
                await CheckFirstRunAsync();
            }
            
            // Navigate to the create page by default
            NavView.SelectedItem = NavView.MenuItems[0];
            NavigateToPage("create");
        }
        
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigateToPage("settings");
            }
            else if (args.SelectedItem is NavigationViewItem item)
            {
                string tag = item.Tag?.ToString() ?? "";
                NavigateToPage(tag);
            }
        }
        
        private void NavigateToPage(string tag)
        {
            Type pageType = tag.ToLowerInvariant() switch
            {
                "create" => typeof(CreateView),
                "storyboard" => typeof(StoryboardView),
                "render" => typeof(RenderView),
                "publish" => typeof(PublishView),
                "library" => null, // To be implemented
                "hardware" => typeof(HardwareProfileView),
                "settings" => typeof(SettingsView),
                _ => null
            };
            
            if (pageType != null)
            {
                try
                {
                    // Create the page with DI
                    var page = ActivatorUtilities.CreateInstance(_serviceProvider, pageType) as Page;
                    if (page != null)
                    {
                        ContentFrame.Content = page;
                        StatusText.Text = $"Navigated to {tag}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to navigate to {Tag}", tag);
                    StatusText.Text = $"Error navigating to {tag}";
                }
            }
        }
        
        private async Task CheckFirstRunAsync()
        {
            try
            {
                // Update status
                StatusText.Text = "Detecting system hardware...";
                
                // Detect hardware
                var profile = await _hardwareDetector.DetectSystemAsync();
                
                // Update UI with hardware info
                EncoderInfoText.Text = $"Encoder: {(profile.EnableNVENC ? "NVENC" : "x264")}";
                
                // Show the hardware profile dialog for first run
                if (_isFirstRun)
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "Welcome to Aura Video Studio",
                        Content = "We've detected your hardware and configured the app accordingly. Would you like to review your hardware profile?",
                        PrimaryButtonText = "Review Profile",
                        SecondaryButtonText = "Later",
                        DefaultButton = ContentDialogButton.Primary
                    };
                    
                    var result = await dialog.ShowAsync();
                    
                    if (result == ContentDialogResult.Primary)
                    {
                        // Navigate to the hardware profile page
                        NavView.SelectedItem = NavView.MenuItems[6];
                    }
                }
                
                // Update status
                StatusText.Text = "Ready";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during first-run hardware detection");
                StatusText.Text = "Hardware detection failed";
            }
        }
    }
}
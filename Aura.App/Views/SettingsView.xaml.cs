using Microsoft.UI.Xaml.Controls;
using Aura.App.ViewModels;

namespace Aura.App.Views
{
    /// <summary>
    /// Settings view
    /// </summary>
    public sealed partial class SettingsView : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsView()
        {
            this.InitializeComponent();
        }

        public SettingsView(SettingsViewModel viewModel) : this()
        {
            ViewModel = viewModel;
        }
    }
}

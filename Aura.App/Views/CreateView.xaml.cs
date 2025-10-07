using Microsoft.UI.Xaml.Controls;
using Aura.App.ViewModels;

namespace Aura.App.Views
{
    /// <summary>
    /// Create/Brief wizard view
    /// </summary>
    public sealed partial class CreateView : Page
    {
        public CreateViewModel ViewModel { get; }

        public CreateView()
        {
            this.InitializeComponent();
            
            // In production, ViewModel would be injected via DI
            // For now, we'll set it in the MainWindow navigation
        }

        public CreateView(CreateViewModel viewModel) : this()
        {
            ViewModel = viewModel;
        }
    }
}

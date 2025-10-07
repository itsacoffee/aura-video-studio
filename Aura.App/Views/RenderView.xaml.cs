using Microsoft.UI.Xaml.Controls;
using Aura.App.ViewModels;

namespace Aura.App.Views
{
    /// <summary>
    /// Render/Export view
    /// </summary>
    public sealed partial class RenderView : Page
    {
        public RenderViewModel ViewModel { get; }

        public RenderView()
        {
            this.InitializeComponent();
        }

        public RenderView(RenderViewModel viewModel) : this()
        {
            ViewModel = viewModel;
        }
    }
}

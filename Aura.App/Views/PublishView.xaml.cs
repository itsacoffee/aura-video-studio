using Microsoft.UI.Xaml.Controls;
using Aura.App.ViewModels;

namespace Aura.App.Views
{
    /// <summary>
    /// Publish/Upload view
    /// </summary>
    public sealed partial class PublishView : Page
    {
        public PublishViewModel ViewModel { get; }

        public PublishView()
        {
            this.InitializeComponent();
        }

        public PublishView(PublishViewModel viewModel) : this()
        {
            ViewModel = viewModel;
        }
    }
}

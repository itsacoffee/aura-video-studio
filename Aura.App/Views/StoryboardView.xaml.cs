using Microsoft.UI.Xaml.Controls;
using Aura.App.ViewModels;

namespace Aura.App.Views
{
    /// <summary>
    /// Storyboard/Timeline editor view
    /// </summary>
    public sealed partial class StoryboardView : Page
    {
        public StoryboardViewModel ViewModel { get; }

        public StoryboardView()
        {
            this.InitializeComponent();
        }

        public StoryboardView(StoryboardViewModel viewModel) : this()
        {
            ViewModel = viewModel;
        }
    }
}

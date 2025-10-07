using Microsoft.UI.Xaml.Controls;
using Aura.App.ViewModels;

namespace Aura.App.Views
{
    /// <summary>
    /// Hardware profile and system information view
    /// </summary>
    public sealed partial class HardwareProfileView : Page
    {
        public HardwareProfileViewModel ViewModel { get; }

        public HardwareProfileView()
        {
            this.InitializeComponent();
        }

        public HardwareProfileView(HardwareProfileViewModel viewModel) : this()
        {
            ViewModel = viewModel;
        }
    }
}

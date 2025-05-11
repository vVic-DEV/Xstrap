using Bloxstrap.UI.ViewModels.Settings;

using System.Windows.Controls;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for AppearancePage.xaml
    /// </summary>
    public partial class AppearancePage
    {
        public AppearancePage()
        {
            InitializeComponent();
            this.DataContext = new AppearanceViewModel(this);
        }

        public void CustomThemeSelection(object sender, SelectionChangedEventArgs e)
        {
            AppearanceViewModel viewModel = (AppearanceViewModel)DataContext;

            viewModel.SelectedCustomTheme = (string)((ListBox)sender).SelectedItem;
            viewModel.SelectedCustomThemeName = viewModel.SelectedCustomTheme;

            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomTheme));
            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomThemeName));
        }
    }
}
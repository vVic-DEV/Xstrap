using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls; // For NavigationItem

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for AppearancePage.xaml
    /// </summary>
    public partial class AppearancePage
    {
        private readonly MainWindow _mainWindow;

        public AppearancePage()
        {
            InitializeComponent();

            this.DataContext = new AppearanceViewModel(this);

            _mainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;

            ListBoxNavigationItems.ItemsSource = MainWindow.MainNavigationItems;
        }

        public void CustomThemeSelection(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = (AppearanceViewModel)DataContext;

            viewModel.SelectedCustomTheme = (string)((ListBox)sender).SelectedItem;
            viewModel.SelectedCustomThemeName = viewModel.SelectedCustomTheme;

            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomTheme));
            viewModel.OnPropertyChanged(nameof(viewModel.SelectedCustomThemeName));
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxNavigationItems.SelectedItem is NavigationItem item)
            {
                var navItems = MainWindow.MainNavigationItems;

                int oldIndex = navItems.IndexOf(item);

                if (oldIndex > 0 && oldIndex < navItems.Count - 1)
                {
                    navItems.Move(oldIndex, oldIndex - 1);
                    ListBoxNavigationItems.SelectedIndex = oldIndex - 1;

                    _mainWindow.ApplyNavigationReorder();
                }
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxNavigationItems.SelectedItem is NavigationItem item)
            {
                var navItems = MainWindow.MainNavigationItems;

                int oldIndex = navItems.IndexOf(item);

                if (oldIndex >= 0 && oldIndex < navItems.Count - 2)
                {
                    navItems.Move(oldIndex, oldIndex + 1);
                    ListBoxNavigationItems.SelectedIndex = oldIndex + 1;

                    _mainWindow.ApplyNavigationReorder();
                }
            }
        }
    }
}

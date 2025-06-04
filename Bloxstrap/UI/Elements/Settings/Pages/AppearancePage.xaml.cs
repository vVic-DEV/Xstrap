using Bloxstrap.UI.ViewModels.Settings;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

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
            if (ListBoxNavigationItems.SelectedItem is not NavigationItem selectedItem)
                return;

            if (MainWindow.MainNavigationItems.Contains(selectedItem))
            {
                var navItems = MainWindow.MainNavigationItems;
                int index = navItems.IndexOf(selectedItem);

                if (index > 0)
                {
                    navItems.Move(index, index - 1);
                    ListBoxNavigationItems.SelectedItem = selectedItem;
                    _mainWindow.ApplyNavigationReorder();
                }
            }
            else if (_mainWindow.RootNavigation.Footer.Contains(selectedItem))
            {
                var footerList = _mainWindow.RootNavigation.Footer;
                int index = footerList.IndexOf(selectedItem);

                if (index > 0)
                {
                    footerList.Move(index, index - 1);
                    ListBoxNavigationItems.SelectedItem = selectedItem;
                    _mainWindow.ApplyNavigationReorder();
                }
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxNavigationItems.SelectedItem is not NavigationItem selectedItem)
                return;

            if (MainWindow.MainNavigationItems.Contains(selectedItem))
            {
                var navItems = MainWindow.MainNavigationItems;
                int index = navItems.IndexOf(selectedItem);

                if (index < navItems.Count - 1)
                {
                    navItems.Move(index, index + 1);
                    ListBoxNavigationItems.SelectedItem = selectedItem;
                    _mainWindow.ApplyNavigationReorder();
                }
            }
            else if (_mainWindow.RootNavigation.Footer.Contains(selectedItem))
            {
                var footerList = _mainWindow.RootNavigation.Footer;
                int index = footerList.IndexOf(selectedItem);

                if (index < footerList.Count - 1)
                {
                    footerList.Move(index, index + 1);
                    ListBoxNavigationItems.SelectedItem = selectedItem;
                    _mainWindow.ApplyNavigationReorder();
                }
            }
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

using Bloxstrap.UI.ViewModels.Settings;
using Wpf.Ui.Common;

namespace Bloxstrap.UI.Elements.Settings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        private Models.Persistable.WindowState _state => App.State.Prop.SettingsWindow;

        public static ObservableCollection<NavigationItem> MainNavigationItems { get; } = new ObservableCollection<NavigationItem>();
        public static ObservableCollection<NavigationItem> FooterNavigationItems { get; } = new ObservableCollection<NavigationItem>();

        public MainWindow(bool showAlreadyRunningWarning)
        {
            var viewModel = new MainWindowViewModel();

            viewModel.RequestSaveNoticeEvent += (_, _) => SettingsSavedSnackbar.Show();
            viewModel.RequestCloseWindowEvent += (_, _) => Close();

            DataContext = viewModel;

            InitializeComponent();

            App.Logger.WriteLine("MainWindow", "Initializing settings window");

            if (showAlreadyRunningWarning)
                ShowAlreadyRunningSnackbar();

            LoadState();

            var allItems = RootNavigation.Items.OfType<NavigationItem>().ToList();
            var allFooters = RootNavigation.Footer?.OfType<NavigationItem>().ToList() ?? new System.Collections.Generic.List<NavigationItem>();

            MainNavigationItems.Clear();
            FooterNavigationItems.Clear();

            foreach (var item in allItems)
                MainNavigationItems.Add(item);

            foreach (var item in allFooters)
                FooterNavigationItems.Add(item);

            RebuildNavigationItems();

            int lastPage = App.State.Prop.LastPage;
            if (lastPage >= 0 && lastPage < RootNavigation.Items.Count)
                RootNavigation.SelectedPageIndex = lastPage;
            else
                RootNavigation.SelectedPageIndex = 0;

            RootNavigation.Navigated += SaveNavigation!;

            void SaveNavigation(object? sender, RoutedNavigationEventArgs? e)
            {
                if (sender == null || e == null) return;

                if (RootNavigation.SelectedPageIndex >= 0 && RootNavigation.SelectedPageIndex < RootNavigation.Items.Count)
                    App.State.Prop.LastPage = RootNavigation.SelectedPageIndex;
            }
        }

        private void RebuildNavigationItems()
        {
            RootNavigation.Items.Clear();
            foreach (var item in MainNavigationItems)
                RootNavigation.Items.Add(item);

            if (RootNavigation.Footer == null)
                RootNavigation.Footer = new ObservableCollection<INavigationControl>();

            RootNavigation.Footer.Clear();
            foreach (var footerItem in FooterNavigationItems)
                RootNavigation.Footer.Add(footerItem);
        }

        public void ApplyNavigationReorder()
        {
            RebuildNavigationItems();
        }

        public void LoadState()
        {
            if (_state.Left > SystemParameters.VirtualScreenWidth)
                _state.Left = 0;

            if (_state.Top > SystemParameters.VirtualScreenHeight)
                _state.Top = 0;

            if (_state.Width > 0)
                this.Width = _state.Width;

            if (_state.Height > 0)
                this.Height = _state.Height;

            if (_state.Left > 0 && _state.Top > 0)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = _state.Left;
                this.Top = _state.Top;
            }
        }

        private async void ShowAlreadyRunningSnackbar()
        {
            await Task.Delay(500); // wait for everything to finish loading
            AlreadyRunningSnackbar.Show();
        }

        #region INavigationWindow methods

        public Frame GetFrame() => RootFrame;

        public INavigation GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) => RootNavigation.PageService = pageService;

        public void ShowWindow() => Show();

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        private void WpfUiWindow_Closing(object sender, CancelEventArgs e)
        {
            if (App.FastFlags.Changed || App.PendingSettingTasks.Any())
            {
                var result = Frontend.ShowMessageBox(Strings.Menu_UnsavedChanges, MessageBoxImage.Warning, MessageBoxButton.YesNo);

                if (result != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            _state.Width = this.Width;
            _state.Height = this.Height;

            _state.Top = this.Top;
            _state.Left = this.Left;

            App.State.Save();
        }

        private void WpfUiWindow_Closed(object sender, EventArgs e)
        {
            if (App.LaunchSettings.TestModeFlag.Active)
                LaunchHandler.LaunchRoblox(LaunchMode.Player);
            else
                App.SoftTerminate();
        }
    }
}

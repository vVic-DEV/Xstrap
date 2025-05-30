using Microsoft.Win32;
using System.Windows;
using Bloxstrap.Resources;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for FastFlagEditorSettingsDialog.xaml
    /// </summary>
    public partial class FastFlagEditorSettingsDialog
    {
        public FastFlagEditorSettingViewModel ViewModel { get; } = new();

        public FastFlagEditorSettingsDialog()
        {
            InitializeComponent();
            ViewModel.LoadSettings();
            DataContext = ViewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveSettings();
            MessageBox.Show("Settings saved, press on the close button now.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ChannelPage.xaml
    /// </summary>
    public partial class ChannelPage
    {
        private ChannelViewModel? ViewModel => DataContext as ChannelViewModel;

        public ChannelPage()
        {
            DataContext = new ChannelViewModel();
            InitializeComponent();

            ViewModel?.ApplyHardwareAcceleration();
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle)
            {
                bool isChecked = toggle.IsChecked == true;
                if (ViewModel != null)
                {
                    ViewModel.IsHardwareAccelerationEnabled = isChecked;
                    ViewModel.ApplyHardwareAcceleration();
                }
            }
        }

    }
}

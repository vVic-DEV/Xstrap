using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;

namespace Bloxstrap.UI.Elements.Base
{
    public abstract class WpfUiWindow : UiWindow
    {
        private readonly IThemeService _themeService = new ThemeService();

        public WpfUiWindow()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2; // index for CustomTheme merged dictionary

            _themeService.SetTheme(App.Settings.Prop.Theme.GetFinal() == Enums.Theme.Light ? ThemeType.Light : ThemeType.Dark);
            _themeService.SetSystemAccent();

            // there doesn't seem to be a way to query the name for merged dictionaries
            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(App.Settings.Prop.Theme.GetFinal())}.xaml") };
            Application.Current.Resources.MergedDictionaries[customThemeIndex] = dict;

#if QA_BUILD
            this.BorderBrush = System.Windows.Media.Brushes.Red;
            this.BorderThickness = new Thickness(4);
#endif
        }

        // In your settings property class (or however you persist)
        private bool? _isFirstLaunch;
        public bool IsFirstLaunch
        {
            get
            {
                if (_isFirstLaunch == null)
                    _isFirstLaunch = true;  // default true for first launch
                return _isFirstLaunch.Value;
            }
            set => _isFirstLaunch = value;
        }

        private bool? _wpfSoftwareRender;
        public bool WPFSoftwareRender
        {
            get
            {
                if (_wpfSoftwareRender == null)
                    _wpfSoftwareRender = false; // default false
                return _wpfSoftwareRender.Value;
            }
            set => _wpfSoftwareRender = value;
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                if (App.Settings.Prop.IsFirstLaunch)
                {
                    App.Settings.Prop.WPFSoftwareRender = true;
                    App.Settings.Prop.IsFirstLaunch = false;
                    App.Settings.Save();  // Make sure this actually writes your settings file
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("OnSourceInitialized", $"Settings update failed: {ex}");
            }

            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                {
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
                }
            }
        }
    }
}

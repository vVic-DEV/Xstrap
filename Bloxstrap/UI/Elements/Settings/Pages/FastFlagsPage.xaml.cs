using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Windows.Threading;

using Bloxstrap.UI.ViewModels.Settings;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for FastFlagsPage.xaml
    /// </summary>
    public partial class FastFlagsPage : UiPage
    {

        private CancellationTokenSource? _searchDebounceCts;

        private bool _initialLoad = false;
        private FastFlagsViewModel _viewModel;
        private List<FrameworkElement> _optionControls = new();
        private List<CardExpander> _cardExpanders;

        private List<FrameworkElement> _recommendedOptions = new();
        private List<FrameworkElement> _experimentalOptions = new();
        private List<FrameworkElement> _debugOptions = new();



        public FastFlagsPage()
        {
            InitializeComponent();
            _viewModel = new FastFlagsViewModel();
            _cardExpanders = new List<CardExpander>();
            SetupViewModel();
            Loaded += FastFlagsPage_Loaded;
        }


        private void FastFlagsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _cardExpanders = new List<CardExpander>();
            foreach (var expander in FindVisualChildren<CardExpander>(this))
                _cardExpanders.Add(expander);

            _optionControls = new List<FrameworkElement>();
            foreach (var option in FindVisualChildren<FrameworkElement>(this))
            {
                if (option.GetType().Name == "OptionControl")
                    _optionControls.Add(option);
            }


            _cardExpanders = new List<CardExpander>
            {
                SystemExpander,
                RenderingExpander,
                BasicExpander,
                UserInterfaceExpander,
               RobloxMenuExpander,
                PrivacyExpander,
                SystemExperimentalExpander,
                RenderingAdvancedExpander,
                NetworkExpander,
                MiscExpander
            };

            _recommendedOptions = new List<FrameworkElement>
            {
                PreferredGPU,
                ForceLogicalProcessors,
                ForceAsyncThreads,
                AvoidTaskSchedulerSleep,
                RefreshRate,
                DisableShadows,
                DisablePostFX,
                DisableTerrainTextures,
                RemoveGrass,
                GraySky,
                DisableClouds,
                GrayAvatars,
                MSAA,
                MinimalRendering,
                FPSLimit,
                LightingTechnology,
                FixDisplayScaling,
                RenderingMode,
                TextureQuality,
                PixelResolution,
                VRToggle,
                FeedBackButton,
                LanguageSelector,
                FrameRate,
                ChatTranslation,
                FullscreenTitlebar,
                NoDisconnectMessage,
                DisableAds,
                HidePurchaseUI,
                HideGUI,
                FontPaddingOption,
                RobloxTelemetry,
                WebView2Telemetry,
                VoiceChatTelemetry,
                BlockTencent,
            };

            _experimentalOptions = new List<FrameworkElement>
            {
                BypassVulkan,
                MemoryProbing,
                LightCulling,
                BGRA,
                NewFrameRateSystem,
                LowEndHardwareOptimization,
                WhiteSky,
                LowQualityParticles,
                LowPolyMeshes,
                FrameCreationBufferPercentage,
                StartingGraphicsLevel,
                OverrideGraphicQualityLevel,
                TextureSkipping,
                NetworkOutputStabilization,
                BetterPacketSending,
                NoPayloadLimit,
                RCore,
                LargeReplicator,
                FasterLoadding,
                BufferArray,
                MTU,
                UnthemedInstances,
                RedFont,
                Pseudolocalization,
                DisableLayeredClothing,
                MoreCharacters,
            };

            _debugOptions = new List<FrameworkElement>
            {
                Chunks,
                PingBreakdownOption,
                FlagStateOption
            };

        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;

            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await Task.Delay(300, token);
                    if (!token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() => PerformSearchBoxFiltering());
                    }
                }
                catch (TaskCanceledException)
                {
                    // Ignore cancellation
                }
            });
        }

        private void PerformSearchBoxFiltering()
        {
            string search = SearchBox.Text.Trim().ToLowerInvariant();
            bool isSearching = !string.IsNullOrWhiteSpace(search);

            // This will handle ManagerEnabled and Reset automatically
            foreach (var option in _optionControls)
            {
                string? header = option.GetType().GetProperty("Header")?.GetValue(option)?.ToString();
                if (string.IsNullOrEmpty(header))
                    header = option.Name;

                option.Visibility = !isSearching || (header?.ToLowerInvariant().Contains(search) ?? false)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (isSearching)
            {
                if (ManagerEnabled != null)
                    ManagerEnabled.Visibility = Visibility.Collapsed;
                if (Reset != null)
                    Reset.Visibility = Visibility.Collapsed;
            }

            RecommendedTextBlock.Visibility = _recommendedOptions.Exists(opt => opt.Visibility == Visibility.Visible)
                ? Visibility.Visible : Visibility.Collapsed;
            ExperimentalTextBlock.Visibility = _experimentalOptions.Exists(opt => opt.Visibility == Visibility.Visible)
                ? Visibility.Visible : Visibility.Collapsed;
            DebugTextBlock.Visibility = _debugOptions.Exists(opt => opt.Visibility == Visibility.Visible)
                ? Visibility.Visible : Visibility.Collapsed;

            foreach (var expander in _cardExpanders)
            {
                var expanderOptions = new List<FrameworkElement>();
                foreach (var child in FindVisualChildren<FrameworkElement>(expander))
                {
                    if (_optionControls.Contains(child))
                        expanderOptions.Add(child);
                }

                bool anyVisible = expanderOptions.Exists(opt => opt.Visibility == Visibility.Visible);

                expander.Visibility = (!isSearching || anyVisible) ? Visibility.Visible : Visibility.Collapsed;

                if (isSearching && anyVisible)
                    expander.IsExpanded = true;
                else if (!isSearching)
                    expander.IsExpanded = false;
            }
        }


        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                    yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        private void SetupViewModel()
        {
            _viewModel = new FastFlagsViewModel();

            _viewModel.OpenFlagEditorEvent += OpenFlagEditor;
            _viewModel.RequestPageReloadEvent += (_, _) => SetupViewModel();

            DataContext = _viewModel;
        }

        private void OpenFlagEditor(object? sender, EventArgs e)
        {
            if (Window.GetWindow(this) is INavigationWindow window)
            {
                window.Navigate(typeof(FastFlagEditorPage));
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // refresh datacontext on page load to synchronize with editor page

            if (!_initialLoad)
            {
                _initialLoad = true;
                return;
            }

            SetupViewModel();
        }

        private void ValidateInt32(object sender, TextCompositionEventArgs e)
        {
            // Allow "-" only at the start, and digits
            if (e.Text == "-")
                e.Handled = ((sender as System.Windows.Controls.TextBox)?.CaretIndex != 0);
            else
                e.Handled = !int.TryParse(e.Text, out _);
        }

        private void ValidateUInt32(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !uint.TryParse(e.Text, out _);
        }
    }
}

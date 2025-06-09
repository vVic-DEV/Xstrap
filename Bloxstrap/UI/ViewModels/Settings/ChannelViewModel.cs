using System.ComponentModel;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.ViewModels.Settings
{

    public class ChannelViewModel : NotifyPropertyChangedViewModel
    {
        public ChannelViewModel()
        {
            Task.Run(() => LoadChannelDeployInfo(App.Settings.Prop.Channel));

            // Initialize SelectedPriority from settings
            _selectedPriority = App.Settings.Prop.SelectedProcessPriority;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        private new void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool UpdateCheckingEnabled
        {
            get => App.Settings.Prop.CheckForUpdates;
            set => App.Settings.Prop.CheckForUpdates = value;
        }
        public bool DisableAnimations
        {
            get => App.Settings.Prop.DisableAnimations;
            set => App.Settings.Prop.DisableAnimations = value;
        }

        public bool HardwareAcceleration
        {
            get => App.Settings.Prop.WPFSoftwareRender;
            set => App.Settings.Prop.WPFSoftwareRender = value;
        }


        private ProcessPriorityOption _selectedPriority;

        public IReadOnlyList<ProcessPriorityOption> ProcessPriorityOptions { get; } =
            Enum.GetValues(typeof(ProcessPriorityOption)).Cast<ProcessPriorityOption>().ToList();

        public ProcessPriorityOption SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                if (_selectedPriority != value)
                {
                    _selectedPriority = value;
                    App.Settings.Prop.SelectedProcessPriority = value;
                    OnPropertyChanged(nameof(SelectedPriority));
                }
            }
        }

        private async Task LoadChannelDeployInfo(string channel)
        {
            ShowLoadingError = false;
            OnPropertyChanged(nameof(ShowLoadingError));

            ChannelInfoLoadingText = Strings.Menu_Channel_Switcher_Fetching;
            OnPropertyChanged(nameof(ChannelInfoLoadingText));

            ChannelDeployInfo = null;
            OnPropertyChanged(nameof(ChannelDeployInfo));

            try
            {
                ClientVersion info = await Deployment.GetInfo(channel);

                ShowChannelWarning = info.IsBehindDefaultChannel;
                OnPropertyChanged(nameof(ShowChannelWarning));

                ChannelDeployInfo = new DeployInfo
                {
                    Version = info.Version,
                    VersionGuid = info.VersionGuid
                };

                App.State.Prop.IgnoreOutdatedChannel = true;

                OnPropertyChanged(nameof(ChannelDeployInfo));
            }
            catch (InvalidChannelException ex)
            {
                ShowLoadingError = true;
                OnPropertyChanged(nameof(ShowLoadingError));

                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    ChannelInfoLoadingText = Strings.Menu_Channel_Switcher_Unauthorized;
                else
                    ChannelInfoLoadingText = $"An http error has occured ({ex.StatusCode})";

                OnPropertyChanged(nameof(ChannelInfoLoadingText));
            }
        }

        public bool IsRobloxInstallationMissing => string.IsNullOrEmpty(App.RobloxState.Prop.Player.VersionGuid) &&
                                                    string.IsNullOrEmpty(App.RobloxState.Prop.Studio.VersionGuid);

        public bool ShowLoadingError { get; set; } = false;
        public bool ShowChannelWarning { get; set; } = false;

        public DeployInfo? ChannelDeployInfo { get; private set; } = null;
        public string ChannelInfoLoadingText { get; private set; } = null!;

        public string ViewChannel
        {
            get => App.Settings.Prop.Channel;
            set
            {
                value = value.Trim();
                Task.Run(() => LoadChannelDeployInfo(value));

                App.Settings.Prop.Channel = value;
            }
        }

        public string ChannelHash
        {
            get => App.Settings.Prop.ChannelHash;
            set
            {
                const string VersionHashFormat = "version-(.*)";
                Match match = Regex.Match(value, VersionHashFormat);
                if (match.Success || string.IsNullOrEmpty(value))
                {
                    App.Settings.Prop.ChannelHash = value;
                }
            }
        }

        public bool UpdateRoblox
        {
            get => App.Settings.Prop.UpdateRoblox;
            set => App.Settings.Prop.UpdateRoblox = value;
        }

        public IReadOnlyDictionary<string, ChannelChangeMode> ChannelChangeModes => new Dictionary<string, ChannelChangeMode>
        {
            { Strings.Menu_Channel_ChangeAction_Automatic, ChannelChangeMode.Automatic },
            { Strings.Menu_Channel_ChangeAction_Prompt, ChannelChangeMode.Prompt },
            { Strings.Menu_Channel_ChangeAction_Ignore, ChannelChangeMode.Ignore },
        };

        public string SelectedChannelChangeMode
        {
            get => ChannelChangeModes.FirstOrDefault(x => x.Value == App.Settings.Prop.ChannelChangeMode).Key;
            set => App.Settings.Prop.ChannelChangeMode = ChannelChangeModes[value];
        }

        public bool ForceRobloxReinstallation
        {
            get => App.State.Prop.ForceReinstall || IsRobloxInstallationMissing;
            set => App.State.Prop.ForceReinstall = value;
        }
    }
}
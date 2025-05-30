using System.ComponentModel;

public class FastFlagEditorSettingViewModel : INotifyPropertyChanged
{
    public static event EventHandler? ShowPresetColumnChanged;

    private CopyFormatMode _selectedCopyFormat = CopyFormatMode.Format1;

    public CopyFormatMode SelectedCopyFormat
    {
        get => _selectedCopyFormat;
        set
        {
            if (_selectedCopyFormat != value)
            {
                _selectedCopyFormat = value;
                OnPropertyChanged(nameof(SelectedCopyFormat));
            }
        }
    }

    public IEnumerable<CopyFormatMode> CopyFormatModes => Enum.GetValues(typeof(CopyFormatMode)).Cast<CopyFormatMode>();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void SaveSettings()
    {
        var settings = new Dictionary<string, string>
    {
        { "SelectedCopyFormat", SelectedCopyFormat.ToString() },
        { "ShowPresetColumn", _showPresetColumnSetting.ToString() }
    };

        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void LoadSettings()
    {
        if (!File.Exists(SettingsPath))
            return;

        var json = File.ReadAllText(SettingsPath);
        var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        if (settings != null)
        {
            if (settings.TryGetValue("SelectedCopyFormat", out var copyFormatString))
            {
                if (Enum.TryParse(copyFormatString, out CopyFormatMode format))
                    SelectedCopyFormat = format;
            }

            if (settings.TryGetValue("ShowPresetColumn", out var showPresetString))
            {
                if (bool.TryParse(showPresetString, out var showPreset))
                    ShowPresetColumnSetting = showPreset;  // This updates the property & UI
            }
        }
    }


    private static string SettingsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fastflag-editor-settings.json");

    private bool _showPresetColumnSetting;
    public bool ShowPresetColumnSetting
    {
        get => _showPresetColumnSetting;
        set
        {
            if (_showPresetColumnSetting != value)
            {
                _showPresetColumnSetting = value;
                OnPropertyChanged(nameof(ShowPresetColumnSetting));
                SaveSettings(); // persist the change
                ShowPresetColumnChanged?.Invoke(this, EventArgs.Empty); // notify UI
            }
        }
    }

}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;

using Wpf.Ui.Mvvm.Contracts;
using Bloxstrap.UI.Elements.Dialogs;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for FastFlagEditorPage.xaml
    /// </summary>
    public partial class FastFlagEditorPage
    {
        private readonly ObservableCollection<FastFlag> _fastFlagList = new();

        private bool _showPresets = true;
        private string _searchFilter = string.Empty;
        private string _lastSearch = string.Empty;
        private DateTime _lastSearchTime = DateTime.MinValue;
        private const int _debounceDelay = 70;

        private bool LoadShowPresetColumnSetting()
        {
            return App.Settings.Prop.ShowPresetColumn;
        }




        public FastFlagEditorPage()
        {
            InitializeComponent();

            AdvancedSettingViewModel.ShowPresetColumnChanged += (_, _) =>
            {
                Dispatcher.Invoke(() =>
                {
                    PresetColumn.Visibility = LoadShowPresetColumnSetting()
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                });
            };

            AdvancedSettingViewModel.ShowFlagCountChanged += (_, _) =>
            {
                Dispatcher.Invoke(UpdateTotalFlagsCount);
            };

            SetDefaultStates();
        }


        private void SetDefaultStates()
        {
            TogglePresetsButton.IsChecked = true;
        }

        private void ReloadList()
        {
            PresetColumn.Visibility = LoadShowPresetColumnSetting() ? Visibility.Visible : Visibility.Collapsed;

            _fastFlagList.Clear();

            var presetFlags = FastFlagManager.PresetFlags.Values;

            foreach (var pair in App.FastFlags.Prop.OrderBy(x => x.Key))
            {
                if (!_showPresets && presetFlags.Contains(pair.Key))
                    continue;

                if (!pair.Key.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var entry = new FastFlag
                {
                    Name = pair.Key,
                    Value = pair.Value?.ToString() ?? string.Empty,
                    Preset = presetFlags.Contains(pair.Key)
                        ? "pack://application:,,,/Resources/Checkmark.ico"
                        : "pack://application:,,,/Resources/CrossMark.ico"
                };

                _fastFlagList.Add(entry);
            }

            if (DataGrid.ItemsSource is null)
                DataGrid.ItemsSource = _fastFlagList;

            UpdateTotalFlagsCount();
        }

        public string FlagCountText => $"Total flags: {_fastFlagList.Count}";

        private void UpdateTotalFlagsCount()
        {
            // Get current flags count from the DataGrid's ItemsSource, safely
            int count = 0;
            if (DataGrid.ItemsSource is IEnumerable<FastFlag> flags)
                count = flags.Count();

            // Update the TextBlock text with the count
            TotalFlagsTextBlock.Text = $"Total Flags: {count}";

            // Toggle visibility based on the user setting
            TotalFlagsTextBlock.Visibility = App.Settings.Prop.ShowFlagCount
                ? Visibility.Visible
                : Visibility.Collapsed;
        }



        private void ClearSearch(bool refresh = true)
        {
            SearchTextBox.Text = "";
            _searchFilter = "";

            if (refresh)
                ReloadList();
        }

        private void ShowAddDialog()
        {
            var dialog = new AddFastFlagDialog();
            dialog.ShowDialog();

            if (dialog.Result != MessageBoxResult.OK)
                return;

            if (dialog.Tabs.SelectedIndex == 0)
                AddSingle(dialog.FlagNameTextBox.Text.Trim(), dialog.FlagValueTextBox.Text);
            else if (dialog.Tabs.SelectedIndex == 1)
                ImportJSON(dialog.JsonTextBox.Text);
            else if (dialog.Tabs.SelectedIndex == 2)
                AddWithGameId(
                    dialog.GameFlagNameTextBox.Text.Trim(),
                    dialog.GameFlagValueTextBox.Text,
                    dialog.GameFlagIdTextBox.Text,
                    dialog.AddIdFilterType
                );
            else if (dialog.Tabs.SelectedIndex == 3)
                ImportGameIdJson(
                    dialog.ImportGameIdJson,
                    dialog.ImportGameId,
                    dialog.ImportIdFilterType
                );
        }

        private void AdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AdvancedSettingsDialog();
            dialog.Owner = Window.GetWindow(this);
            dialog.ShowDialog();
        }
        private void ImportGameIdJson(string? json, string? gameId, FastFlagFilterType filterType)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(gameId))
                return;

            Dictionary<string, object>? list = null;

            json = json.Trim();

            if (!json.StartsWith('{'))
                json = '{' + json;

            if (!json.EndsWith('}'))
            {
                int lastIndex = json.LastIndexOf('}');

                if (lastIndex == -1)
                    json += '}';
                else
                    json = json.Substring(0, lastIndex + 1);
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                list = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

                if (list is null)
                    throw new Exception("JSON deserialization returned null");
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Invalid JSON: {ex.Message}",
                    MessageBoxImage.Error
                );
                ShowAddDialog();
                return;
            }

            string suffix = filterType == FastFlagFilterType.DataCenterFilter ? "_DataCenterFilter" : "_PlaceFilter";

            foreach (var pair in list)
            {
                string newName = $"{pair.Key}{suffix}";
                string newValue = $"{pair.Value};{gameId}";
                AddSingle(newName, newValue);
            }

            ClearSearch();
        }

        public async Task CheckAndRemoveInvalidFlagsAsync()
        {
            var urlsJson = new[]
            {
                "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCDesktopClient.json",
                "https://raw.githubusercontent.com/DynamicFastFlag/DynamicFastFlag/refs/heads/main/FvaribleV2.json",
                "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCClientBootstrapper.json",
                "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCStudioApp.json"
            };

            const string rawTextUrl = "https://raw.githubusercontent.com/MaximumADHD/Roblox-Client-Tracker/refs/heads/roblox/FVariables.txt";

            // will add more fflags to whitelist/blacklist system in the future
            var manualWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FLogTencentAuthPath",
                "DFFlagSendRenderFidelityTelemetry",
                "DFFlagReportRenderDistanceTelemetry",
            };

            var manualBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DFFlagFrameTimeStdDev",
                "FIntGameJoinLoadTime",
                "FFlagEnableCloseButtonOnClientToastNotifications2"
            };

            try
            {
                using HttpClient client = new();

                var validFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var url in urlsJson)
                {
                    string jsonText = await client.GetStringAsync(url);
                    using var jsonDoc = JsonDocument.Parse(jsonText);
                    var jsonFlags = jsonDoc.RootElement.EnumerateObject()
                        .Select(prop => prop.Name.Trim());

                    validFlags.UnionWith(jsonFlags);
                }

                string rawText = await client.GetStringAsync(rawTextUrl);
                var rawFlags = rawText
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line =>
                        line.StartsWith("[C++] ", StringComparison.Ordinal) ||
                        line.StartsWith("[Lua] ", StringComparison.Ordinal) ||
                        line.StartsWith("[Com] ", StringComparison.Ordinal))
                    .Select(line => line.Substring(line.IndexOf(']') + 1).Trim())
                    .Where(name => !string.IsNullOrWhiteSpace(name));

                validFlags.UnionWith(rawFlags);
                validFlags.UnionWith(manualWhitelist);

                var allFlags = App.FastFlags.GetAllFlags();

                var toRemove = allFlags
                    .Where(flag =>
                    {
                        var name = flag.Name.Trim();
                        if (manualWhitelist.Contains(name))
                            return false;
                        if (manualBlacklist.Contains(name))
                            return true;
                        return !validFlags.Contains(name);
                    })
                    .ToList();

                if (toRemove.Count == 0)
                {
                    Frontend.ShowMessageBox("All your FastFlags are valid.", MessageBoxImage.Information);
                    return;
                }

                var result = Frontend.ShowMessageBox(
                    $"Found {toRemove.Count} invalid FastFlags. Remove them?",
                    MessageBoxImage.Warning,
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.Cancel)
                    return;

                if (result == MessageBoxResult.OK)
                {
                    foreach (var flag in toRemove)
                        App.FastFlags.SetValue(flag.Name, null);

                    Frontend.ShowMessageBox($"Removed {toRemove.Count} invalid FastFlags.", MessageBoxImage.Information);

                    var showInvalid = Frontend.ShowMessageBox(
                        "Do you want to see the list of invalid FastFlags?",
                        MessageBoxImage.Question,
                        MessageBoxButton.YesNo);

                    if (showInvalid == MessageBoxResult.Yes)
                    {
                        var invalidFlagsWindow = new InvalidFlagsWindow(toRemove.Select(f => f.Name))
                        {
                            Owner = Application.Current.MainWindow
                        };
                        invalidFlagsWindow.ShowDialog();
                    }

                    ReloadList();
                    UpdateTotalFlagsCount();
                }
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Error checking FastFlags: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private async void CheckInvalidFlagsButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckAndRemoveInvalidFlagsAsync();
        }

        private void AddWithGameId(string name, string value, string gameId, FastFlagFilterType filterType)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(gameId))
            {
                Frontend.ShowMessageBox("Please fill in all fields.", MessageBoxImage.Warning);
                return;
            }

            string suffix = filterType == FastFlagFilterType.DataCenterFilter ? "_DataCenterFilter" : "_PlaceFilter";
            string formattedName = $"{name}{suffix}";
            string formattedValue = $"{value};{gameId}";
            FastFlag? entry;

            if (App.FastFlags.GetValue(formattedName) is null)
            {
                entry = new FastFlag
                {
                    Name = formattedName,
                    Value = formattedValue
                };

                if (!formattedName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    ClearSearch();

                App.FastFlags.SetValue(entry.Name, entry.Value);
                _fastFlagList.Add(entry);
            }
            else
            {
                Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_AlreadyExists, MessageBoxImage.Information);

                bool refresh = false;

                if (!_showPresets && FastFlagManager.PresetFlags.Values.Contains(formattedName))
                {
                    TogglePresetsButton.IsChecked = true;
                    _showPresets = true;
                    refresh = true;
                }

                if (!formattedName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    ClearSearch(false);
                    refresh = true;
                }

                if (refresh)
                    ReloadList();

                entry = _fastFlagList.FirstOrDefault(x => x.Name == formattedName);
            }

            DataGrid.SelectedItem = entry;
            DataGrid.ScrollIntoView(entry);
            UpdateTotalFlagsCount();
        }



        private void ShowProfilesDialog()
        {
            var dialog = new FlagProfilesDialog();
            dialog.ShowDialog();

            if (dialog.Result != MessageBoxResult.OK)
                return;

            if (dialog.Tabs.SelectedIndex == 0)
                App.FastFlags.SaveProfile(dialog.SaveProfile.Text);
            else if (dialog.Tabs.SelectedIndex == 1)
            {
                if (dialog.LoadProfile.SelectedValue == null)
                    return;
                App.FastFlags.LoadProfile(dialog.LoadProfile.SelectedValue.ToString(), dialog.ClearFlags.IsChecked);
            }

            Thread.Sleep(1000);
            ReloadList();
        }

        private void AddSingle(string name, string value)
        {
            FastFlag? entry;

            if (App.FastFlags.GetValue(name) is null)
            {
                entry = new FastFlag
                {
                    Name = name,
                    Value = value
                };

                if (!name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    ClearSearch();

                App.FastFlags.SetValue(entry.Name, entry.Value);
                _fastFlagList.Add(entry);
            }
            else
            {
                Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_AlreadyExists, MessageBoxImage.Information);

                bool refresh = false;

                if (!_showPresets && FastFlagManager.PresetFlags.Values.Contains(name))
                {
                    TogglePresetsButton.IsChecked = true;
                    _showPresets = true;
                    refresh = true;
                }

                if (!name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    ClearSearch(false);
                    refresh = true;
                }

                if (refresh)
                    ReloadList();

                entry = _fastFlagList.FirstOrDefault(x => x.Name == name);
            }

            DataGrid.SelectedItem = entry;
            DataGrid.ScrollIntoView(entry);
            UpdateTotalFlagsCount();
        }

        private void ImportJSON(string json)
        {
            Dictionary<string, object>? list = null;

            json = json.Trim();

            // autocorrect where possible
            if (!json.StartsWith('{'))
                json = '{' + json;

            if (!json.EndsWith('}'))
            {
                int lastIndex = json.LastIndexOf('}');

                if (lastIndex == -1)
                    json += '}';
                else
                    json = json.Substring(0, lastIndex + 1);
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                list = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

                if (list is null)
                    throw new Exception("JSON deserialization returned null");
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    string.Format(Strings.Menu_FastFlagEditor_InvalidJSON, ex.Message),
                    MessageBoxImage.Error
                );

                ShowAddDialog();

                return;
            }

            var conflictingFlags = App.FastFlags.Prop.Where(x => list.ContainsKey(x.Key)).Select(x => x.Key);
            bool overwriteConflicting = false;

            if (conflictingFlags.Any())
            {
                int count = conflictingFlags.Count();

                string message = string.Format(
                    Strings.Menu_FastFlagEditor_ConflictingImport,
                    count,
                    string.Join(", ", conflictingFlags.Take(25))
                );

                if (count > 25)
                    message += "...";

                var result = Frontend.ShowMessageBox(message, MessageBoxImage.Question, MessageBoxButton.YesNo);

                overwriteConflicting = result == MessageBoxResult.Yes;
            }

            foreach (var pair in list)
            {
                if (App.FastFlags.Prop.ContainsKey(pair.Key) && !overwriteConflicting)
                    continue;

                if (pair.Value is null)
                    continue;

                var val = pair.Value.ToString();

                if (val is null)
                    continue;

                App.FastFlags.SetValue(pair.Key, val);
            }

            ClearSearch();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => ReloadList();

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.DataContext is not FastFlag entry)
                return;

            if (e.EditingElement is not TextBox textbox)
                return;

            switch (e.Column.Header)
            {
                case "Name":
                    string oldName = entry.Name;
                    string newName = textbox.Text;

                    if (newName == oldName)
                        return;

                    if (App.FastFlags.GetValue(newName) is not null)
                    {
                        Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_AlreadyExists, MessageBoxImage.Information);
                        e.Cancel = true;
                        textbox.Text = oldName;
                        return;
                    }

                    App.FastFlags.SetValue(oldName, null);
                    App.FastFlags.SetValue(newName, entry.Value);

                    if (!newName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                        ClearSearch();

                    entry.Name = newName;

                    break;

                case "Value":
                    string newValue = textbox.Text;
                    App.FastFlags.SetValue(entry.Name, newValue);
                    break;
            }

            UpdateTotalFlagsCount();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is INavigationWindow window)
                window.Navigate(typeof(FastFlagsPage));
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) => ShowAddDialog();

        private void FlagProfiles_Click(object sender, RoutedEventArgs e) => ShowProfilesDialog();

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var tempList = new List<FastFlag>();

            foreach (FastFlag entry in DataGrid.SelectedItems)
                tempList.Add(entry);

            foreach (FastFlag entry in tempList)
            {
                _fastFlagList.Remove(entry);
                App.FastFlags.SetValue(entry.Name, null);
            }

            UpdateTotalFlagsCount();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button)
                return;

            DataGrid.Columns[0].Visibility = button.IsChecked ?? false ? Visibility.Visible : Visibility.Collapsed;

            _showPresets = button.IsChecked ?? true;
            ReloadList();
        }

        private void ExportJSONButton_Click(object sender, RoutedEventArgs e)
        {
            var flags = App.FastFlags.Prop;

            var groupedFlags = flags
                .GroupBy(kvp =>
                {
                    var match = Regex.Match(kvp.Key, @"^[A-Z]+[a-z]*");
                    return match.Success ? match.Value : "Other";
                })
                .OrderBy(g => g.Key);

            var formattedJson = new StringBuilder();
            formattedJson.AppendLine("{");

            int totalItems = flags.Count;
            int writtenItems = 0;
            int groupIndex = 0;

            foreach (var group in groupedFlags)
            {
                if (groupIndex > 0)
                    formattedJson.AppendLine();

                var sortedGroup = group
                    .OrderByDescending(kvp => kvp.Key.Length + (kvp.Value?.ToString()?.Length ?? 0));

                foreach (var kvp in sortedGroup)
                {
                    writtenItems++;
                    bool isLast = (writtenItems == totalItems);
                    string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                    if (!isLast)
                        line += ",";

                    formattedJson.AppendLine(line);
                }

                groupIndex++;
            }

            formattedJson.AppendLine("}");

            SaveJSONToFile(formattedJson.ToString());
        }

        private void CopyJSONButton_Click(object sender, RoutedEventArgs e)
        {
            CopyFormatMode format = App.Settings.Prop.SelectedCopyFormat;

            if (format == CopyFormatMode.Format1)
            {
                string json = JsonSerializer.Serialize(App.FastFlags.Prop, new JsonSerializerOptions { WriteIndented = true });
                Clipboard.SetDataObject(json);
            }
            else if (format == CopyFormatMode.Format2)
            {
                var flags = App.FastFlags.Prop;

                var groupedFlags = flags
                    .GroupBy(kvp =>
                    {
                        var match = Regex.Match(kvp.Key, @"^[A-Z]+[a-z]*");
                        return match.Success ? match.Value : "Other";
                    })
                    .OrderBy(g => g.Key);

                var formattedJson = new StringBuilder();
                formattedJson.AppendLine("{");

                int totalItems = flags.Count;
                int writtenItems = 0;
                int groupIndex = 0;

                foreach (var group in groupedFlags)
                {
                    if (groupIndex > 0)
                        formattedJson.AppendLine();

                    var sortedGroup = group
                        .OrderByDescending(kvp => kvp.Key.Length + (kvp.Value?.ToString()?.Length ?? 0));

                    foreach (var kvp in sortedGroup)
                    {
                        writtenItems++;
                        bool isLast = (writtenItems == totalItems);
                        string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                        if (!isLast)
                            line += ",";

                        formattedJson.AppendLine(line);
                    }

                    groupIndex++;
                }

                formattedJson.AppendLine("}");
                Clipboard.SetText(formattedJson.ToString());
            }
            else if (format == CopyFormatMode.Format3)
            {
                var flags = App.FastFlags.Prop;

                // Sort all flags alphabetically by key
                var sortedFlags = flags.OrderBy(kvp => kvp.Key);

                var formattedJson = new StringBuilder();
                formattedJson.AppendLine("{");

                int totalItems = flags.Count;
                int writtenItems = 0;

                foreach (var kvp in sortedFlags)
                {
                    writtenItems++;
                    bool isLast = (writtenItems == totalItems);
                    string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                    if (!isLast)
                        line += ",";

                    formattedJson.AppendLine(line);
                }

                formattedJson.AppendLine("}");
                Clipboard.SetText(formattedJson.ToString());
            }
            else if (format == CopyFormatMode.Format4) 
            {
                var flags = App.FastFlags.Prop;

                var sortedFlags = flags.OrderByDescending(kvp =>
                    $"    \"{kvp.Key}\": \"{kvp.Value}\"".Length
                );

                var formattedJson = new StringBuilder();
                formattedJson.AppendLine("{");

                int totalItems = flags.Count;
                int writtenItems = 0;

                foreach (var kvp in sortedFlags)
                {
                    writtenItems++;
                    bool isLast = (writtenItems == totalItems);
                    string line = $"    \"{kvp.Key}\": \"{kvp.Value}\"";

                    if (!isLast)
                        line += ",";

                    formattedJson.AppendLine(line);
                }

                formattedJson.AppendLine("}");
                Clipboard.SetText(formattedJson.ToString());

            }
        }



        private void SaveJSONToFile(string json)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt",
                Title = "Save JSON or TXT File",
                FileName = "FroststrapExport.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {

                    var filePath = saveFileDialog.FileName;
                    if (string.IsNullOrEmpty(Path.GetExtension(filePath)))
                    {
                        filePath += ".json";
                    }

                    File.WriteAllText(filePath, json);
                    Frontend.ShowMessageBox("JSON file saved successfully!", MessageBoxImage.Information);
                }
                catch (IOException ioEx)
                {
                    Frontend.ShowMessageBox($"Error saving file: {ioEx.Message}", MessageBoxImage.Error);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    Frontend.ShowMessageBox($"Permission error: {uaEx.Message}", MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    Frontend.ShowMessageBox($"Unexpected error: {ex.Message}", MessageBoxImage.Error);
                }
            }
        }

        private void ShowDeleteAllFlagsConfirmation()
        {
            // Show a confirmation message box to the user
            if (Frontend.ShowMessageBox(
                "Are you sure you want to delete all flags?",
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return; // Exit if the user cancels the action
            }

            // Exit if there are no flags to delete
            if (!HasFlagsToDelete())
            {
                ShowInfoMessage("There are no flags to delete.");
                return;
            }

            try
            {
                DeleteAllFlags();
                ReloadUI();
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private bool HasFlagsToDelete()
        {
            return _fastFlagList.Any() || App.FastFlags.Prop.Any();
        }

        private void DeleteAllFlags()
        {

            _fastFlagList.Clear();


            foreach (var key in App.FastFlags.Prop.Keys.ToList())
            {
                App.FastFlags.SetValue(key, null);
            }
        }

        private void ReloadUI()
        {
            ReloadList();
        }

        private void ShowInfoMessage(string message)
        {
            Frontend.ShowMessageBox(message, MessageBoxImage.Information, MessageBoxButton.OK);
        }

        private void HandleError(Exception ex)
        {
            // Display and log the error message
            Frontend.ShowMessageBox($"An error occurred while deleting flags:\n{ex.Message}", MessageBoxImage.Error, MessageBoxButton.OK);
            LogError(ex); // Logging error in a centralized method
        }

        private void LogError(Exception ex)
        {
            // Detailed logging for developers
            Console.WriteLine(ex.ToString());
        }


        private void DeleteAllButton_Click(object sender, RoutedEventArgs e) => ShowDeleteAllFlagsConfirmation();

        private CancellationTokenSource? _searchCancellationTokenSource;

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textbox) return;

            string newSearch = textbox.Text.Trim();

            if (newSearch == _lastSearch && (DateTime.Now - _lastSearchTime).TotalMilliseconds < _debounceDelay)
                return;

            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            _searchFilter = newSearch;
            _lastSearch = newSearch;
            _lastSearchTime = DateTime.Now;

            try
            {
                await Task.Delay(_debounceDelay, _searchCancellationTokenSource.Token);

                if (_searchCancellationTokenSource.Token.IsCancellationRequested)
                    return;

                Dispatcher.Invoke(() =>
                {
                    ReloadList();
                    ShowSearchSuggestion(newSearch);
                });
            }
            catch (TaskCanceledException)
            {
            }
        }


        private void ShowSearchSuggestion(string searchFilter)
        {
            if (string.IsNullOrWhiteSpace(searchFilter))
            {
                AnimateSuggestionVisibility(0);
                return;
            }

            var bestMatch = App.FastFlags.Prop.Keys
                .Where(flag => flag.Contains(searchFilter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(flag => !flag.StartsWith(searchFilter, StringComparison.OrdinalIgnoreCase))
                .ThenBy(flag => flag.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase))
                .ThenBy(flag => flag.Length)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(bestMatch))
            {
                SuggestionKeywordRun.Text = bestMatch;
                AnimateSuggestionVisibility(1);
            }
            else
            {
                AnimateSuggestionVisibility(0);
            }
        }

        private void SuggestionTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var suggestion = SuggestionKeywordRun.Text;
            if (!string.IsNullOrEmpty(suggestion))
            {
                SearchTextBox.Text = suggestion;
                SearchTextBox.CaretIndex = suggestion.Length;
            }
        }

        private void AnimateSuggestionVisibility(double targetOpacity)
        {
            var opacityAnimation = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromMilliseconds(120),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            var translateAnimation = new DoubleAnimation
            {
                To = targetOpacity > 0 ? 0 : 10,
                Duration = TimeSpan.FromMilliseconds(120),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            opacityAnimation.Completed += (s, e) =>
            {
                if (targetOpacity == 0)
                {
                    SuggestionTextBlock.Visibility = Visibility.Collapsed;
                }
            };

            if (targetOpacity > 0)
                SuggestionTextBlock.Visibility = Visibility.Visible;

            SuggestionTextBlock.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
            SuggestionTranslateTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
        }
    }
}
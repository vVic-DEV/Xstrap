using Microsoft.Win32;
using System.Windows;
using Bloxstrap.Resources;

namespace Bloxstrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Interaction logic for AddFastFlagDialog.xaml
    /// </summary>
    public partial class AddFastFlagDialog
    {
        public string? FormattedName { get; private set; }
        public string? FormattedValue { get; private set; }
        public string? ImportGameId { get; private set; }
        public string? ImportGameIdJson { get; private set; }

        public FastFlagFilterType AddIdFilterType =>
    (AddIdFilterTypeComboBox.SelectedIndex == 1) ? FastFlagFilterType.DataCenterFilter : FastFlagFilterType.PlaceFilter;

        public FastFlagFilterType ImportIdFilterType =>
            (ImportIdFilterTypeComboBox.SelectedIndex == 1) ? FastFlagFilterType.DataCenterFilter : FastFlagFilterType.PlaceFilter;



        public MessageBoxResult Result = MessageBoxResult.Cancel;

        public AddFastFlagDialog()
        {
            InitializeComponent();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.FileTypes_JSONFiles}|*.json"
            };

            if (dialog.ShowDialog() != true)
                return;

            JsonTextBox.Text = File.ReadAllText(dialog.FileName);
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = Tabs.SelectedIndex;

            if (selectedIndex == 2)
            {
                string name = GameFlagNameTextBox.Text.Trim();
                string value = GameFlagValueTextBox.Text.Trim();
                string gameId = GameFlagIdTextBox.Text.Trim();
                var filterType = AddIdFilterType;
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(gameId))
                {
                    string suffix = filterType == FastFlagFilterType.DataCenterFilter ? "_DataCenterFilter" : "_PlaceFilter";
                    FormattedName = $"{name}{suffix}";
                    FormattedValue = $"{value};{gameId}";
                    ImportGameId = null;
                    ImportGameIdJson = null;
                    Result = MessageBoxResult.OK;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Please fill in all fields.");
                }
                return;
            }
            else if (selectedIndex == 3)
            {
                var filterType = ImportIdFilterType;
                ImportGameId = ImportGameIdTextBox.Text.Trim();
                ImportGameIdJson = GameIdJsonTextBox.Text.Trim();
                FormattedName = null;
                FormattedValue = null;
                Result = MessageBoxResult.OK;
                DialogResult = true;
                Close();
                return;
            }

            FormattedName = null;
            FormattedValue = null;
            ImportGameId = null;
            ImportGameIdJson = null;
            Result = MessageBoxResult.OK;
            DialogResult = true;
            Close();
        }

    }
}

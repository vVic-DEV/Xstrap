using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class InvalidFlagsWindow : Base.WpfUiWindow
    {
        public InvalidFlagsWindow(IEnumerable<string> invalidFlags)
        {
            InitializeComponent();

            var sb = new StringBuilder();
            foreach (var flag in invalidFlags)
            {
                sb.AppendLine(flag);
            }
            InvalidFlagsTextBox.Text = sb.ToString();

            InvalidFlagsTextBox.Focus();
            InvalidFlagsTextBox.Select(0, 0);

            CloseButton.Click += (_, __) => Close();
        }
    }
}

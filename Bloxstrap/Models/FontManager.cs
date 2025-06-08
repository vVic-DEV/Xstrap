using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Bloxstrap.Models
{
    public static class FontManager
    {
        public static bool IsCustomFontApplied { get; private set; }

        public static System.Windows.Media.FontFamily? LoadFontFromFile(string fontFilePath)
        {
            if (!File.Exists(fontFilePath))
                return null;

            string tempFontsFolder = Path.Combine(Path.GetTempPath(), "BloxstrapFonts");
            Directory.CreateDirectory(tempFontsFolder);

            string destFontPath = Path.Combine(tempFontsFolder, Path.GetFileName(fontFilePath));
            File.Copy(fontFilePath, destFontPath, overwrite: true);

            var fontDirectoryUri = new Uri(Path.GetDirectoryName(destFontPath) + Path.DirectorySeparatorChar);
            var fontFamilies = Fonts.GetFontFamilies(fontDirectoryUri);

            return fontFamilies.FirstOrDefault();
        }

        public static bool ApplySavedCustomFont()
        {
            string? savedFontPath = App.Settings.Prop.CustomFontPath;

            if (!string.IsNullOrWhiteSpace(savedFontPath) && File.Exists(savedFontPath))
            {
                try
                {
                    System.Windows.Media.FontFamily? font = LoadFontFromFile(savedFontPath);
                    if (font != null)
                    {
                        ApplyFontGlobally(font);
                        IsCustomFontApplied = true;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine("FontManager", $"Failed to load saved font: {ex}");
                }
            }

            return false;
        }

        public static void ApplyFontGlobally(System.Windows.Media.FontFamily fontFamily)
        {
            Application.Current.Resources[SystemFonts.MessageFontFamilyKey] = fontFamily;

            foreach (Window window in Application.Current.Windows)
                window.FontFamily = fontFamily;
        }

        public static void RemoveCustomFont()
        {
            var defaultFont = new System.Windows.Media.FontFamily("Segoe UI");
            ApplyFontGlobally(defaultFont);
            IsCustomFontApplied = false;
            App.Settings.Prop.CustomFontPath = null;
            App.Settings.Save();
        }
    }
}

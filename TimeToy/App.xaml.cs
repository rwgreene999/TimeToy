using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TimeToy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            // Load default theme (you can change default here or read from config)
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary { Source = new Uri("ThemeDark.xaml", UriKind.Relative) });
        }

        public void SetTheme(string themeName)
        {
            string themeFile = $"Theme{themeName}.xaml";
            var dict = new ResourceDictionary { Source = new Uri(themeFile, UriKind.Relative) };

            // Remove any merged dictionaries that are theme files (Theme*.xaml)
            var existingThemeDicts = Application.Current.Resources.MergedDictionaries
                .Where(d => d.Source != null && d.Source.OriginalString.IndexOf("Theme", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            foreach (var d in existingThemeDicts)
            {
                Application.Current.Resources.MergedDictionaries.Remove(d);
            }

            // Add the requested theme dictionary
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}

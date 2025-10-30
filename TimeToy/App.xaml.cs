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
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(
                new ResourceDictionary { Source = new Uri("ThemeDark.xaml", UriKind.Relative) });

        }

        public void SetTheme(string themeName)
        {
            string themeFile = themeName == "Dark" ? "ThemeDark.xaml" : "ThemeLight.xaml";
            var dict = new ResourceDictionary { Source = new Uri(themeFile, UriKind.Relative) };

            // Remove existing theme dictionaries
            var existingDictionaries = Application.Current.Resources.MergedDictionaries
                .Where(d => d.Source != null && (d.Source.OriginalString.Contains("ThemeDark.xaml") || d.Source.OriginalString.Contains("ThemeLight.xaml")))
                .ToList();

            foreach (var d in existingDictionaries)
                Application.Current.Resources.MergedDictionaries.Remove(d);

            // Add the new theme
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Xml;
using Newtonsoft.Json; // Install-Package Newtonsoft.Json
using TimeToy;

namespace TimeToy
{
    // configuration for complete application
    public static class RunConfigManager
    {
        private static string ConfigFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TimeToy.json");

        public static RunConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    return JsonConvert.DeserializeObject<RunConfig>(json) ?? new RunConfig();
                }
                else
                {
                    var config = new RunConfig();
                    Save(config);
                    return config;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WIP Error loading configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new RunConfig();
            }
        }

        public static void Save(RunConfig config)
        {
            var json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}



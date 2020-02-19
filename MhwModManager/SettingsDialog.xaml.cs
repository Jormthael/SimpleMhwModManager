﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using WinForms = System.Windows.Forms;
using System;
using System.Windows.Media;
using System.Linq;

namespace MhwModManager
{
    public class Setting
    {
        public Settings settings = new Settings();

        public static string FindMHWInstallFolder()
        {
            /* Thanks to WildGoat07 : https://github.com/WildGoat07 */
            // Read the registery to get the steam path
            var steamRoot = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", null) as string;
            var libFolders = new List<string>();
            libFolders.Add(steamRoot);
            App.logStream.WriteLine("Registry key found");
            // Read the steam library
            dynamic libraryfolders = Gameloop.Vdf.VdfConvert.Deserialize(File.ReadAllText(Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf")));
            try
            {
                for (int i = 1; ; i++)
                    libFolders.Add(libraryfolders.Value[i.ToString()].ToString());
            }
            catch (Exception e) { App.logStream.WriteLine(e.Message); }
            const string appid = "582010"; // The appid of Monster Hunter World
            foreach (var folder in libFolders)
                foreach (var file in new DirectoryInfo(Path.Combine(folder, "steamapps")).GetFiles())
                    if (file.Extension == ".acf")
                        using (var text = file.OpenText())
                        {
                            dynamic content = Gameloop.Vdf.VdfConvert.Deserialize(text.ReadToEnd());
                            if (content.Value.appid.ToString() == appid)
                            {
                                App.logStream.WriteLine("MHW found");
                                return Path.Combine(file.DirectoryName, "common", content.Value.installdir.ToString()); // The path of Monster Hunter World
                            }
                        }
            return null;
        }

        public void GenConfig()
        {
            if (!File.Exists(App.SettingsPath))
            {
                App.logStream.WriteLine("Settings not found");
                if (!Directory.Exists(App.AppData))
                {
                    App.logStream.WriteLine("AppData created");
                    Directory.CreateDirectory(App.AppData);
                }

                settings.dark_mode = false;
                settings.mhw_path = FindMHWInstallFolder();
                ParseSettingsJSON();
            }
            else
            {
                App.logStream.WriteLine("Settings found");
                Setting sets;
                using (StreamReader file = new StreamReader(App.SettingsPath))
                {
                    sets = JsonConvert.DeserializeObject<Setting>(file.ReadToEnd());
                    file.Close();
                }

                settings.dark_mode = sets.settings.dark_mode;
                if (!Directory.Exists(sets.settings.mhw_path))
                    sets.settings.mhw_path = FindMHWInstallFolder();
                settings.mhw_path = sets.settings.mhw_path;
                App.logStream.WriteLine("Settings loaded");
            }
        }

        public void ParseSettingsJSON()
        {
            App.logStream.WriteLine("Settings updated");
            using (StreamWriter file = new StreamWriter(App.SettingsPath))
            {
                file.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                file.Close();
            }
        }

        public struct Settings
        {
            public bool dark_mode;
            public string mhw_path;
        }
    }

    /// <summary>
    /// Logique d'interaction pour SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window
    {
        public SettingsDialog()
        {
            InitializeComponent();
            InitializeSettings();
            MakeDarkTheme();
            versionLbl.Content = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void browseBTN_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            dialog.SelectedPath = App.Settings.settings.mhw_path;
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                App.Settings.settings.mhw_path = dialog.SelectedPath;
        }

        private void cancelBTN_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.GenConfig();
            App.ReloadTheme();
            Close();
        }

        private void darkmodeCB_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.settings.dark_mode = darkmodeCB.IsChecked.Value;
            if (App.Settings.settings.dark_mode)
                darkmodeCB.Content = "Enabled";
            else
                darkmodeCB.Content = "Disabled";

            App.ReloadTheme();
            MakeDarkTheme();
        }

        private void InitializeSettings()
        {
            pathTB.Text = App.Settings.settings.mhw_path;
            darkmodeCB.IsChecked = App.Settings.settings.dark_mode;
        }

        private void MakeDarkTheme()
        {
            var converter = new BrushConverter();
            if (App.Settings.settings.dark_mode)
            {
                Background = (Brush)converter.ConvertFromString("#FF171717");
                (browseBTN.Content as System.Windows.Controls.Border).BorderBrush = (Brush)converter.ConvertFromString("#FFFFFFFF");
            }
            else
            {
                Background = (Brush)converter.ConvertFromString("#FFFFFFFF");
                (browseBTN.Content as System.Windows.Controls.Border).BorderBrush = (Brush)converter.ConvertFromString("#FF171717");
            }
        }

        private void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Warning ! Clicking Yes will delete every mod informations stored for the software. It will NOT remove installed mods. Are you sure you want to continue ?", "Reset data", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Yes)
            {
                Directory.Delete(App.ModsPath, true);
                Close();
                Application.Current.MainWindow.Close();
                System.Diagnostics.Process.Start(Environment.GetCommandLineArgs().First());
                Environment.Exit(0);
            }
        }

        private void validateBTN_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.ParseSettingsJSON();
            App.ReloadTheme();
            Close();
        }
    }
}
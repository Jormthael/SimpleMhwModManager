﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using WinForms = System.Windows.Forms;
using Newtonsoft.Json;

namespace MhwModManager
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string AppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SMMM");
        public static string ModsPath = Path.Combine(AppData, "mods");
        public static Setting Settings = new Setting();
        public static string SettingsPath = Path.Combine(AppData, "settings.json");
        public static List<(ModInfo, string)> Mods;

        public App()
        {
            Settings.GenConfig();
            if (!Directory.Exists(Settings.settings.mhw_path))
            {
                MessageBox.Show("The path to MHW is not found, you have to install the game first, or if the game is already installed, open it", "Simple MHW Mod Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                var dialog = new WinForms.FolderBrowserDialog();
                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    Settings.settings.mhw_path = dialog.SelectedPath;
                    Settings.ParseSettingsJSON();
                }
                else
                    Environment.Exit(0);
            }
        }

        public static void GetMods()
        {
            // This list contain the ModInfos and the folder name of each mod
            Mods = new List<(ModInfo, string)>();

            if (!Directory.Exists(ModsPath))
                Directory.CreateDirectory(ModsPath);

            var modFolder = new DirectoryInfo(ModsPath);

            int i = 0;
            foreach (var mod in modFolder.GetDirectories())
            {
                var info = new ModInfo();
                info.GenInfo(mod.FullName);
                // If the order change the generation of the list
                if (info.order >= Mods.Count)
                    Mods.Add((info, mod.Name));
                else
                {
                    if (i > 0)
                        if (info.order == Mods[i - 1].Item1.order || info.order == Mods[i + 1].Item1.order)
                        {
                            info.order++;
                            info.ParseSettingsJSON(mod.FullName);
                        }
                    Mods.Insert(info.order, (info, mod.Name));
                }
                i++;
            }
        }

        public async static void Updater()
        {
            /* Credits to WildGoat07 : https://github.com/WildGoat07 */
            var github = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("SimpleMhwModManager"));
            var lastRelease = await github.Repository.Release.GetLatest(234864718);
            var current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (new Version(lastRelease.TagName) > current)
            {
                var result = MessageBox.Show("A new version is available, do you want to download it now ?", "SMMM", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start("https://github.com/oxypomme/SimpleMhwModManager/releases/latest");
            }
        }
    }

    public class ModInfo
    {
        public bool activated { get; set; }
        public int order { get; set; }
        public string name { get; set; }

        public void GenInfo(string path, int? index = null)
        {
            if (!File.Exists(Path.Combine(path, "mod.info")))
            {
                activated = false;
                if (index != null)
                    order = index.Value;
                else
                    order = App.Mods.Count();

                // Get the name of the extracted folder (without the .zip at the end), not the full path
                var foldName = path.Split('\\');
                name = foldName[foldName.GetLength(0) - 1].Split('.')[0];

                ParseSettingsJSON(path);
            }
            else
            {
                ModInfo sets;
                using (StreamReader file = new StreamReader(Path.Combine(path, "mod.info")))
                {
                    sets = JsonConvert.DeserializeObject<ModInfo>(file.ReadToEnd());
                    file.Close();
                }

                activated = sets.activated;
                order = sets.order;
                name = sets.name;
            }
        }

        public void ParseSettingsJSON(string path)
        {
            using (StreamWriter file = new StreamWriter(Path.Combine(path, "mod.info")))
            {
                file.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                file.Close();
            }
        }
    }
}
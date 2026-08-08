using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace ChromeUpdaterWPF
{
    public partial class MainWindow
    {
        private void EnsureOptimizerConfig()
        {
            if (File.Exists(configFilePath)) return;

            string json = @"{
  ""_Description"": ""Chrome 便携版智能升级器 - 核心优化驱动规则库 (引擎外置化)"",
  ""_Notice"": ""修改此文件后，系统会自动读取并接管底层注入。0=关闭，1=开启"",

  ""Policies"": {
    ""_组1"": ""--- 性能榨取与系统资源控制 ---"",
    ""BackgroundModeEnabled"": 0,
    ""HighEfficiencyModeEnabled"": 1,
    ""BatterySaverModeState"": 1,
    ""SpellcheckEnabled"": 0,

    ""_组2"": ""--- AI 大模型与臃肿服务封杀 ---"",
    ""GenAiDefaultSettings"": 1,
    ""GenAILocalFoundationalModelSettings"": 1,
    ""OptimizationGuideModelDownloading"": 0,
    ""HelpMeWriteSettings"": 2,
    ""TabOrganizerSettings"": 2,

    ""_组3"": ""--- 遥测、隐私与网络安全 ---"",
    ""ComponentUpdatesEnabled"": 0,
    ""NetworkPredictionOptions"": 2,
    ""HttpsOnlyMode"": ""disallowed"",

    ""_组4"": ""--- UI 纯净与臃肿功能剥离 ---"",
    ""PromotionalTabsEnabled"": 0,
    ""ShoppingListEnabled"": 0,
    ""ShowAppsShortcutInBookmarkBar"": 0
  },

  ""Flags"": [
    ""optimization-guide-on-device-model@2"",
    ""prompt-api-for-gemini-nano@2"",
    ""ai-mode-omnibox-entrypoint@2"",
    ""glic@2"",
    ""summarization-api-for-gemini-nano@2"",
    ""compose@2"",
    ""enable-parallel-downloading@1"",
    ""hardware-media-key-handling@2"",
    ""smooth-scrolling@1"",
    ""enable-gpu-rasterization@1"",
    ""lens-overlay-optimization-filter@2"",
    ""omnibox-contextual-suggestions@2""
  ],

  ""Preferences"": {
    ""session|restore_on_startup"": 1,
    ""performance_tuning|high_efficiency_mode"": {""state"":1,""mode"":1},
    ""performance_tuning|battery_saver_mode"": {""state"":1}
  }
}";
            try { File.WriteAllText(configFilePath, json, System.Text.Encoding.UTF8); } catch { }
        }

        private int GetPolicyValueInt(string json, string key, int defaultVal)
        {
            Match m = Regex.Match(json, $@"""{key}""\s*:\s*(\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out int val) ? val : defaultVal;
        }

        private string GetPolicyValueString(string json, string key, string defaultVal)
        {
            Match m = Regex.Match(json, $@"""{key}""\s*:\s*""([^""]*)""");
            return m.Success ? m.Groups[1].Value : defaultVal;
        }

        private string SetPolicyValueInt(string json, string key, int value)
        {
            if (Regex.IsMatch(json, $@"""{key}""\s*:\s*\d+")) return Regex.Replace(json, $@"""{key}""\s*:\s*\d+", $@"""{key}"": {value}");
            Match pMatch = Regex.Match(json, @"""Policies""\s*:\s*\{");
            return pMatch.Success ? json.Insert(pMatch.Index + pMatch.Length, $"\n    \"{key}\": {value},") : json;
        }

        private string SetPolicyValueString(string json, string key, string value)
        {
            if (Regex.IsMatch(json, $@"""{key}""\s*:\s*""[^""]*""")) return Regex.Replace(json, $@"""{key}""\s*:\s*""[^""]*""", $@"""{key}"": ""{value}""");
            Match pMatch = Regex.Match(json, @"""Policies""\s*:\s*\{");
            return pMatch.Success ? json.Insert(pMatch.Index + pMatch.Length, $"\n    \"{key}\": \"{value}\",") : json;
        }

        private void ApplyEnterprisePolicies(string configJson)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Policies\Google\Chrome"))
                {
                    if (key == null) return;
                    key.SetValue("UserDataDir", GetNormalizedPath(userDataDir), Microsoft.Win32.RegistryValueKind.String);
                    key.SetValue("DefaultBrowserSettingEnabled", 0, Microsoft.Win32.RegistryValueKind.DWord);

                    Match policyBlock = Regex.Match(configJson, @"""Policies""\s*:\s*\{([^}]*)\}");
                    if (policyBlock.Success)
                    {
                        foreach (Match m in Regex.Matches(policyBlock.Groups[1].Value, @"""([A-Za-z0-9_]+)""\s*:\s*(\d+)"))
                            if (!m.Groups[1].Value.StartsWith("_")) key.SetValue(m.Groups[1].Value, int.Parse(m.Groups[2].Value), Microsoft.Win32.RegistryValueKind.DWord);

                        foreach (Match m in Regex.Matches(policyBlock.Groups[1].Value, @"""([A-Za-z0-9_]+)""\s*:\s*""([^""]+)"""))
                        {
                            if (!m.Groups[1].Value.StartsWith("_"))
                            {
                                if (m.Groups[2].Value == "default") try { key.DeleteValue(m.Groups[1].Value); } catch { }
                                else key.SetValue(m.Groups[1].Value, m.Groups[2].Value, Microsoft.Win32.RegistryValueKind.String);
                            }
                        }
                    }

                    if (rdoDisableCacheYes.IsChecked == true) key.SetValue("MetricsReportingEnabled", 0, Microsoft.Win32.RegistryValueKind.DWord);
                    else try { key.DeleteValue("MetricsReportingEnabled"); } catch { }

                    if (rdoLimitYes.IsChecked == true && double.TryParse(txtCacheLimit.Text, out double gb) && gb > 0)
                    {
                        long bytes = (long)(gb * 1024 * 1024 * 1024); if (bytes > int.MaxValue) bytes = int.MaxValue;
                        key.SetValue("DiskCacheSize", (int)bytes, Microsoft.Win32.RegistryValueKind.DWord);
                        long mediaBytes = bytes / 5; if (mediaBytes > 500 * 1024 * 1024) mediaBytes = 500 * 1024 * 1024;
                        key.SetValue("MediaCacheSize", (int)mediaBytes, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    else { try { key.DeleteValue("DiskCacheSize"); } catch { } try { key.DeleteValue("MediaCacheSize"); } catch { } }
                }
            }
            catch { }
        }

        private void ApplyChromeTuningConfig()
        {
            EnsureOptimizerConfig();
            string configJson = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}";
            ApplyEnterprisePolicies(configJson);

            var targetUserDirs = new List<string> { userDataDir };
            try { string localPath = GetLocalUserDataDir(); if (!string.IsNullOrEmpty(localPath)) targetUserDirs.Add(localPath); } catch { }

            foreach (var uDir in targetUserDirs)
            {
                try
                {
                    string defaultDir = Path.Combine(uDir, "Default");
                    if (!Directory.Exists(defaultDir)) Directory.CreateDirectory(defaultDir);

                    string localStatePath = Path.Combine(uDir, "Local State");
                    string localStateContent = File.Exists(localStatePath) ? File.ReadAllText(localStatePath) : "{}";
                    File.WriteAllText(localStatePath, InjectLabsExperiments(localStateContent, configJson));

                    string prefPath = Path.Combine(defaultDir, "Preferences");
                    string prefContent = File.Exists(prefPath) ? File.ReadAllText(prefPath) : "{}";
                    File.WriteAllText(prefPath, InjectPreferences(prefContent, configJson));

                    LockAiDirectoryIfClean(uDir);
                    LockDisposableCachesIfEnabled(uDir);
                }
                catch { }
            }
        }

        private string InjectLabsExperiments(string localStateJson, string configJson)
        {
            var targetFlags = new List<string>();
            Match flagsBlock = Regex.Match(configJson, @"""Flags""\s*:\s*\[([^\]]*)\]");
            if (flagsBlock.Success) foreach (Match m in Regex.Matches(flagsBlock.Groups[1].Value, @"""([^""]+)""")) targetFlags.Add(m.Groups[1].Value);

            if (string.IsNullOrWhiteSpace(localStateJson) || localStateJson.Trim() == "{}") return "{\"browser\":{\"enabled_labs_experiments\":[" + string.Join(",", targetFlags.ConvertAll(f => $"\"{f}\"")) + "]}}";

            if (!localStateJson.Contains("\"browser\""))
            {
                int index = localStateJson.IndexOf('{');
                if (index >= 0) localStateJson = localStateJson.Insert(index + 1, "\"browser\":{},");
            }
            Match browserMatch = Regex.Match(localStateJson, @"""browser""\s*:\s*\{");
            if (browserMatch.Success && !localStateJson.Contains("\"enabled_labs_experiments\"")) localStateJson = localStateJson.Insert(browserMatch.Index + browserMatch.Length, "\"enabled_labs_experiments\":[],");

            Match arrayMatch = Regex.Match(localStateJson, @"""enabled_labs_experiments""\s*:\s*\[([^\]]*)\]");
            if (arrayMatch.Success)
            {
                var currentFlags = new List<string>();
                foreach (Match m in Regex.Matches(arrayMatch.Groups[1].Value, @"""([^""]+)""")) currentFlags.Add(m.Groups[1].Value);
                foreach (var flag in targetFlags) { currentFlags.RemoveAll(f => f.StartsWith(flag.Split('@')[0] + "@")); currentFlags.Add(flag); }
                localStateJson = Regex.Replace(localStateJson, @"""enabled_labs_experiments""\s*:\s*\[([^\]]*)\]", $"\"enabled_labs_experiments\":[{string.Join(",", currentFlags.ConvertAll(f => $"\"{f}\""))}]");
            }
            return localStateJson;
        }

        private string InjectPreferences(string prefJson, string configJson)
        {
            if (string.IsNullOrWhiteSpace(prefJson) || prefJson.Trim() == "{}") prefJson = "{}";
            Match prefsBlock = Regex.Match(configJson, @"""Preferences""\s*:\s*\{([^}]*)\}");
            if (prefsBlock.Success)
            {
                foreach (Match m in Regex.Matches(prefsBlock.Groups[1].Value, @"""([^""]+)""\s*:\s*(\{[^\}]+\}|""[^""]+""|[\w]+)"))
                {
                    string keyPair = m.Groups[1].Value; string val = m.Groups[2].Value;
                    if (keyPair.StartsWith("_") || !keyPair.Contains("|")) continue;
                    prefJson = InjectJsonKeyValue(prefJson, keyPair.Split('|')[0], keyPair.Split('|')[1], val);
                }
            }
            return prefJson;
        }

        private string InjectJsonKeyValue(string json, string parentKey, string childKey, string value)
        {
            if (!json.Contains($"\"{parentKey}\"")) { int index = json.IndexOf('{'); if (index >= 0) json = json.Insert(index + 1, $"\"{parentKey}\":{{}},"); }
            Match parentMatch = Regex.Match(json, @"""" + parentKey + @"""\s*:\s*\{");
            if (parentMatch.Success)
            {
                Match childMatch = Regex.Match(json, @"""" + childKey + @"""\s*:\s*([^,{}]+|\{[^{}]*\})");
                if (childMatch.Success)
                {
                    string updatedSub = Regex.Replace(json.Substring(parentMatch.Index), @"""" + childKey + @"""\s*:\s*([^,{}]+|\{[^{}]*\})", $"\"{childKey}\":{value}", RegexOptions.None);
                    json = json.Substring(0, parentMatch.Index) + updatedSub;
                }
                else json = json.Insert(parentMatch.Index + parentMatch.Length, $"\"{childKey}\":{value},");
            }
            return json.Replace(",,", ",").Replace(",}", "}").Replace("{,", "{");
        }

        private void LockAiDirectoryIfClean(string uDir)
        {
            try { string aiPath = Path.Combine(uDir, "OptGuideOnDeviceModel"); if (!Directory.Exists(aiPath) && !File.Exists(aiPath)) { File.WriteAllText(aiPath, "LOCKED"); File.SetAttributes(aiPath, FileAttributes.ReadOnly | FileAttributes.Hidden); } } catch { }
        }

        private void LockDisposableCachesIfEnabled(string uDir)
        {
            string[] disposableFolders = { "GPUCache", "ShaderCache", "GrShaderCache", "GraphiteDawnCache", "Crashpad" };
            foreach (var folder in disposableFolders)
            {
                try { string targetPath = Path.Combine(uDir, folder); if (rdoDisableCacheYes.IsChecked == true) { if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true); if (!File.Exists(targetPath)) { File.WriteAllText(targetPath, "LOCKED"); File.SetAttributes(targetPath, FileAttributes.ReadOnly | FileAttributes.Hidden); } } else { if (File.Exists(targetPath)) { File.SetAttributes(targetPath, FileAttributes.Normal); File.Delete(targetPath); } } } catch { }
            }
        }

        private (long freedBytes, int cleanedCount) PerformSilentCacheClean()
        {
            var targetUserDirs = new List<string> { userDataDir };
            try { string localPath = GetLocalUserDataDir(); if (!string.IsNullOrEmpty(localPath)) targetUserDirs.Add(localPath); } catch { }
            long fBytes = 0; int cCount = 0;
            string[] relativeCachePaths = { @"Default\Cache", @"Default\Code Cache", @"Default\GPUCache", @"Default\Media Cache", @"GPUCache", @"ShaderCache", @"GrShaderCache", @"GraphiteDawnCache", @"Crashpad", @"BrowserMetrics" };
            foreach (var uDir in targetUserDirs)
            {
                foreach (var relPath in relativeCachePaths)
                {
                    try { string fullPath = Path.Combine(uDir, relPath); if (Directory.Exists(fullPath)) { foreach (var f in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories)) { try { fBytes += new FileInfo(f).Length; } catch { } } Directory.Delete(fullPath, true); cCount++; } } catch { }
                }
            }
            return (fBytes, cCount);
        }

        private void LoadAndApplySavedCacheSettings()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Policies\Google\Chrome"))
                {
                    if (key != null)
                    {
                        object metrics = key.GetValue("MetricsReportingEnabled"); if (metrics != null && Convert.ToInt32(metrics) == 0) rdoDisableCacheYes.IsChecked = true;
                        object diskCache = key.GetValue("DiskCacheSize");
                        if (diskCache != null) { long bytes = Convert.ToInt64(diskCache); if (bytes > 0) { rdoLimitYes.IsChecked = true; txtCacheLimit.IsEnabled = true; double gb = Math.Round((double)bytes / (1024 * 1024 * 1024), 1); txtCacheLimit.Text = (gb <= 0 ? 1 : gb).ToString(); } }
                    }
                }
                if (File.Exists(Path.Combine(userDataDir, "GPUCache"))) rdoDisableCacheYes.IsChecked = true;
            }
            catch { }
        }

        public void BtnCleanCache_Click(object sender, RoutedEventArgs e)
        {
            var (freedBytes, cleanedCount) = PerformSilentCacheClean();
            ApplyChromeTuningConfig();
            double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 2); string freedText = freedMb > 1024 ? $"{Math.Round(freedMb / 1024, 2)} GB" : $"{freedMb} MB";
            MessageBox.Show($"【缓存清理完成】\n\n成功清空了 {cleanedCount} 个无用缓存目录！\n共为您释放磁盘空间: {freedText}\n\n已保留您的全部密码、Cookies、网页历史与书签！", "清理成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool IsAiModelPresent(out List<string> foundPaths)
        {
            foundPaths = new List<string>();
            string portableAiPath = Path.Combine(userDataDir, "OptGuideOnDeviceModel");
            if (Directory.Exists(portableAiPath) && Directory.GetFiles(portableAiPath, "*", SearchOption.AllDirectories).Length > 0) foundPaths.Add(portableAiPath);
            try { string localAiPath = Path.Combine(GetLocalUserDataDir(), "OptGuideOnDeviceModel"); if (Directory.Exists(localAiPath) && Directory.GetFiles(localAiPath, "*", SearchOption.AllDirectories).Length > 0) foundPaths.Add(localAiPath); } catch { }
            return foundPaths.Count > 0;
        }

        private void UpdateAiButtonStatus()
        {
            try
            {
                if (!((Directory.Exists(portableDir) && File.Exists(chromeExe)) || Directory.Exists(GetLocalUserDataDir()))) { btnAICheck.Content = "AI 模型体检"; btnAICheck.IsEnabled = false; btnAICheck.Background = new SolidColorBrush(Color.FromRgb(149, 165, 166)); return; }
                if (IsAiModelPresent(out List<string> foundPaths)) { btnAICheck.Content = "存在AI模型"; btnAICheck.IsEnabled = true; btnAICheck.Background = new SolidColorBrush(Color.FromRgb(211, 84, 0)); }
                else { btnAICheck.Content = "纯净无AI模型"; btnAICheck.IsEnabled = true; btnAICheck.Background = new SolidColorBrush(Color.FromRgb(155, 89, 182)); }
            }
            catch { }
        }

        private void BtnAICheck_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAiModelPresent(out List<string> foundPaths)) return;
            if (MessageBox.Show("【AI 模型清理】\n\n检测到存在已静默下载的 AI 模型！\n是否立即物理清理，并通过企业策略彻底锁死未来下载通道？", "确认清理", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ApplyChromeTuningConfig();
                int deletedCount = 0; long freedBytes = 0;
                foreach (var path in foundPaths)
                {
                    try { if (Directory.Exists(path)) { foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) { try { freedBytes += new FileInfo(file).Length; } catch { } } Directory.Delete(path, true); deletedCount++; if (!File.Exists(path)) { File.WriteAllText(path, "LOCKED"); File.SetAttributes(path, FileAttributes.ReadOnly | FileAttributes.Hidden); } } }
                    catch (Exception ex) { MessageBox.Show($"清理失败: {ex.Message}\n请先关闭浏览器！", "错误", MessageBoxButton.OK, MessageBoxImage.Error); UpdateAiButtonStatus(); return; }
                }
                double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 2); string freedText = freedMb > 1024 ? $"{Math.Round(freedMb / 1024, 2)} GB" : $"{freedMb} MB";
                MessageBox.Show($"【清理完成】\n\n清除 {deletedCount} 个 AI 模型目录！\n共释放空间: {freedText}\n\n已为您锁死 AI 静默下载！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateAiButtonStatus();
            }
        }
    }
}
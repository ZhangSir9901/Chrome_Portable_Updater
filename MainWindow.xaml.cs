using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Collections.Generic;

namespace ChromeUpdaterWPF
{
    public partial class MainWindow : Window
    {
        private readonly string portableDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Chrome_Portable");
        private readonly string chromeExe;
        private readonly string userDataDir;
        private readonly string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "optimizer_config.json");

        private static readonly string APP_VERSION = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion;
        private readonly string poemText = "巴女浅醉黄鹤楼，\n江风吹皱美人绸。\n此别经年何时了，\n云锁巫山夜未犹。\n\n";

        public MainWindow()
        {
            InitializeComponent();
            chromeExe = Path.Combine(portableDir, "Chrome", "chrome.exe");
            userDataDir = Path.Combine(portableDir, "UserData");
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CleanupOldUpdater();
            CheckRunningDirectory();

            try { this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/logo.ico")); } catch { }

            if (Environment.Is64BitOperatingSystem) rdo64.Content = "64位 (自动检测)";
            else { rdo32.IsChecked = true; rdo32.Content = "32位 (自动检测)"; }

            lblAppVersion.Text = $"当前升级器版本: {APP_VERSION}";

            if (Process.GetProcessesByName("chrome").Length == 0)
            {
                PerformSilentCacheClean();
            }

            EnsureOptimizerConfig();
            LoadAndApplySavedCacheSettings();
            _ = CheckSelfUpdateAsync();
            await CheckAndDisplayVersionAsync();
            UpdateAiButtonStatus();
        }

        private string GetNormalizedPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            try { return Path.GetFullPath(path).ToLowerInvariant().TrimEnd('\\', '/'); }
            catch { return path.ToLowerInvariant().TrimEnd('\\', '/'); }
        }

        private string GetExactCanonicalPath(string path)
        {
            return GetNormalizedPath(path);
        }

        // ================== 🌟 配置文件引擎外置核心 ==================
        private void EnsureOptimizerConfig()
        {
            if (File.Exists(configFilePath)) return;

            string json = @"{
  ""_Description"": ""Chrome 便携版智能升级器 - 核心优化驱动规则库 (引擎外置化)"",
  ""_Notice"": ""修改此文件后，系统会自动读取并接管底层注入。0=关闭，1=开启，2=禁用"",

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
    ""_说明"": ""格式为 '父节点|子节点' : 设定值 (支持嵌套JSON字符串)"",
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
            if (m.Success && int.TryParse(m.Groups[1].Value, out int val))
                return val;
            return defaultVal;
        }

        private string GetPolicyValueString(string json, string key, string defaultVal)
        {
            Match m = Regex.Match(json, $@"""{key}""\s*:\s*""([^""]*)""");
            if (m.Success)
                return m.Groups[1].Value;
            return defaultVal;
        }

        private string SetPolicyValueInt(string json, string key, int value)
        {
            if (Regex.IsMatch(json, $@"""{key}""\s*:\s*\d+"))
                return Regex.Replace(json, $@"""{key}""\s*:\s*\d+", $@"""{key}"": {value}");

            Match pMatch = Regex.Match(json, @"""Policies""\s*:\s*\{");
            if (pMatch.Success)
                return json.Insert(pMatch.Index + pMatch.Length, $"\n    \"{key}\": {value},");

            return json;
        }

        private string SetPolicyValueString(string json, string key, string value)
        {
            if (Regex.IsMatch(json, $@"""{key}""\s*:\s*""[^""]*"""))
                return Regex.Replace(json, $@"""{key}""\s*:\s*""[^""]*""", $@"""{key}"": ""{value}""");

            Match pMatch = Regex.Match(json, @"""Policies""\s*:\s*\{");
            if (pMatch.Success)
                return json.Insert(pMatch.Index + pMatch.Length, $"\n    \"{key}\": \"{value}\",");

            return json;
        }

        // 🌟 互斥切换到高级设置界面，并自动读取 JSON 赋值给 UI
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContentPanel.Visibility = Visibility.Collapsed;
            SettingsContentPanel.Visibility = Visibility.Visible;

            string json = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}";

            chkHighEfficiency.IsChecked = GetPolicyValueInt(json, "HighEfficiencyModeEnabled", 1) == 1;
            chkBatterySaver.IsChecked = GetPolicyValueInt(json, "BatterySaverModeState", 1) == 1;
            chkDisableBgMode.IsChecked = GetPolicyValueInt(json, "BackgroundModeEnabled", 0) == 0;
            chkDisableSpellcheck.IsChecked = GetPolicyValueInt(json, "SpellcheckEnabled", 0) == 0;

            chkDisableGenAI.IsChecked = GetPolicyValueInt(json, "GenAiDefaultSettings", 1) == 1;
            chkDisableLocalModel.IsChecked = GetPolicyValueInt(json, "GenAILocalFoundationalModelSettings", 1) == 1;

            chkDisableCompUpdate.IsChecked = GetPolicyValueInt(json, "ComponentUpdatesEnabled", 0) == 0;
            chkDisablePrediction.IsChecked = GetPolicyValueInt(json, "NetworkPredictionOptions", 2) == 2;
            chkDisableHttpsOnly.IsChecked = GetPolicyValueString(json, "HttpsOnlyMode", "disallowed") == "disallowed";

            chkDisablePromotions.IsChecked = GetPolicyValueInt(json, "PromotionalTabsEnabled", 0) == 0;
            chkDisableShopping.IsChecked = GetPolicyValueInt(json, "ShoppingListEnabled", 0) == 0;
        }

        // 🌟 将 UI 开关状态写回 JSON 并立即强制注入底层
        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string json = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}";

            json = SetPolicyValueInt(json, "HighEfficiencyModeEnabled", chkHighEfficiency.IsChecked == true ? 1 : 0);
            json = SetPolicyValueInt(json, "BatterySaverModeState", chkBatterySaver.IsChecked == true ? 1 : 0);
            json = SetPolicyValueInt(json, "BackgroundModeEnabled", chkDisableBgMode.IsChecked == true ? 0 : 1);
            json = SetPolicyValueInt(json, "SpellcheckEnabled", chkDisableSpellcheck.IsChecked == true ? 0 : 1);

            json = SetPolicyValueInt(json, "GenAiDefaultSettings", chkDisableGenAI.IsChecked == true ? 1 : 0);
            json = SetPolicyValueInt(json, "GenAILocalFoundationalModelSettings", chkDisableLocalModel.IsChecked == true ? 1 : 0);

            json = SetPolicyValueInt(json, "ComponentUpdatesEnabled", chkDisableCompUpdate.IsChecked == true ? 0 : 1);
            json = SetPolicyValueInt(json, "NetworkPredictionOptions", chkDisablePrediction.IsChecked == true ? 2 : 0);
            json = SetPolicyValueString(json, "HttpsOnlyMode", chkDisableHttpsOnly.IsChecked == true ? "disallowed" : "default");

            json = SetPolicyValueInt(json, "PromotionalTabsEnabled", chkDisablePromotions.IsChecked == true ? 0 : 1);
            json = SetPolicyValueInt(json, "ShoppingListEnabled", chkDisableShopping.IsChecked == true ? 0 : 1);

            try
            {
                File.WriteAllText(configFilePath, json, System.Text.Encoding.UTF8);
                ApplyChromeTuningConfig();
                MessageBox.Show("极客优化参数已成功保存！并已自动写入系统底层组策略拦截规则。\n重启浏览器后即可完美生效。", "应用成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"保存配置文件失败: {ex.Message}"); }

            SettingsContentPanel.Visibility = Visibility.Collapsed;
            MainContentPanel.Visibility = Visibility.Visible;
        }

        private void BtnCancelSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsContentPanel.Visibility = Visibility.Collapsed;
            MainContentPanel.Visibility = Visibility.Visible;
        }

        // ================== 🌟 缓存清理与自更新 ==================
        private (long freedBytes, int cleanedCount) PerformSilentCacheClean()
        {
            var targetUserDirs = new List<string> { userDataDir };
            try
            {
                string localPath = GetLocalUserDataDir();
                if (!string.IsNullOrEmpty(localPath)) targetUserDirs.Add(localPath);
            }
            catch { }

            long fBytes = 0; int cCount = 0;
            string[] relativeCachePaths = {
                @"Default\Cache", @"Default\Code Cache", @"Default\GPUCache", @"Default\Media Cache",
                @"GPUCache", @"ShaderCache", @"GrShaderCache", @"GraphiteDawnCache", @"Crashpad", @"BrowserMetrics"
            };

            foreach (var uDir in targetUserDirs)
            {
                foreach (var relPath in relativeCachePaths)
                {
                    try
                    {
                        string fullPath = Path.Combine(uDir, relPath);
                        if (Directory.Exists(fullPath))
                        {
                            foreach (var f in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
                            {
                                try { fBytes += new FileInfo(f).Length; } catch { }
                            }
                            Directory.Delete(fullPath, true);
                            cCount++;
                        }
                    }
                    catch { }
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
                        object metrics = key.GetValue("MetricsReportingEnabled");
                        if (metrics != null && Convert.ToInt32(metrics) == 0) rdoDisableCacheYes.IsChecked = true;

                        object diskCache = key.GetValue("DiskCacheSize");
                        if (diskCache != null)
                        {
                            long bytes = Convert.ToInt64(diskCache);
                            if (bytes > 0)
                            {
                                rdoLimitYes.IsChecked = true;
                                txtCacheLimit.IsEnabled = true;
                                double gb = Math.Round((double)bytes / (1024 * 1024 * 1024), 1);
                                if (gb <= 0) gb = 1;
                                txtCacheLimit.Text = gb.ToString();
                            }
                        }
                    }
                }
                string portableGpuPath = Path.Combine(userDataDir, "GPUCache");
                if (File.Exists(portableGpuPath)) rdoDisableCacheYes.IsChecked = true;
            }
            catch { }
        }

        private void CleanupOldUpdater()
        {
            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string oldExe = currentExe + ".old";
                if (File.Exists(oldExe)) File.Delete(oldExe);
            }
            catch { }
        }

        private async Task CheckSelfUpdateAsync()
        {
            try
            {
                string[] jsonUrls = {
                    $"https://ghproxy.net/https://raw.githubusercontent.com/ZhangSir9901/Chrome_Portable_Updater/main/Release/update.json?t={DateTime.Now.Ticks}",
                    $"https://raw.kkgithub.com/ZhangSir9901/Chrome_Portable_Updater/main/Release/update.json?t={DateTime.Now.Ticks}"
                };

                using (var client = CreateSafeWebClient())
                {
                    string json = "";
                    foreach (string url in jsonUrls)
                    {
                        try { json = await client.DownloadStringTaskAsync(url); break; } catch { continue; }
                    }
                    if (string.IsNullOrEmpty(json)) return;

                    Match mVer = Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
                    Match mUrl = Regex.Match(json, @"""url""\s*:\s*""([^""]+)""");

                    if (mVer.Success && mUrl.Success)
                    {
                        string onlineVer = mVer.Groups[1].Value;
                        string downloadUrl = mUrl.Groups[1].Value;

                        if (string.Compare(onlineVer, APP_VERSION, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            lblAppVersion.Visibility = Visibility.Collapsed;
                            btnAppUpdate.Visibility = Visibility.Visible;
                            btnAppUpdate.Content = $"🚀 发现新版本 {onlineVer}，点击立即自动升级！";
                            btnAppUpdate.Tag = downloadUrl;
                        }
                    }
                }
            }
            catch { }
        }

        public async void BtnAppUpdate_Click(object sender, RoutedEventArgs e)
        {
            string downloadUrl = (sender as Button)?.Tag?.ToString();
            if (string.IsNullOrEmpty(downloadUrl)) return;

            OverlayPanel.Visibility = Visibility.Visible;
            lblOverlayStatus.Text = "正在通过极速 CDN 下载新版升级器...";
            progressBar.Value = 0;
            StartBlinking();

            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string newExe = currentExe + ".new";
                string oldExe = currentExe + ".old";
                string noCacheUrl = downloadUrl + (downloadUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks;

                using (var client = CreateSafeWebClient())
                {
                    client.DownloadProgressChanged += (s, ev) => { progressBar.Value = ev.ProgressPercentage; };
                    await client.DownloadFileTaskAsync(new Uri(noCacheUrl), newExe);
                }

                lblOverlayStatus.Text = "下载完成，正在执行无感影子热替换...";
                progressBar.IsIndeterminate = true;
                await Task.Delay(800);

                if (File.Exists(oldExe)) File.Delete(oldExe);
                File.Move(currentExe, oldExe);
                File.Move(newExe, currentExe);

                Process.Start(currentExe);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"自升级失败: {ex.Message}\n请检查网络或所在目录权限。", "热更新错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StopBlinking();
                OverlayPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void CheckRunningDirectory()
        {
            try
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string downloadsPath = Path.Combine(userProfile, "Downloads");

                if (currentDir.StartsWith(systemDrive, StringComparison.OrdinalIgnoreCase) ||
                    currentDir.StartsWith(desktopPath, StringComparison.OrdinalIgnoreCase) ||
                    currentDir.StartsWith(downloadsPath, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("【便携版使用建议】\n\n检测到您当前在系统盘或桌面运行本程序。\n建议您关闭本程序后，将整个文件夹【剪切】到 D盘、E盘 等非系统盘下再运行！", "智能路径提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch { }
        }

        private string GetLocalUserDataDir()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                int channelIndex = cmbChannel.SelectedIndex;
                string folderName = channelIndex == 1 ? "Chrome Beta" : channelIndex == 2 ? "Chrome Dev" : channelIndex == 3 ? "Chrome SxS" : "Chrome";
                return Path.Combine(localAppData, "Google", folderName, "User Data");
            }
            catch { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data"); }
        }

        private bool IsAiModelPresent(out List<string> foundPaths)
        {
            foundPaths = new List<string>();
            string portableAiPath = Path.Combine(userDataDir, "OptGuideOnDeviceModel");
            if (Directory.Exists(portableAiPath) && Directory.GetFiles(portableAiPath, "*", SearchOption.AllDirectories).Length > 0)
                foundPaths.Add(portableAiPath);

            try
            {
                string localAiPath = Path.Combine(GetLocalUserDataDir(), "OptGuideOnDeviceModel");
                if (Directory.Exists(localAiPath) && Directory.GetFiles(localAiPath, "*", SearchOption.AllDirectories).Length > 0)
                    foundPaths.Add(localAiPath);
            }
            catch { }
            return foundPaths.Count > 0;
        }

        private void UpdateAiButtonStatus()
        {
            try
            {
                bool chromeExists = (Directory.Exists(portableDir) && File.Exists(chromeExe)) || Directory.Exists(GetLocalUserDataDir());

                if (!chromeExists)
                {
                    btnAICheck.Content = "AI 模型体检";
                    btnAICheck.IsEnabled = false;
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
                    return;
                }

                if (IsAiModelPresent(out List<string> foundPaths))
                {
                    btnAICheck.Content = "存在AI模型";
                    btnAICheck.IsEnabled = true;
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 84, 0));
                }
                else
                {
                    btnAICheck.Content = "纯净无AI模型";
                    btnAICheck.IsEnabled = true;
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(155, 89, 182));
                }
            }
            catch { }
        }

        // ================== 🌟 极客防线模块：底层逻辑注入 ==================
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
                        // 注入数字型策略
                        foreach (Match m in Regex.Matches(policyBlock.Groups[1].Value, @"""([A-Za-z0-9_]+)""\s*:\s*(\d+)"))
                        {
                            if (!m.Groups[1].Value.StartsWith("_"))
                                key.SetValue(m.Groups[1].Value, int.Parse(m.Groups[2].Value), Microsoft.Win32.RegistryValueKind.DWord);
                        }

                        // 注入字符串型策略 (处理如 HttpsOnlyMode: "disallowed")
                        foreach (Match m in Regex.Matches(policyBlock.Groups[1].Value, @"""([A-Za-z0-9_]+)""\s*:\s*""([^""]+)"""))
                        {
                            if (!m.Groups[1].Value.StartsWith("_"))
                            {
                                if (m.Groups[2].Value == "default")
                                    try { key.DeleteValue(m.Groups[1].Value); } catch { }
                                else
                                    key.SetValue(m.Groups[1].Value, m.Groups[2].Value, Microsoft.Win32.RegistryValueKind.String);
                            }
                        }
                    }

                    if (rdoDisableCacheYes.IsChecked == true) key.SetValue("MetricsReportingEnabled", 0, Microsoft.Win32.RegistryValueKind.DWord);
                    else try { key.DeleteValue("MetricsReportingEnabled"); } catch { }

                    if (rdoLimitYes.IsChecked == true && double.TryParse(txtCacheLimit.Text, out double gb) && gb > 0)
                    {
                        long bytes = (long)(gb * 1024 * 1024 * 1024);
                        if (bytes > int.MaxValue) bytes = int.MaxValue;
                        key.SetValue("DiskCacheSize", (int)bytes, Microsoft.Win32.RegistryValueKind.DWord);

                        long mediaBytes = bytes / 5;
                        if (mediaBytes > 500 * 1024 * 1024) mediaBytes = 500 * 1024 * 1024;
                        key.SetValue("MediaCacheSize", (int)mediaBytes, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                    else
                    {
                        try { key.DeleteValue("DiskCacheSize"); } catch { }
                        try { key.DeleteValue("MediaCacheSize"); } catch { }
                    }
                }
            }
            catch { }
        }

        private void ApplyChromeTuningConfig()
        {
            EnsureOptimizerConfig();
            string configJson = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}";

            ApplyEnterprisePolicies(configJson);

            try
            {
                if (Process.GetProcessesByName("chrome").Length > 0)
                {
                    MessageBox.Show("【参数注入提示】\n\n检测到有 Chrome 进程正在后台运行。\n为了让所有的优化策略规则完美生效，请先关闭全部 Chrome 窗口！", "请关闭 Chrome", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch { }

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
            if (flagsBlock.Success)
            {
                foreach (Match m in Regex.Matches(flagsBlock.Groups[1].Value, @"""([^""]+)""")) targetFlags.Add(m.Groups[1].Value);
            }

            if (string.IsNullOrWhiteSpace(localStateJson) || localStateJson.Trim() == "{}")
                return "{\"browser\":{\"enabled_labs_experiments\":[" + string.Join(",", targetFlags.ConvertAll(f => $"\"{f}\"")) + "]}}";

            if (!localStateJson.Contains("\"browser\""))
            {
                int index = localStateJson.IndexOf('{');
                if (index >= 0) localStateJson = localStateJson.Insert(index + 1, "\"browser\":{},");
            }
            Match browserMatch = Regex.Match(localStateJson, @"""browser""\s*:\s*\{");
            if (browserMatch.Success && !localStateJson.Contains("\"enabled_labs_experiments\""))
                localStateJson = localStateJson.Insert(browserMatch.Index + browserMatch.Length, "\"enabled_labs_experiments\":[],");

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
                    string keyPair = m.Groups[1].Value;
                    string val = m.Groups[2].Value;
                    if (keyPair.StartsWith("_") || !keyPair.Contains("|")) continue;

                    string parent = keyPair.Split('|')[0];
                    string child = keyPair.Split('|')[1];
                    prefJson = InjectJsonKeyValue(prefJson, parent, child, val);
                }
            }
            return prefJson;
        }

        private string InjectJsonKeyValue(string json, string parentKey, string childKey, string value)
        {
            if (!json.Contains($"\"{parentKey}\""))
            {
                int index = json.IndexOf('{');
                if (index >= 0) json = json.Insert(index + 1, $"\"{parentKey}\":{{}},");
            }
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
            try
            {
                string aiPath = Path.Combine(uDir, "OptGuideOnDeviceModel");
                if (!Directory.Exists(aiPath) && !File.Exists(aiPath))
                {
                    File.WriteAllText(aiPath, "LOCKED_BY_CHROME_PORTABLE_UPDATER");
                    File.SetAttributes(aiPath, FileAttributes.ReadOnly | FileAttributes.Hidden);
                }
            }
            catch { }
        }

        private void LockDisposableCachesIfEnabled(string uDir)
        {
            string[] disposableFolders = { "GPUCache", "ShaderCache", "GrShaderCache", "GraphiteDawnCache", "Crashpad" };
            foreach (var folder in disposableFolders)
            {
                try
                {
                    string targetPath = Path.Combine(uDir, folder);
                    if (rdoDisableCacheYes.IsChecked == true)
                    {
                        if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
                        if (!File.Exists(targetPath))
                        {
                            File.WriteAllText(targetPath, "LOCKED_BY_CHROME_PORTABLE_UPDATER");
                            File.SetAttributes(targetPath, FileAttributes.ReadOnly | FileAttributes.Hidden);
                        }
                    }
                    else
                    {
                        if (File.Exists(targetPath))
                        {
                            File.SetAttributes(targetPath, FileAttributes.Normal);
                            File.Delete(targetPath);
                        }
                    }
                }
                catch { }
            }
        }

        // ================== 🌟 UI 与交互事件 ==================
        public void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { this.DragMove(); }
        public void BtnMinimize_Click(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        public void BtnClose_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        public void Copyright_Click(object sender, MouseButtonEventArgs e)
        {
            bool chromeExists = Directory.Exists(portableDir) && File.Exists(chromeExe);
            bool isDefault = IsPortableChromeDefault();
            if (chromeExists && isDefault) OpenUrl("https://t.me/YuC2027");
        }

        public void Github_Click(object sender, RoutedEventArgs e) { OpenUrl("https://github.com/ZhangSir9901/Chrome_Portable_Updater"); }

        private void WeChat_Click(object sender, RoutedEventArgs e)
        {
            try { Clipboard.SetText("jiujiujiayi666"); MessageBox.Show("微信号已成功复制！\n您可以去微信中粘贴添加好友。", "技术支持", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show($"复制失败: {ex.Message}", "技术支持"); }
        }

        private void Telegram_Click(object sender, RoutedEventArgs e)
        {
            bool chromeExists = Directory.Exists(portableDir) && File.Exists(chromeExe);
            bool isDefault = IsPortableChromeDefault();
            if (chromeExists && isDefault) OpenUrl("https://t.me/YuC2027");
        }

        private void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { try { Process.Start("cmd", $"/c start {url.Replace("&", "^&")}"); } catch { } }
        }

        public void RdoDisableCache_Click(object sender, RoutedEventArgs e)
        {
            ApplyChromeTuningConfig();
            UpdateBorderStyles(Directory.Exists(portableDir) && File.Exists(chromeExe), IsPortableChromeDefault(), ShortcutsExistOnDesktop());
            if (rdoDisableCacheYes.IsChecked == true)
            {
                lblStatus.Text = "已成功开启无用缓存拦截！";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
            }
            else
            {
                lblStatus.Text = "已解除无用缓存拦截。";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 73, 94));
            }
        }

        public void RdoLimit_Click(object sender, RoutedEventArgs e)
        {
            txtCacheLimit.IsEnabled = (rdoLimitYes.IsChecked == true);
            ApplyChromeTuningConfig();
            UpdateBorderStyles(Directory.Exists(portableDir) && File.Exists(chromeExe), IsPortableChromeDefault(), ShortcutsExistOnDesktop());
            if (rdoLimitYes.IsChecked == true)
            {
                lblStatus.Text = $"已限制磁盘缓存上限为 {txtCacheLimit.Text} GB！";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
            }
            else
            {
                lblStatus.Text = "已取消缓存容量限制。";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 73, 94));
            }
        }

        public void TxtCacheLimit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"[\d\.]");
        }

        public void TxtCacheLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplyChromeTuningConfig();
                if (rdoLimitYes.IsChecked == true && double.TryParse(txtCacheLimit.Text, out double gb) && gb > 0)
                {
                    lblStatus.Text = $"缓存上限已更新限制为 {gb} GB！";
                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                }
            }
        }

        public void BtnCleanCache_Click(object sender, RoutedEventArgs e)
        {
            var (freedBytes, cleanedCount) = PerformSilentCacheClean();
            ApplyChromeTuningConfig();
            double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 2);
            string freedText = freedMb > 1024 ? $"{Math.Round(freedMb / 1024, 2)} GB" : $"{freedMb} MB";
            MessageBox.Show($"【缓存清理完成】\n\n成功清空了 {cleanedCount} 个无用缓存目录！\n共为您释放磁盘空间: {freedText}\n\n已保留您的全部密码、Cookies、网页历史与书签！", "清理成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void CmbChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await CheckAndDisplayVersionAsync();
                UpdateAiButtonStatus();
            }
        }

        // ================== 🌟 界面状态更新 ==================
        private void UpdateBorderStyles(bool chromeExists, bool isDefault, bool shortcutsExist)
        {
            var activeRed = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
            var defaultText = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 73, 94));

            if (rdo64.IsChecked == true)
            {
                rdo64.Foreground = activeRed; rdo64.FontWeight = FontWeights.Bold;
                rdo32.Foreground = defaultText; rdo32.FontWeight = FontWeights.Normal;
            }
            else
            {
                rdo32.Foreground = activeRed; rdo32.FontWeight = FontWeights.Bold;
                rdo64.Foreground = defaultText; rdo64.FontWeight = FontWeights.Normal;
            }

            for (int i = 0; i < cmbChannel.Items.Count; i++)
            {
                if (cmbChannel.Items[i] is ComboBoxItem item)
                {
                    if (chromeExists && i == cmbChannel.SelectedIndex)
                    {
                        item.Foreground = activeRed; item.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        item.Foreground = defaultText; item.FontWeight = FontWeights.Normal;
                    }
                }
            }

            if (chromeExists)
            {
                cmbChannel.Foreground = activeRed; cmbChannel.FontWeight = FontWeights.Bold;
                if (isDefault)
                {
                    rdoDefaultYes.IsChecked = true; rdoDefaultYes.Foreground = activeRed; rdoDefaultYes.FontWeight = FontWeights.Bold;
                    rdoDefaultNo.Foreground = defaultText; rdoDefaultNo.FontWeight = FontWeights.Normal;
                }
                else
                {
                    rdoDefaultNo.IsChecked = true; rdoDefaultNo.Foreground = activeRed; rdoDefaultNo.FontWeight = FontWeights.Bold;
                    rdoDefaultYes.Foreground = defaultText; rdoDefaultYes.FontWeight = FontWeights.Normal;
                }
                if (shortcutsExist)
                {
                    rdoShortcutYes.IsChecked = true; rdoShortcutYes.Foreground = activeRed; rdoShortcutYes.FontWeight = FontWeights.Bold;
                    rdoShortcutNo.Foreground = defaultText; rdoShortcutNo.FontWeight = FontWeights.Normal;
                }
                else
                {
                    rdoShortcutNo.IsChecked = true; rdoShortcutNo.Foreground = activeRed; rdoShortcutNo.FontWeight = FontWeights.Bold;
                    rdoShortcutYes.Foreground = defaultText; rdoShortcutYes.FontWeight = FontWeights.Normal;
                }
            }
            else
            {
                cmbChannel.Foreground = defaultText; cmbChannel.FontWeight = FontWeights.Normal;
                rdoDefaultNo.IsChecked = true; rdoDefaultYes.Foreground = defaultText; rdoDefaultYes.FontWeight = FontWeights.Normal;
                rdoDefaultNo.Foreground = defaultText; rdoDefaultNo.FontWeight = FontWeights.Normal;
                rdoShortcutNo.IsChecked = true; rdoShortcutYes.Foreground = defaultText; rdoShortcutYes.FontWeight = FontWeights.Normal;
                rdoShortcutNo.Foreground = defaultText; rdoShortcutNo.FontWeight = FontWeights.Normal;
            }

            if (rdoDisableCacheYes.IsChecked == true)
            {
                rdoDisableCacheYes.Foreground = activeRed; rdoDisableCacheYes.FontWeight = FontWeights.Bold;
                rdoDisableCacheNo.Foreground = defaultText; rdoDisableCacheNo.FontWeight = FontWeights.Normal;
            }
            else
            {
                rdoDisableCacheNo.Foreground = activeRed; rdoDisableCacheNo.FontWeight = FontWeights.Bold;
                rdoDisableCacheYes.Foreground = defaultText; rdoDisableCacheYes.FontWeight = FontWeights.Normal;
            }

            if (rdoLimitYes.IsChecked == true)
            {
                rdoLimitYes.Foreground = activeRed; rdoLimitYes.FontWeight = FontWeights.Bold;
                rdoLimitNo.Foreground = defaultText; rdoLimitNo.FontWeight = FontWeights.Normal;
            }
            else
            {
                rdoLimitNo.Foreground = activeRed; rdoLimitNo.FontWeight = FontWeights.Bold;
                rdoLimitYes.Foreground = defaultText; rdoLimitYes.FontWeight = FontWeights.Normal;
            }
        }

        private async Task CheckAndDisplayVersionAsync()
        {
            string localVer = "";
            if (Directory.Exists(portableDir) && File.Exists(chromeExe))
            {
                try
                {
                    localVer = FileVersionInfo.GetVersionInfo(chromeExe).FileVersion;
                    ManageShortcuts();
                }
                catch { }
            }

            if (string.IsNullOrEmpty(localVer))
            {
                lblStatus.Text = "本地还没有Chrome浏览器，请点击【检查更新】来获取！";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                UpdateBorderStyles(false, false, false);
                return;
            }

            UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop());
            lblStatus.Text = $"本地已存在版本为 {localVer}，正在比对云端最新版本...";
            lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 73, 94));

            string os = GetOsType();
            int channelIndex = cmbChannel.SelectedIndex;
            bool is64 = rdo64.IsChecked == true;
            string onlineVer = await Task.Run(() => FetchLatestVersion(os, channelIndex, is64));

            if (string.IsNullOrEmpty(onlineVer))
            {
                lblStatus.Text = $"本地Chrome浏览器版本为 {localVer} (云端检测失败)";
                return;
            }

            if (CompareVersions(localVer, onlineVer) >= 0)
            {
                lblStatus.Text = $"本地Chrome浏览器版本为 {localVer}，已经是最新版了！";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
            }
            else
            {
                lblStatus.Text = $"本地浏览器版本为 {localVer}，最新版 {onlineVer}，请【检查更新】！";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 126, 34));
            }
        }

        // ================== 🌟 快捷方式与默认浏览器接管 ==================
        private bool ShortcutsExistOnDesktop()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            return File.Exists(Path.Combine(desktop, "Chrome 便携版.lnk"));
        }

        private bool IsPortableChromeDefault()
        {
            try
            {
                string normalizedExe = GetNormalizedPath(chromeExe).ToLower().Replace("\\", "/");
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice"))
                {
                    if (key != null && key.GetValue("ProgId") != null)
                    {
                        string progId = key.GetValue("ProgId").ToString();
                        using (var cmdKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command") ?? Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command"))
                        {
                            if (cmdKey != null && cmdKey.GetValue("") != null && cmdKey.GetValue("").ToString().ToLower().Replace("\\", "/").Contains(normalizedExe)) return true;
                        }
                        return false;
                    }
                }
                using (var legacyKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\https\shell\open\command"))
                {
                    if (legacyKey != null && legacyKey.GetValue("") != null && legacyKey.GetValue("").ToString().ToLower().Replace("\\", "/").Contains(normalizedExe)) return true;
                }
            }
            catch { }
            return false;
        }

        private void ManageShortcuts()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPortable = Path.Combine(desktop, "Chrome 便携版.lnk");
                string shortcutNormal = Path.Combine(desktop, "Chrome 浏览器.lnk");

                if (File.Exists(shortcutNormal)) { try { File.Delete(shortcutNormal); } catch { } }

                if (rdoShortcutYes.IsChecked == true)
                {
                    Type t = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(t);

                    string exePath = GetNormalizedPath(chromeExe);
                    string dataPath = GetNormalizedPath(userDataDir);

                    dynamic sPortable = shell.CreateShortcut(shortcutPortable);
                    sPortable.TargetPath = exePath;
                    sPortable.Arguments = $"--user-data-dir=\"{dataPath}\"";
                    sPortable.WorkingDirectory = Path.GetDirectoryName(exePath);
                    sPortable.Save();
                }
                else
                {
                    if (File.Exists(shortcutPortable)) File.Delete(shortcutPortable);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新快捷方式失败: {ex.Message}");
            }
        }

        public void RdoShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(portableDir) && File.Exists(chromeExe))
            {
                ManageShortcuts();
                UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop());
            }
        }

        public async void RdoDefault_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(portableDir) || !File.Exists(chromeExe)) return;

            if (rdoDefaultNo.IsChecked == true)
            {
                UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop());
                return;
            }

            if (rdoDefaultYes.IsChecked == true)
            {
                borderDefault.IsEnabled = false;
                string originalText = rdoDefaultYes.Content.ToString();

                await Task.Run(() => RegisterPortableChrome());

                Version osVer = Environment.OSVersion.Version;
                if (osVer.Major >= 10 || (osVer.Major == 6 && osVer.Minor >= 2))
                {
                    rdoDefaultYes.Content = "请在弹出的系统设置中确认...";
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (osVer.Major >= 10) Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
                            else Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.DefaultPrograms /page pageDefaultProgram") { UseShellExecute = true });
                        }
                        catch { }
                    });

                    bool isSetSuccess = false;
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(1000);
                        if (IsPortableChromeDefault()) { isSetSuccess = true; break; }
                    }

                    if (isSetSuccess)
                    {
                        lblStatus.Text = "太棒了！已成功将便携版设为默认浏览器。";
                        lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                    }
                    else
                    {
                        lblStatus.Text = "等待超时或已取消，未检测到默认浏览器变更。";
                        lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                    }
                }
                else
                {
                    rdoDefaultYes.Content = "正在极速配置...";
                    await Task.Run(() => ForceSetLegacyDefault());
                    await Task.Delay(500);
                    lblStatus.Text = "配置成功！已接管 Win7/XP 默认浏览器关联。";
                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                }

                rdoDefaultYes.Content = originalText;
                borderDefault.IsEnabled = true;
                UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop());
            }
        }

        private void RegisterPortableChrome()
        {
            string exePath = GetNormalizedPath(chromeExe);
            string iconPath = $"{exePath},0";
            string progId = "ChromePortableHTML";
            string clientName = "ChromePortable";
            string appName = "Google Chrome 便携版";
            string appDesc = "智能且快速的便携式 Web 浏览器";

            string configJson = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}";
            ApplyEnterprisePolicies(configJson);

            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser)
                {
                    string[] garbageKeys = { @"Software\Classes\ChromeHTML.Portable", @"Software\Clients\StartMenuInternet\ChromePortable", @"Software\Classes\ChromePortableHTML" };
                    foreach (var gk in garbageKeys) { try { key.DeleteSubKeyTree(gk, false); } catch { } }

                    bool isHijacked = false;
                    try { using (var chk = key.OpenSubKey(@"Software\Classes\ChromeHTML\shell\open\command")) { if (chk != null && chk.GetValue("")?.ToString().Contains("--user-data-dir") == true) isHijacked = true; } } catch { }

                    if (isHijacked)
                    {
                        try { key.DeleteSubKeyTree(@"Software\Classes\ChromeHTML", false); } catch { }
                        try { key.DeleteSubKeyTree(@"Software\Clients\StartMenuInternet\Google Chrome", false); } catch { }
                        try { using (var regApps = key.OpenSubKey(@"Software\RegisteredApplications", true)) { regApps?.DeleteValue("Google Chrome", false); } } catch { }
                    }
                }
            }
            catch { }

            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser)
                {
                    using (var progIdKey = key.CreateSubKey($@"Software\Classes\{progId}"))
                    {
                        progIdKey.SetValue("", appName);
                        progIdKey.SetValue("URL Protocol", "");

                        using (var icon = progIdKey.CreateSubKey("DefaultIcon")) icon.SetValue("", iconPath);
                        using (var cmd = progIdKey.CreateSubKey(@"shell\open\command"))
                        {
                            cmd.SetValue("", $"\"{exePath}\" --single-argument %1");
                            cmd.SetValue("DelegateExecute", "");
                        }
                        try { progIdKey.DeleteSubKeyTree(@"shell\open\ddeexec", false); } catch { }

                        using (var appKey = progIdKey.CreateSubKey("Application"))
                        {
                            appKey.SetValue("ApplicationIcon", iconPath);
                            appKey.SetValue("ApplicationName", appName);
                            appKey.SetValue("ApplicationDescription", appDesc);
                            appKey.SetValue("ApplicationCompany", "Google");
                        }
                    }

                    using (var clientKey = key.CreateSubKey($@"Software\Clients\StartMenuInternet\{clientName}"))
                    {
                        clientKey.SetValue("", appName);
                        using (var icon = clientKey.CreateSubKey("DefaultIcon")) icon.SetValue("", iconPath);
                        using (var cmd = clientKey.CreateSubKey(@"shell\open\command"))
                            cmd.SetValue("", $"\"{exePath}\"");

                        using (var capKey = clientKey.CreateSubKey("Capabilities"))
                        {
                            capKey.SetValue("ApplicationDescription", appDesc);
                            capKey.SetValue("ApplicationIcon", iconPath);
                            capKey.SetValue("ApplicationName", appName);

                            using (var fileAssoc = capKey.CreateSubKey("FileAssociations"))
                            {
                                fileAssoc.SetValue(".htm", progId);
                                fileAssoc.SetValue(".html", progId);
                                fileAssoc.SetValue(".webp", progId);
                                fileAssoc.SetValue(".pdf", progId);
                            }

                            using (var urlAssoc = capKey.CreateSubKey("URLAssociations"))
                            {
                                urlAssoc.SetValue("http", progId);
                                urlAssoc.SetValue("https", progId);
                                urlAssoc.SetValue("ftp", progId);
                            }
                        }
                    }

                    using (var regAppsKey = key.CreateSubKey(@"Software\RegisteredApplications"))
                    {
                        regAppsKey.SetValue(clientName, $@"Software\Clients\StartMenuInternet\{clientName}\Capabilities");
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"注册失败: {ex.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ForceSetLegacyDefault()
        {
            try
            {
                string exePath = GetNormalizedPath(chromeExe);
                string configJson = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}";
                ApplyEnterprisePolicies(configJson);

                using (var key = Microsoft.Win32.Registry.CurrentUser)
                {
                    string[] protocols = { "http", "https" };
                    foreach (var p in protocols)
                    {
                        using (var cmdKey = key.CreateSubKey($@"Software\Classes\{p}\shell\open\command"))
                        {
                            cmdKey.SetValue("", $"\"{exePath}\" --single-argument %1");
                            cmdKey.SetValue("DelegateExecute", "");
                        }
                        try { key.DeleteSubKeyTree($@"Software\Classes\{p}\shell\open\ddeexec", false); } catch { }
                    }
                }
            }
            catch { }
        }

        // ================== 🌟 核心操作按钮 ==================
        public void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(chromeExe))
            {
                try
                {
                    ApplyChromeTuningConfig();
                    ManageShortcuts();

                    string exePath = GetExactCanonicalPath(chromeExe);
                    string dataPath = GetExactCanonicalPath(userDataDir);

                    Process.Start(new ProcessStartInfo(exePath, $"--user-data-dir=\"{dataPath}\"") { UseShellExecute = true });
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"启动发生错误: {ex.Message}", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else MessageBox.Show("请先检查并更新！");
        }

        private void BtnAICheck_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAiModelPresent(out List<string> foundPaths)) return;
            var result = MessageBox.Show("【AI 模型清理】\n\n检测到本地或系统存在已静默下载的 AI 模型！\n\n是否立即物理清理，并通过企业策略彻底锁死未来下载通道？", "确认清理", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ApplyChromeTuningConfig();
                int deletedCount = 0; long freedBytes = 0;
                foreach (var path in foundPaths)
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) { try { freedBytes += new FileInfo(file).Length; } catch { } }
                            Directory.Delete(path, true); deletedCount++;
                            if (!File.Exists(path)) { File.WriteAllText(path, "LOCKED_BY_CHROME_PORTABLE"); File.SetAttributes(path, FileAttributes.ReadOnly | FileAttributes.Hidden); }
                        }
                    }
                    catch (Exception ex) { MessageBox.Show($"清理失败: {ex.Message}\n请先关闭浏览器！", "错误", MessageBoxButton.OK, MessageBoxImage.Error); UpdateAiButtonStatus(); return; }
                }
                double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 2);
                string freedText = freedMb > 1024 ? $"{Math.Round(freedMb / 1024, 2)} GB" : $"{freedMb} MB";
                MessageBox.Show($"【清理完成】\n\n清除 {deletedCount} 个 AI 模型目录！\n共释放空间: {freedText}\n\n已为您通过策略外置引擎锁死 AI 静默下载！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateAiButtonStatus();
            }
        }

        public async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            btnUpdate.IsEnabled = btnLaunch.IsEnabled = false;
            string os = GetOsType();
            int channelIndex = cmbChannel.SelectedIndex;
            bool is64 = rdo64.IsChecked == true;
            string onlineVer = await Task.Run(() => FetchLatestVersion(os, channelIndex, is64));

            if (string.IsNullOrEmpty(onlineVer))
            {
                MessageBox.Show("网络连接超时！");
                btnUpdate.IsEnabled = btnLaunch.IsEnabled = true;
                return;
            }

            string localVer = File.Exists(chromeExe) ? FileVersionInfo.GetVersionInfo(chromeExe).FileVersion : "";
            if (CompareVersions(localVer, onlineVer) >= 0)
            {
                MessageBox.Show("已经是最新版了亲。");
                await CheckAndDisplayVersionAsync();
                btnUpdate.IsEnabled = btnLaunch.IsEnabled = true;
                return;
            }

            OverlayPanel.Visibility = Visibility.Visible;
            StartBlinking();
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Directory.CreateDirectory(portableDir);
                var (_, downloadUrl) = GetChromeDownloadInfo(os, onlineVer, is64, channelIndex);
                string szPath = Path.Combine(portableDir, "7zr.exe");
                if (!File.Exists(szPath)) await Download7zWithFallback(szPath);

                string installerPath = Path.Combine(portableDir, "chrome_installer.exe");
                lblOverlayStatus.Text = $"正在直连下载 Chrome {onlineVer}...";

                using (var client = CreateSafeWebClient())
                {
                    client.DownloadProgressChanged += (s, ev) => { progressBar.Value = ev.ProgressPercentage; };
                    await client.DownloadFileTaskAsync(new Uri(downloadUrl), installerPath);
                    progressBar.Value = 0;
                }

                lblOverlayStatus.Text = "正在执行双重静静解压缩...";
                progressBar.IsIndeterminate = true;
                await Task.Run(() => PerformExtraction(szPath, installerPath));
                progressBar.IsIndeterminate = false;
                progressBar.Value = 100;

                ApplyChromeTuningConfig();
                ManageShortcuts();

                if (rdoDefaultYes.IsChecked == true)
                    RunProcess(chromeExe, "--make-default-browser");

                System.Media.SystemSounds.Asterisk.Play();
                lblOverlayStatus.Text = $"Chrome浏览器已成功升级为最新版 {onlineVer}！";
                await Task.Delay(7000);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新发生错误: {ex.Message}");
            }
            finally
            {
                StopBlinking();
                OverlayPanel.Visibility = Visibility.Collapsed;
                await CheckAndDisplayVersionAsync();
                UpdateAiButtonStatus();
                btnUpdate.IsEnabled = btnLaunch.IsEnabled = true;
            }
        }

        // ================== 🌟 下载、解压网络模块 ==================
        private void PerformExtraction(string szPath, string installerPath)
        {
            string temp1 = Path.Combine(portableDir, "Temp1");
            string temp2 = Path.Combine(portableDir, "Temp2");
            string temp3 = Path.Combine(portableDir, "Temp3");
            string temp4 = Path.Combine(portableDir, "Temp4");

            if (Directory.Exists(temp1)) Directory.Delete(temp1, true);
            if (Directory.Exists(temp2)) Directory.Delete(temp2, true);
            if (Directory.Exists(temp3)) Directory.Delete(temp3, true);
            if (Directory.Exists(temp4)) Directory.Delete(temp4, true);

            RunProcess(szPath, $"x \"{installerPath}\" -o\"{temp1}\" -y");

            string updater7z = Path.Combine(temp1, "updater.7z");
            if (File.Exists(updater7z))
                RunProcess(szPath, $"x \"{updater7z}\" -o\"{temp2}\" -y");
            else
                temp2 = temp1;

            string[] innerInstallers = Directory.GetFiles(temp2, "*_chrome_installer.exe", SearchOption.AllDirectories);
            if (innerInstallers.Length > 0)
                RunProcess(szPath, $"x \"{innerInstallers[0]}\" -o\"{temp3}\" -y");
            else
                temp3 = temp2;

            string[] chrome7zs = Directory.GetFiles(temp3, "chrome.7z", SearchOption.AllDirectories);
            if (chrome7zs.Length > 0)
                RunProcess(szPath, $"x \"{chrome7zs[0]}\" -o\"{temp4}\" -y");
            else
                temp4 = temp3;

            string[] chromeBinDirs = Directory.GetDirectories(temp4, "Chrome-bin", SearchOption.AllDirectories);
            if (chromeBinDirs.Length > 0)
            {
                string targetDir = Path.Combine(portableDir, "Chrome");
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
                Directory.Move(chromeBinDirs[0], targetDir);
            }

            if (!Directory.Exists(userDataDir)) Directory.CreateDirectory(userDataDir);
            if (Directory.Exists(temp1)) Directory.Delete(temp1, true);
            if (Directory.Exists(temp2)) Directory.Delete(temp2, true);
            if (Directory.Exists(temp3)) Directory.Delete(temp3, true);
            if (Directory.Exists(temp4)) Directory.Delete(temp4, true);
            if (File.Exists(installerPath)) File.Delete(installerPath);
        }

        private Storyboard blinkStoryboard;
        private Storyboard poemStoryboard;

        private void StartBlinking()
        {
            DoubleAnimation anim = new DoubleAnimation(1, 0.2, TimeSpan.FromMilliseconds(500)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            blinkStoryboard = new Storyboard();
            blinkStoryboard.Children.Add(anim);
            Storyboard.SetTarget(anim, lblOverlayStatus);
            Storyboard.SetTargetProperty(anim, new PropertyPath(TextBlock.OpacityProperty));
            blinkStoryboard.Begin();
        }

        private void StopBlinking()
        {
            blinkStoryboard?.Stop();
            lblOverlayStatus.Opacity = 1;
        }

        public void ProgressBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (poemStoryboard != null)
                {
                    poemStoryboard.Stop();
                    poemStoryboard = null;
                    txtPoem.Text = "";
                    return;
                }
                txtPoem.Text = poemText + poemText + poemText;
                DoubleAnimation scrollAnim = new DoubleAnimation { From = 180, To = -350, Duration = TimeSpan.FromSeconds(15), RepeatBehavior = RepeatBehavior.Forever };
                poemStoryboard = new Storyboard();
                poemStoryboard.Children.Add(scrollAnim);
                Storyboard.SetTarget(scrollAnim, txtPoem);
                Storyboard.SetTargetProperty(scrollAnim, new PropertyPath("(Canvas.Top)"));
                poemStoryboard.Begin();
            }
        }

        private WebClient CreateSafeWebClient()
        {
            WebClient c = new WebClient { Proxy = null };
            c.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            c.Headers[HttpRequestHeader.Accept] = "*/*";
            return c;
        }

        private async Task Download7zWithFallback(string savePath)
        {
            string[] urls = {
                "https://gitee.com/zhoupan/AI-OCR/raw/master/7zr.exe",
                "https://www.7-zip.org/a/7zr.exe",
                "https://cytranet-dal.dl.sourceforge.net/project/sevenzip/7-Zip/25.01/7zr.exe"
            };
            foreach (string u in urls)
            {
                try
                {
                    using (var c = CreateSafeWebClient())
                    {
                        await c.DownloadFileTaskAsync(new Uri(u), savePath);
                        return;
                    }
                }
                catch { }
            }
            throw new Exception("7zr.exe 下载失败");
        }

        private string FetchLatestVersion(string osType, int channelIndex, bool is64)
        {
            if (osType == "XP") return "49.0.2623.112";
            if (osType == "7/8") return "109.0.5414.120";

            string ap = is64 ?
                (channelIndex == 0 ? "x64-stable-statsdef_1" : channelIndex == 1 ? "x64-beta-statsdef_1" : channelIndex == 2 ? "x64-dev-statsdef_1" : "x64-canary-statsdef_1") :
                (channelIndex == 0 ? "stable-arch_x86-statsdef_1" : channelIndex == 1 ? "beta-arch_x86-statsdef_1" : channelIndex == 2 ? "dev-arch_x86-statsdef_1" : "canary-arch_x86-statsdef_1");
            string appID = channelIndex == 3 ? "{4ea16ac7-fd5a-47c3-875b-dbf8a2000d21}" : "{8A69D345-D564-463C-AFF1-A69D9E530F96}";
            string arch = is64 ? "x64" : "x86";

            string xmlData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<request protocol=""3.0"" updater=""Omaha"" updaterversion=""1.3.36.352"" shell_version=""1.3.36.352"" ismachine=""0"" sessionid=""{{00000000-0000-0000-0000-000000000000}}"" installsource=""ondemandcheckforupdate"" requestid=""{{00000000-0000-0000-0000-000000000000}}"" dedup=""cr"">
  <os platform=""win"" version=""10.0"" sp="""" arch=""{arch}""/>
  <app appid=""{appID}"" version=""0.0.0.0"" nextversion="""" ap=""{ap}"" lang=""zh-CN"" brand=""GGLS"" client="""" installage=""-1"">
    <updatecheck/>
  </app>
</request>";

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(xmlData);

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://update.googleapis.com/service/update2");
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = postBytes.Length;
                req.Timeout = 8000;

                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(postBytes, 0, postBytes.Length);
                }

                using (HttpWebResponse res = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                {
                    Match m = Regex.Match(sr.ReadToEnd(), @"<manifest\s+version=""([^""]+)""");
                    if (m.Success) return m.Groups[1].Value;
                }
            }
            catch { }
            return "126.0.0.0";
        }

        private (string Version, string Url) GetChromeDownloadInfo(string osType, string version, bool is64, int chIdx)
        {
            if (osType == "XP") return ("49.0.2623.112", "https://dl.google.com/release2/h8vnfiy7pvn3lxy9ehfsaxlrnnukgff8jnghr1/49.0.2623.112_chrome_installer.exe");
            if (osType == "7/8") return ("109.0.5414.120", is64 ? "https://dl.google.com/release2/chrome/czao2hrvpk5wgqrkz4kks5r734_109.0.5414.120/109.0.5414.120_chrome_installer.exe" : "https://dl.google.com/release2/chrome/acihtkcueyye3ymoj2afvv7ulzxa_109.0.5414.120/109.0.5414.120_chrome_installer.exe");

            string ap = is64 ?
                (chIdx == 0 ? "x64-stable-statsdef_1" : chIdx == 1 ? "x64-beta-statsdef_1" : chIdx == 2 ? "x64-dev-statsdef_1" : "x64-canary-statsdef_1") :
                (chIdx == 0 ? "stable-arch_x86-statsdef_1" : chIdx == 1 ? "beta-arch_x86-statsdef_1" : chIdx == 2 ? "dev-arch_x86-statsdef_1" : "canary-arch_x86-statsdef_1");
            string fName = chIdx == 3 ? (is64 ? "ChromeCanarySetup64.exe" : "ChromeCanarySetup.exe") : (is64 ? "ChromeStandaloneSetup64.exe" : "ChromeStandaloneSetup.exe");
            string appID = chIdx == 3 ? "%7B4ea16ac7-fd5a-47c3-875b-dbf8a2000d21%7D" : "%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D";

            return (version, $"https://dl.google.com/tag/s/appguid%3D{appID}%26iid%3D%7B00000000-0000-0000-0000-000000000000%7D%26lang%3Dzh-CN%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26ap%3D{ap}%26installdataindex%3Dempty/update2/installers/{fName}");
        }

        private string GetOsType()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k != null && k.GetValue("CurrentMajorVersionNumber") != null && int.Parse(k.GetValue("CurrentMajorVersionNumber").ToString()) >= 10)
                        return "10/11";
                }
            }
            catch { }
            return Environment.OSVersion.Version.Major <= 5 ? "XP" : "7/8";
        }

        private int CompareVersions(string vA, string vB)
        {
            try { return new Version(vA).CompareTo(new Version(vB)); }
            catch { return string.Compare(vA, vB, StringComparison.OrdinalIgnoreCase); }
        }

        private void RunProcess(string exe, string args)
        {
            Process p = new Process { StartInfo = { FileName = exe, Arguments = args, CreateNoWindow = true, UseShellExecute = false } };
            p.Start();
            p.WaitForExit();
        }
    }
}
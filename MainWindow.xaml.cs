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
                PerformSilentCacheClean();

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

        private string GetExactCanonicalPath(string path) => GetNormalizedPath(path);

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
                    MessageBox.Show("【便携版使用建议】\n\n检测到您当前在系统盘或桌面运行本程序。\n建议您将文件夹移动到 D盘、E盘 等非系统盘下再运行！", "路径提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // ================== 🌟 UI 窗口与按钮事件 ==================
        public void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { this.DragMove(); }
        public void BtnMinimize_Click(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        public void BtnClose_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        public void Copyright_Click(object sender, MouseButtonEventArgs e)
        {
            if (Directory.Exists(portableDir) && File.Exists(chromeExe) && IsPortableChromeDefault()) OpenUrl("https://t.me/YuC2027");
        }

        public void Github_Click(object sender, RoutedEventArgs e) { OpenUrl("https://github.com/ZhangSir9901/Chrome_Portable_Updater"); }

        private void WeChat_Click(object sender, RoutedEventArgs e)
        {
            try { Clipboard.SetText("jiujiujiayi666"); MessageBox.Show("微信号已成功复制！", "技术支持", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show($"复制失败: {ex.Message}", "技术支持"); }
        }

        private void Telegram_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(portableDir) && File.Exists(chromeExe) && IsPortableChromeDefault()) OpenUrl("https://t.me/YuC2027");
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
            lblStatus.Text = rdoDisableCacheYes.IsChecked == true ? "已成功开启无用缓存拦截！" : "已解除无用缓存拦截。";
            lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(rdoDisableCacheYes.IsChecked == true ? System.Windows.Media.Color.FromRgb(46, 204, 113) : System.Windows.Media.Color.FromRgb(52, 73, 94));
        }

        public void RdoLimit_Click(object sender, RoutedEventArgs e)
        {
            txtCacheLimit.IsEnabled = (rdoLimitYes.IsChecked == true);
            ApplyChromeTuningConfig();
            UpdateBorderStyles(Directory.Exists(portableDir) && File.Exists(chromeExe), IsPortableChromeDefault(), ShortcutsExistOnDesktop());
            lblStatus.Text = rdoLimitYes.IsChecked == true ? $"已限制磁盘缓存上限为 {txtCacheLimit.Text} GB！" : "已取消缓存容量限制。";
            lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(rdoLimitYes.IsChecked == true ? System.Windows.Media.Color.FromRgb(46, 204, 113) : System.Windows.Media.Color.FromRgb(52, 73, 94));
        }

        public void TxtCacheLimit_PreviewTextInput(object sender, TextCompositionEventArgs e) { e.Handled = !Regex.IsMatch(e.Text, @"[\d\.]"); }

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

        private async void CmbChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) { await CheckAndDisplayVersionAsync(); UpdateAiButtonStatus(); }
        }

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
                catch (Exception ex) { MessageBox.Show($"启动发生错误: {ex.Message}", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            else MessageBox.Show("请先检查并更新！");
        }

        // ================== 🌟 高级极客面板互斥控制 ==================
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
                MessageBox.Show("极客参数已保存并注入底层组策略！\n重启浏览器后即可生效。", "应用成功", MessageBoxButton.OK, MessageBoxImage.Information);
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

        // ================== 🌟 UI 交互动画 ==================
        private Storyboard blinkStoryboard;
        private Storyboard poemStoryboard;

        private void StartBlinking()
        {
            DoubleAnimation anim = new DoubleAnimation(1, 0.2, TimeSpan.FromMilliseconds(500)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            blinkStoryboard = new Storyboard(); blinkStoryboard.Children.Add(anim);
            Storyboard.SetTarget(anim, lblOverlayStatus); Storyboard.SetTargetProperty(anim, new PropertyPath(TextBlock.OpacityProperty));
            blinkStoryboard.Begin();
        }

        private void StopBlinking() { blinkStoryboard?.Stop(); lblOverlayStatus.Opacity = 1; }

        public void ProgressBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (poemStoryboard != null) { poemStoryboard.Stop(); poemStoryboard = null; txtPoem.Text = ""; return; }
                txtPoem.Text = poemText + poemText + poemText;
                DoubleAnimation scrollAnim = new DoubleAnimation { From = 180, To = -350, Duration = TimeSpan.FromSeconds(15), RepeatBehavior = RepeatBehavior.Forever };
                poemStoryboard = new Storyboard(); poemStoryboard.Children.Add(scrollAnim);
                Storyboard.SetTarget(scrollAnim, txtPoem); Storyboard.SetTargetProperty(scrollAnim, new PropertyPath("(Canvas.Top)"));
                poemStoryboard.Begin();
            }
        }
    }
}
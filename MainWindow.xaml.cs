using System;
using System.Diagnostics;
using System.IO;
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

        // ================== 🌟 UI 窗口与基础交互事件 ==================
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

        // ================== 🌟 被误删的清理与体检按钮补回 ==================
        public void BtnCleanCache_Click(object sender, RoutedEventArgs e)
        {
            var (freedBytes, cleanedCount) = PerformSilentCacheClean();
            ApplyChromeTuningConfig();
            double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 2);
            string freedText = freedMb > 1024 ? $"{Math.Round(freedMb / 1024, 2)} GB" : $"{freedMb} MB";
            MessageBox.Show($"【缓存清理完成】\n\n成功清空了 {cleanedCount} 个无用缓存目录！\n共为您释放磁盘空间: {freedText}\n\n已保留您的全部密码、Cookies、网页历史与书签！", "清理成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void BtnAICheck_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAiModelPresent(out List<string> foundPaths)) return;
            if (MessageBox.Show("【全局 AI 模型清理】\n\n检测到便携版、系统 Chrome 或 Edge 中存在已静默下载的本地大模型(占用数GB空间)！\n\n是否立即物理粉碎，并通过企业策略彻底锁死未来下载通道？", "确认清理", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                ApplyChromeTuningConfig();
                int deletedCount = 0; long freedBytes = 0;
                foreach (var path in foundPaths)
                {
                    try { if (Directory.Exists(path)) { foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) { try { freedBytes += new FileInfo(file).Length; } catch { } } Directory.Delete(path, true); deletedCount++; if (!File.Exists(path)) { File.WriteAllText(path, "LOCKED"); File.SetAttributes(path, FileAttributes.ReadOnly | FileAttributes.Hidden); } } }
                    catch (Exception ex) { MessageBox.Show($"清理失败: {ex.Message}\n请先关闭全部 Chrome 与 Edge 浏览器窗口！", "错误", MessageBoxButton.OK, MessageBoxImage.Error); UpdateAiButtonStatus(); return; }
                }
                double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 2); string freedText = freedMb > 1024 ? $"{Math.Round(freedMb / 1024, 2)} GB" : $"{freedMb} MB";
                MessageBox.Show($"【清理完成】\n\n清除 {deletedCount} 个 AI 模型目录！\n共释放空间: {freedText}\n\n已为您锁死 Chrome 与 Edge 的 AI 静默下载！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateAiButtonStatus();
            }
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
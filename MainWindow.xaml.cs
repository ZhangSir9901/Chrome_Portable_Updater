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

        // 🌟 反射核心：读取 AssemblyInfo.cs 里的“产品信息版本（Informational Version）”
        private static readonly string APP_VERSION = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion;

        // 彩蛋诗词
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
            // 🌟 启动清理：检查并物理粉碎前一次自升级遗留的影子旧文件
            CleanupOldUpdater();

            CheckRunningDirectory();

            try { this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/logo.ico")); } catch { }

            if (Environment.Is64BitOperatingSystem) rdo64.Content = "64位 (自动检测)";
            else { rdo32.IsChecked = true; rdo32.Content = "32位 (自动检测)"; }

            // 绑定前台静态版本信息
            lblAppVersion.Text = $"当前升级器版本: {APP_VERSION}";

            // 启动时静默检测升级器自身新版本 (Fire and Forget)
            _ = CheckSelfUpdateAsync();

            // 启动时检测浏览器更新状态
            await CheckAndDisplayVersionAsync();

            // 启动时自动扫描一次本地与系统 AI 模型垃圾
            UpdateAiButtonStatus();
        }

        // ================== 🌟 极客级核心引擎：自更新与影子热替换 ==================
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
                using (var client = CreateSafeWebClient())
                {
                    // 利用 jsDelivr 直连 Github 获取 JSON，国内绝对不墙且极速
                    string json = await client.DownloadStringTaskAsync("https://cdn.jsdelivr.net/gh/ZhangSir9901/Chrome_Portable_Updater@main/update.json");

                    Match mVer = Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
                    Match mUrl = Regex.Match(json, @"""url""\s*:\s*""([^""]+)""");

                    if (mVer.Success && mUrl.Success)
                    {
                        string onlineVer = mVer.Groups[1].Value;
                        string downloadUrl = mUrl.Groups[1].Value;

                        // 版本对比 (针对 2026-07-17 这种日期格式，直接使用字符串字典序比对即可)
                        if (string.Compare(onlineVer, APP_VERSION, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            // 发现新版本！隐藏旧文本，显示酷炫闪动按钮
                            lblAppVersion.Visibility = Visibility.Collapsed;
                            btnAppUpdate.Visibility = Visibility.Visible;
                            btnAppUpdate.Content = $"🚀 发现新版本 {onlineVer}，点击立即自动升级！";
                            btnAppUpdate.Tag = downloadUrl; // 隐蔽地存储下载链接
                        }
                    }
                }
            }
            catch { } // 网络不通则静默跳过，绝不干扰主流程
        }

        private async void BtnAppUpdate_Click(object sender, RoutedEventArgs e)
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

                // 开始下载最新 EXE 到同目录下
                using (var client = CreateSafeWebClient())
                {
                    client.DownloadProgressChanged += (s, ev) => { progressBar.Value = ev.ProgressPercentage; };
                    await client.DownloadFileTaskAsync(new Uri(downloadUrl), newExe);
                }

                lblOverlayStatus.Text = "下载完成，正在执行无感影子热替换...";
                progressBar.IsIndeterminate = true;
                await Task.Delay(800); // 留一点视觉喘息时间

                // 🌟 核心：影子重命名热替换法
                if (File.Exists(oldExe)) File.Delete(oldExe);
                File.Move(currentExe, oldExe);  // 正在运行的 EXE 改名为 .old
                File.Move(newExe, currentExe);  // 刚下载好的新文件改回原名

                // 金蝉脱壳，拉起新的自己，然后自毁当前进程
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
        // ================== 结束 ==================


        private void CheckRunningDirectory()
        {
            try
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string downloadsPath = Path.Combine(userProfile, "Downloads");

                bool isSystemDrive = currentDir.StartsWith(systemDrive, StringComparison.OrdinalIgnoreCase);
                bool isDesktop = currentDir.StartsWith(desktopPath, StringComparison.OrdinalIgnoreCase);
                bool isDownloads = currentDir.StartsWith(downloadsPath, StringComparison.OrdinalIgnoreCase);

                if (isSystemDrive || isDesktop || isDownloads)
                {
                    MessageBox.Show(
                        "【便携版使用建议】\n\n检测到您当前在 系统盘/桌面/下载目录 运行本程序。\n\n作为一款纯正的便携版浏览器，为了防止未来重装系统导致您的书签、账号密码丢失，强烈建议您关闭本程序后，将整个文件夹【剪切】到 D盘、E盘 等非系统盘下再运行！",
                        "智能路径提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                string folderName;

                switch (channelIndex)
                {
                    case 1: folderName = "Chrome Beta"; break;
                    case 2: folderName = "Chrome Dev"; break;
                    case 3: folderName = "Chrome SxS"; break;
                    default: folderName = "Chrome"; break;
                }
                return Path.Combine(localAppData, "Google", folderName, "User Data");
            }
            catch
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
            }
        }

        private bool IsAiModelPresent(out List<string> foundPaths)
        {
            foundPaths = new List<string>();
            string portableAiPath = Path.Combine(userDataDir, "OptGuideOnDeviceModel");
            if (Directory.Exists(portableAiPath) && Directory.GetFiles(portableAiPath, "*", SearchOption.AllDirectories).Length > 0)
            {
                foundPaths.Add(portableAiPath);
            }

            try
            {
                string localAiPath = Path.Combine(GetLocalUserDataDir(), "OptGuideOnDeviceModel");
                if (Directory.Exists(localAiPath) && Directory.GetFiles(localAiPath, "*", SearchOption.AllDirectories).Length > 0)
                {
                    foundPaths.Add(localAiPath);
                }
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
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166)); // #95A5A6
                    btnAICheck.ToolTip = "未检测到本地或系统 Chrome 浏览器";
                    return;
                }

                if (IsAiModelPresent(out List<string> foundPaths))
                {
                    btnAICheck.Content = "存在AI模型";
                    btnAICheck.IsEnabled = true;
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 84, 0)); // #D35400
                    btnAICheck.ToolTip = "点击可清理！！！";
                }
                else
                {
                    btnAICheck.Content = "纯净无AI模型";
                    btnAICheck.IsEnabled = true;
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(155, 89, 182)); // #9B59B6
                    btnAICheck.ToolTip = "恭喜！当前环境非常纯净，没有 AI 模型垃圾文件。";
                }
            }
            catch { }
        }

        private void ApplyChromeTuningConfig()
        {
            try
            {
                var chromeProcesses = Process.GetProcessesByName("chrome");
                if (chromeProcesses.Length > 0)
                {
                    MessageBox.Show(
                        "【参数注入提示】\n\n检测到您的系统原生 Chrome 浏览器正在后台运行。\n\n由于运行中的 Chrome 会独占并锁定配置文件，为了成功彻底屏蔽本地的【问问 Gemini/AI 功能】，请您先保存好当前网页后，手动关闭全部 Chrome 窗口。\n\n关闭后，请点击本窗口的【确定】按钮，继续进行深度优化参数写入！",
                        "请先关闭 Chrome", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch { }

            var targetUserDirs = new List<string> { userDataDir };

            try
            {
                string localPath = GetLocalUserDataDir();
                if (!string.IsNullOrEmpty(localPath)) targetUserDirs.Add(localPath);
            }
            catch { }

            foreach (var uDir in targetUserDirs)
            {
                try
                {
                    string defaultDir = Path.Combine(uDir, "Default");
                    if (!Directory.Exists(defaultDir)) Directory.CreateDirectory(defaultDir);

                    string localStatePath = Path.Combine(uDir, "Local State");
                    string localStateContent = File.Exists(localStatePath) ? File.ReadAllText(localStatePath) : "{}";

                    localStateContent = InjectLabsExperiments(localStateContent);
                    File.WriteAllText(localStatePath, localStateContent);

                    string prefPath = Path.Combine(defaultDir, "Preferences");
                    string prefContent = File.Exists(prefPath) ? File.ReadAllText(prefPath) : "{}";

                    prefContent = InjectPreferences(prefContent);
                    File.WriteAllText(prefPath, prefContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"对目录 {uDir} 进行环境注入时发生异常: {ex.Message}");
                }
            }
        }

        private string InjectLabsExperiments(string json)
        {
            var targetFlags = new List<string>
            {
                "optimization-guide-on-device-model@2",
                "prompt-api-for-gemini-nano@2",
                "ai-mode-omnibox-entrypoint@2",
                "glic@2",
                "summarization-api-for-gemini-nano@2",
                "compose@2",
                "enable-parallel-downloading@1",
                "hardware-media-key-handling@2",
                "smooth-scrolling@1",
                "enable-gpu-rasterization@1"
            };

            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                return "{\"browser\":{\"enabled_labs_experiments\":[" + string.Join(",", targetFlags.ConvertAll(f => $"\"{f}\"")) + "]}}";
            }

            if (!json.Contains("\"browser\""))
            {
                int index = json.IndexOf('{');
                if (index >= 0) json = json.Insert(index + 1, "\"browser\":{},");
            }

            string browserPattern = @"""browser""\s*:\s*\{";
            Match browserMatch = Regex.Match(json, browserPattern);
            if (browserMatch.Success)
            {
                int insertPos = browserMatch.Index + browserMatch.Length;
                if (!json.Contains("\"enabled_labs_experiments\""))
                {
                    json = json.Insert(insertPos, "\"enabled_labs_experiments\":[],");
                }
            }

            string arrayPattern = @"""enabled_labs_experiments""\s*:\s*\[([^\]]*)\]";
            Match arrayMatch = Regex.Match(json, arrayPattern);
            if (arrayMatch.Success)
            {
                string oldArrayContent = arrayMatch.Groups[1].Value;
                var currentFlags = new List<string>();
                var matches = Regex.Matches(oldArrayContent, @"""([^""]+)""");
                foreach (Match m in matches)
                {
                    currentFlags.Add(m.Groups[1].Value);
                }

                foreach (var flag in targetFlags)
                {
                    string flagBase = flag.Split('@')[0] + "@";
                    currentFlags.RemoveAll(f => f.StartsWith(flagBase));
                    currentFlags.Add(flag);
                }

                string newArrayContent = string.Join(",", currentFlags.ConvertAll(f => $"\"{f}\""));
                json = Regex.Replace(json, arrayPattern, $"\"enabled_labs_experiments\":[{newArrayContent}]");
            }

            return json;
        }

        private string InjectPreferences(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                return "{\"session\":{\"restore_on_startup\":1},\"performance_tuning\":{\"high_efficiency_mode\":{\"state\":1,\"mode\":1},\"battery_saver_mode\":{\"state\":1}}}";
            }

            json = InjectJsonKeyValue(json, "session", "restore_on_startup", "1");
            json = InjectJsonKeyValue(json, "performance_tuning", "high_efficiency_mode", "{\"state\":1,\"mode\":1}");
            json = InjectJsonKeyValue(json, "performance_tuning", "battery_saver_mode", "{\"state\":1}");

            return json;
        }

        private string InjectJsonKeyValue(string json, string parentKey, string childKey, string value)
        {
            if (!json.Contains($"\"{parentKey}\""))
            {
                int index = json.IndexOf('{');
                if (index >= 0) json = json.Insert(index + 1, $"\"{parentKey}\":{{}},");
            }

            string parentPattern = @"""" + parentKey + @"""\s*:\s*\{";
            Match parentMatch = Regex.Match(json, parentPattern);
            if (parentMatch.Success)
            {
                int insertPos = parentMatch.Index + parentMatch.Length;
                string childPattern = @"""" + childKey + @"""\s*:\s*([^,{}]+|\{[^{}]*\})";
                Match childMatch = Regex.Match(json, childPattern);

                if (childMatch.Success)
                {
                    string parentSubSection = json.Substring(parentMatch.Index);
                    string updatedSub = Regex.Replace(parentSubSection, childPattern, $"\"{childKey}\":{value}", RegexOptions.None);
                    json = json.Substring(0, parentMatch.Index) + updatedSub;
                }
                else
                {
                    json = json.Insert(insertPos, $"\"{childKey}\":{value},");
                }
            }

            json = json.Replace(",,", ",").Replace(",}", "}").Replace("{,", "{");
            return json;
        }

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

        private void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch
            {
                try { Process.Start("cmd", $"/c start {url.Replace("&", "^&")}"); }
                catch (Exception ex) { MessageBox.Show($"无法打开链接: {ex.Message}"); }
            }
        }

        private async void CmbChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                await CheckAndDisplayVersionAsync();
                UpdateAiButtonStatus();
            }
        }

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
                        item.Foreground = activeRed;
                        item.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        item.Foreground = defaultText;
                        item.FontWeight = FontWeights.Normal;
                    }
                }
            }

            if (chromeExists)
            {
                cmbChannel.Foreground = activeRed;
                cmbChannel.FontWeight = FontWeights.Bold;

                if (isDefault)
                {
                    rdoDefaultYes.IsChecked = true;
                    rdoDefaultYes.Foreground = activeRed; rdoDefaultYes.FontWeight = FontWeights.Bold;
                    rdoDefaultNo.Foreground = defaultText; rdoDefaultNo.FontWeight = FontWeights.Normal;
                }
                else
                {
                    rdoDefaultNo.IsChecked = true;
                    rdoDefaultNo.Foreground = activeRed; rdoDefaultNo.FontWeight = FontWeights.Bold;
                    rdoDefaultYes.Foreground = defaultText; rdoDefaultYes.FontWeight = FontWeights.Normal;
                }

                if (shortcutsExist)
                {
                    rdoShortcutYes.IsChecked = true;
                    rdoShortcutYes.Foreground = activeRed; rdoShortcutYes.FontWeight = FontWeights.Bold;
                    rdoShortcutNo.Foreground = defaultText; rdoShortcutNo.FontWeight = FontWeights.Normal;
                }
                else
                {
                    rdoShortcutNo.IsChecked = true;
                    rdoShortcutNo.Foreground = activeRed; rdoShortcutNo.FontWeight = FontWeights.Bold;
                    rdoShortcutYes.Foreground = defaultText; rdoShortcutYes.FontWeight = FontWeights.Normal;
                }
            }
            else
            {
                cmbChannel.Foreground = defaultText;
                cmbChannel.FontWeight = FontWeights.Normal;

                rdoDefaultNo.IsChecked = true;
                rdoDefaultYes.Foreground = defaultText; rdoDefaultYes.FontWeight = FontWeights.Normal;
                rdoDefaultNo.Foreground = defaultText; rdoDefaultNo.FontWeight = FontWeights.Normal;

                rdoShortcutNo.IsChecked = true;
                rdoShortcutYes.Foreground = defaultText; rdoShortcutYes.FontWeight = FontWeights.Normal;
                rdoShortcutNo.Foreground = defaultText; rdoShortcutNo.FontWeight = FontWeights.Normal;
            }
        }

        private async Task CheckAndDisplayVersionAsync()
        {
            string localVer = "";
            if (Directory.Exists(portableDir) && File.Exists(chromeExe))
            {
                try { localVer = FileVersionInfo.GetVersionInfo(chromeExe).FileVersion; ManageShortcuts(); } catch { }
            }

            if (string.IsNullOrEmpty(localVer))
            {
                lblStatus.Text = "本地还没有Chrome浏览器，请点击【检查并更新】来获取！";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                UpdateBorderStyles(false, false, false);
                return;
            }

            bool isDefault = IsPortableChromeDefault();
            bool isShortcut = ShortcutsExistOnDesktop();
            UpdateBorderStyles(true, isDefault, isShortcut);

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
                lblStatus.Text = $"本地浏览器版本为 {localVer}，最新版本为 {onlineVer}，请点击【检查并更新】获取最新版！";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 126, 34));
            }
        }

        private bool ShortcutsExistOnDesktop()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPortable = Path.Combine(desktop, "Chrome 便携版.lnk");
            string shortcutNormal = Path.Combine(desktop, "Chrome 浏览器.lnk");
            return File.Exists(shortcutPortable) || File.Exists(shortcutNormal);
        }

        private bool IsPortableChromeDefault()
        {
            try
            {
                string normalizedExe = chromeExe.ToLower().Replace("\\", "/");

                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice"))
                {
                    if (key != null && key.GetValue("ProgId") != null)
                    {
                        string progId = key.GetValue("ProgId").ToString();
                        using (var cmdKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command") ?? Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command"))
                        {
                            if (cmdKey != null && cmdKey.GetValue("") != null)
                            {
                                string cmdStr = cmdKey.GetValue("").ToString().ToLower().Replace("\\", "/");
                                if (cmdStr.Contains(normalizedExe)) return true;
                            }
                        }
                        return false;
                    }
                }

                using (var legacyKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\https\shell\open\command"))
                {
                    if (legacyKey != null && legacyKey.GetValue("") != null)
                    {
                        string cmdStr = legacyKey.GetValue("").ToString().ToLower().Replace("\\", "/");
                        if (cmdStr.Contains(normalizedExe)) return true;
                    }
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

                if (rdoShortcutYes.IsChecked == true)
                {
                    Type t = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(t);

                    dynamic sPortable = shell.CreateShortcut(shortcutPortable);
                    sPortable.TargetPath = chromeExe;
                    sPortable.Arguments = $"--user-data-dir=\"{userDataDir}\" --no-first-run";
                    sPortable.WorkingDirectory = Path.GetDirectoryName(chromeExe);
                    sPortable.Save();

                    dynamic sNormal = shell.CreateShortcut(shortcutNormal);
                    sNormal.TargetPath = chromeExe;
                    sNormal.Arguments = "";
                    sNormal.WorkingDirectory = Path.GetDirectoryName(chromeExe);
                    sNormal.Save();
                }
                else
                {
                    if (File.Exists(shortcutPortable)) File.Delete(shortcutPortable);
                    if (File.Exists(shortcutNormal)) File.Delete(shortcutNormal);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新或清理快捷方式失败: {ex.Message}");
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
                bool isWin10Or11 = osVer.Major >= 10;
                bool isWin8 = osVer.Major == 6 && osVer.Minor >= 2;

                if (isWin10Or11 || isWin8)
                {
                    rdoDefaultYes.Content = "请在弹出的系统设置中确认...";
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (isWin10Or11) Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
                            else if (isWin8) Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.DefaultPrograms /page pageDefaultProgram") { UseShellExecute = true });
                        }
                        catch { }
                    });

                    bool isSetSuccess = false;
                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(1000);
                        if (IsPortableChromeDefault())
                        {
                            isSetSuccess = true;
                            break;
                        }
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

                    lblStatus.Text = "配置成功！已霸道接管 Win7/XP 的默认浏览器关联。";
                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                }

                rdoDefaultYes.Content = originalText;
                borderDefault.IsEnabled = true;
                UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop());
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

        public void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(chromeExe))
            {
                try
                {
                    ApplyChromeTuningConfig();
                    ManageShortcuts();
                    Process.Start(new ProcessStartInfo(chromeExe, $"--user-data-dir=\"{userDataDir}\"") { UseShellExecute = true });
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"启动浏览器时发生错误: {ex.Message}", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else MessageBox.Show("请先检查并更新！");
        }

        private void BtnAICheck_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAiModelPresent(out List<string> foundPaths)) return;

            var result = MessageBox.Show(
                "【AI 模型极客清理】\n\n检测到本地便携版或系统 Chrome 的 UserData 目录中存在已下载的本地 AI 模型 (Gemini Nano)！\n\n这些模型文件通常在后台偷偷下载运行并占用高达 4GB 的系统磁盘空间。\n\n是否立即物理清理这些 AI 模型缓存文件，并强力锁定系统安全策略阻断未来再次静默下载？",
                "确认清理 AI 模型", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApplyChromeTuningConfig();

                int deletedCount = 0;
                long freedBytes = 0;

                foreach (var path in foundPaths)
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                            foreach (var file in files)
                            {
                                try { freedBytes += new FileInfo(file).Length; } catch { }
                            }
                            Directory.Delete(path, true);
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"清理目录 {path} 失败，部分文件可能正被正在运行的 Chrome 独占锁定。\n\n请您先关闭所有浏览器窗口，然后重试！\n\n错误信息: {ex.Message}", "清理失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        UpdateAiButtonStatus();
                        return;
                    }
                }

                double freedMb = Math.Round((double)freedBytes / (1024 * 1024), 2);
                string freedText = freedMb > 1024 ? $"{Math.Round(freedMb / 1024, 2)} GB" : $"{freedMb} MB";

                MessageBox.Show($"【清理完成】\n\n成功物理清除 {deletedCount} 个 AI 模型缓存目录！\n共释放磁盘空间: {freedText}\n\n已为您注入最新安全机制，彻底锁死 AI 静默下载！", "清理成功", MessageBoxButton.OK, MessageBoxImage.Information);

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

            if (string.IsNullOrEmpty(onlineVer)) { MessageBox.Show("网络连接超时！"); btnUpdate.IsEnabled = btnLaunch.IsEnabled = true; return; }

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
                if (rdoDefaultYes.IsChecked == true) RunProcess(chromeExe, "--make-default-browser");

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

        private void PerformExtraction(string szPath, string installerPath)
        {
            string temp1 = Path.Combine(portableDir, "Temp1"), temp2 = Path.Combine(portableDir, "Temp2");
            string temp3 = Path.Combine(portableDir, "Temp3"), temp4 = Path.Combine(portableDir, "Temp4");

            if (Directory.Exists(temp1)) Directory.Delete(temp1, true);
            if (Directory.Exists(temp2)) Directory.Delete(temp2, true);
            if (Directory.Exists(temp3)) Directory.Delete(temp3, true);
            if (Directory.Exists(temp4)) Directory.Delete(temp4, true);

            RunProcess(szPath, $"x \"{installerPath}\" -o\"{temp1}\" -y");

            string updater7z = Path.Combine(temp1, "updater.7z");
            if (File.Exists(updater7z)) RunProcess(szPath, $"x \"{updater7z}\" -o\"{temp2}\" -y"); else temp2 = temp1;

            string[] innerInstallers = Directory.GetFiles(temp2, "*_chrome_installer.exe", SearchOption.AllDirectories);
            if (innerInstallers.Length > 0) RunProcess(szPath, $"x \"{innerInstallers[0]}\" -o\"{temp3}\" -y"); else temp3 = temp2;

            string[] chrome7zs = Directory.GetFiles(temp3, "chrome.7z", SearchOption.AllDirectories);
            if (chrome7zs.Length > 0) RunProcess(szPath, $"x \"{chrome7zs[0]}\" -o\"{temp4}\" -y"); else temp4 = temp3;

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

        private WebClient CreateSafeWebClient()
        {
            WebClient c = new WebClient { Proxy = null };
            c.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0";
            return c;
        }

        private async Task Download7zWithFallback(string savePath)
        {
            string[] urls = { "https://gitee.com/zhoupan/AI-OCR/raw/master/7zr.exe", "https://www.7-zip.org/a/7zr.exe", "https://cytranet-dal.dl.sourceforge.net/project/sevenzip/7-Zip/25.01/7zr.exe" };
            foreach (string u in urls)
            {
                try { using (var c = CreateSafeWebClient()) { await c.DownloadFileTaskAsync(new Uri(u), savePath); return; } } catch { }
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
                    string responseXml = sr.ReadToEnd();
                    Match m = Regex.Match(responseXml, @"<manifest\s+version=""([^""]+)""");
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
            string ap = is64 ? (chIdx == 0 ? "x64-stable-statsdef_1" : chIdx == 1 ? "x64-beta-statsdef_1" : chIdx == 2 ? "x64-dev-statsdef_1" : "x64-canary-statsdef_1") : (chIdx == 0 ? "stable-arch_x86-statsdef_1" : chIdx == 1 ? "beta-arch_x86-statsdef_1" : chIdx == 2 ? "dev-arch_x86-statsdef_1" : "canary-arch_x86-statsdef_1");
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
                    if (k != null && k.GetValue("CurrentMajorVersionNumber") != null && int.Parse(k.GetValue("CurrentMajorVersionNumber").ToString()) >= 10) return "10/11";
                }
            }
            catch { }
            return Environment.OSVersion.Version.Major <= 5 ? "XP" : "7/8";
        }

        private int CompareVersions(string vA, string vB) { try { return new Version(vA).CompareTo(new Version(vB)); } catch { return string.Compare(vA, vB, StringComparison.OrdinalIgnoreCase); } }

        private void RunProcess(string exe, string args) { Process p = new Process { StartInfo = { FileName = exe, Arguments = args, CreateNoWindow = true, UseShellExecute = false } }; p.Start(); p.WaitForExit(); }

        private void RegisterPortableChrome()
        {
            try
            {
                string exePath = chromeExe;
                string iconPath = $"{exePath},0";
                string progId = "ChromeHTML.Portable";
                string appName = "Google Chrome 便携版";
                string appDesc = "智能且快速的便携式 Web 浏览器";

                using (var key = Microsoft.Win32.Registry.CurrentUser)
                {
                    using (var progIdKey = key.CreateSubKey($@"Software\Classes\{progId}"))
                    {
                        progIdKey.SetValue("", appName);
                        using (var icon = progIdKey.CreateSubKey("DefaultIcon")) icon.SetValue("", iconPath);
                        using (var cmd = progIdKey.CreateSubKey(@"shell\open\command"))
                            cmd.SetValue("", $"\"{exePath}\" --user-data-dir=\"{userDataDir}\" --single-argument %1");

                        using (var appKey = progIdKey.CreateSubKey("Application"))
                        {
                            appKey.SetValue("ApplicationIcon", iconPath);
                            appKey.SetValue("ApplicationName", appName);
                            appKey.SetValue("ApplicationDescription", appDesc);
                            appKey.SetValue("ApplicationCompany", "Google");
                        }
                    }

                    using (var clientKey = key.CreateSubKey(@"Software\Clients\StartMenuInternet\ChromePortable"))
                    {
                        clientKey.SetValue("", appName);
                        using (var icon = clientKey.CreateSubKey("DefaultIcon")) icon.SetValue("", iconPath);
                        using (var cmd = clientKey.CreateSubKey(@"shell\open\command"))
                            cmd.SetValue("", $"\"{exePath}\" --user-data-dir=\"{userDataDir}\"");

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
                        regAppsKey.SetValue("ChromePortable", @"Software\Clients\StartMenuInternet\ChromePortable\Capabilities");
                    }
                }
            }
            catch { }
        }

        private void ForceSetLegacyDefault()
        {
            try
            {
                string exePath = chromeExe;
                using (var key = Microsoft.Win32.Registry.CurrentUser)
                {
                    string[] protocols = { "http", "https" };
                    foreach (var p in protocols)
                    {
                        using (var cmdKey = key.CreateSubKey($@"Software\Classes\{p}\shell\open\command"))
                        {
                            cmdKey.SetValue("", $"\"{exePath}\" --user-data-dir=\"{userDataDir}\" --single-argument %1");
                        }
                    }
                }
            }
            catch { }
        }

        private void WeChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText("jiujiujiayi666");
                MessageBox.Show("微信号【jiujiujiayi666】已成功复制到剪贴板！\n您可以直接去微信中粘贴搜索、添加好友。", "技术支持", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"微信号复制失败: {ex.Message}\n请手动记录微信号: jiujiujiayi666", "技术支持");
            }
        }

        private void Telegram_Click(object sender, RoutedEventArgs e)
        {
            bool chromeExists = Directory.Exists(portableDir) && File.Exists(chromeExe);
            bool isDefault = IsPortableChromeDefault();
            if (chromeExists && isDefault) OpenUrl("https://t.me/YuC2027");
        }
    }
}
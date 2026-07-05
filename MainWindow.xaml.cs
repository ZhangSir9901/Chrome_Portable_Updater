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

namespace ChromeUpdaterWPF
{
    public partial class MainWindow : Window
    {
        private string portableDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Chrome_Portable");
        private string chromeExe;
        private string userDataDir;

        // 🌟 核心：单点维护版本号，在此处修改，全程序（包括底部版权）自动同步更新！
        private const string APP_VERSION = "8.17";

        // 彩蛋诗词
        private string poemText = "巴女浅醉黄鹤楼，\n江风吹皱美人绸。\n此别经年何时了，\n云锁巫山夜未犹。\n\n";

        public MainWindow()
        {
            InitializeComponent();
            chromeExe = Path.Combine(portableDir, "Chrome", "chrome.exe");
            userDataDir = Path.Combine(portableDir, "UserData");

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try { this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/logo.ico")); } catch { }

            if (Environment.Is64BitOperatingSystem) rdo64.Content = "64位 (自动检测)";
            else { rdo32.IsChecked = true; rdo32.Content = "32位 (自动检测)"; }

            // 绑定动态版本信息
            lblCopyright.Text = $"版本: {APP_VERSION}   技术支持: 微信: jiujiujiayi666   Telegram: @YuC2027";

            // 启动时检测
            await CheckAndDisplayVersionAsync();
        }

        // ================== 无边框窗口控制 ==================
        public void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { this.DragMove(); }
        public void BtnMinimize_Click(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        public void BtnClose_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        // 🌟 完美的 CMD 盲交跳转逻辑，只有本地 Chrome 存在且已设为系统默认时，才允许跳转
        public void Copyright_Click(object sender, MouseButtonEventArgs e)
        {
            bool chromeExists = Directory.Exists(portableDir) && File.Exists(chromeExe);
            bool isDefault = IsPortableChromeDefault();

            if (chromeExists && isDefault)
            {
                OpenUrl("https://t.me/YuC2027");
            }
        }

        // 点击 Github 图标跳转
        public void Github_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/ZhangSir9901/Chrome_Portable_Updater");
        }

        private void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch
            {
                try { Process.Start("cmd", $"/c start {url.Replace("&", "^&")}"); }
                catch (Exception ex) { MessageBox.Show($"无法打开链接: {ex.Message}"); }
            }
        }

        // ================== 🌟 业务逻辑：启动与切换下拉菜单时的智能比对 ==================
        private async void CmbChannel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) await CheckAndDisplayVersionAsync();
        }

        // 🌟 修复 2a/2b：亮起红色 (#E74C3C) 且加粗，其余回弹灰色
        private void UpdateBorderStyles(bool chromeExists, bool isDefault, bool shortcutsExist)
        {
            var activeRed = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)); // 红色
            var defaultText = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 73, 94)); // 默认灰色

            // 1. 系统架构（第一组）永远高亮红色并加粗
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

            // 更新下拉菜单项的颜色
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
                // 2. 版本类型文本颜色变红、加粗
                cmbChannel.Foreground = activeRed;
                cmbChannel.FontWeight = FontWeights.Bold;

                // 3. 默认浏览器
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

                // 4. 快捷方式
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
                // 没有 Chrome，后三组全部置灰并回弹
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

            // 状况 A：没有浏览器，后三组选项全部置灰
            if (string.IsNullOrEmpty(localVer))
            {
                lblStatus.Text = "本地还没有Chrome浏览器，请点击【检查并更新】来获取！";
                lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)); // 红色
                UpdateBorderStyles(false, false, false);
                return;
            }

            // 状况 B & C：检测到本地浏览器，智能高亮红色
            bool isDefault = IsPortableChromeDefault();
            bool isShortcut = ShortcutsExistOnDesktop();
            UpdateBorderStyles(true, isDefault, isShortcut);

            lblStatus.Text = $"本地已存在版本为 {localVer}，正在比对云端最新版本...";
            lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 73, 94));

            string os = GetOsType();
            int channelIndex = cmbChannel.SelectedIndex;
            string onlineVer = await Task.Run(() => FetchLatestVersionFromAli(os, channelIndex));

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

        // 🌟 修复：路径归一化匹配算法，100% 精准判定默认浏览器
        // ================= 阶段三：归一化路径匹配（通杀双重注册表体系） =================
        private bool IsPortableChromeDefault()
        {
            try
            {
                string normalizedExe = chromeExe.ToLower().Replace("\\", "/");

                // 1. 优先检查 Win8/10/11 有 Hash 保护的 UserChoice
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice"))
                {
                    if (key != null && key.GetValue("ProgId") != null)
                    {
                        string progId = key.GetValue("ProgId").ToString();

                        // 为了防坑，同时在 HKCU 和 HKCR 下搜寻对应的执行命令
                        using (var cmdKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command")
                                         ?? Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command"))
                        {
                            if (cmdKey != null && cmdKey.GetValue("") != null)
                            {
                                string cmdStr = cmdKey.GetValue("").ToString().ToLower().Replace("\\", "/");
                                if (cmdStr.Contains(normalizedExe)) return true;
                            }
                        }
                        return false; // 如果被其他浏览器占据，直接返回 false
                    }
                }

                // 2. 如果没有 UserChoice（说明是 XP 或 Win7 等老系统），直接检查 Classes 传统关联
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

        // 🌟 修复 1：两个快捷方式名字改为：“Chrome 便携版”和“Chrome 浏览器”
        private void ManageShortcuts()
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

        // ================= 中枢指挥：点击事件跨平台智能派发 =================
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

                // 【核心前置】不废话，先静默给便携版上系统户口！
                await Task.Run(() => RegisterPortableChrome());

                Version osVer = Environment.OSVersion.Version;
                bool isWin10Or11 = osVer.Major >= 10;
                bool isWin8 = osVer.Major == 6 && osVer.Minor >= 2;

                // 【策略 B】：针对 Windows 8 / 10 / 11 （曲线合规引导）
                if (isWin10Or11 || isWin8)
                {
                    rdoDefaultYes.Content = "请在弹出的系统设置中确认...";

                    await Task.Run(() =>
                    {
                        try
                        {
                            // 不同系统呼出控制面板的指令不同
                            if (isWin10Or11)
                                Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
                            else if (isWin8)
                                Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.DefaultPrograms /page pageDefaultProgram") { UseShellExecute = true });
                        }
                        catch { }
                    });

                    // 智能轮询 30 秒。因为我们已经挂了号，用户现在能在设置里看到 "Google Chrome 便携版" 了！
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
                    // 【策略 A】：针对 Windows XP / 7 （简单粗暴，瞬间强改）
                    rdoDefaultYes.Content = "正在极速配置...";
                    await Task.Run(() => ForceSetLegacyDefault());
                    await Task.Delay(500); // 假装停顿一下，让 UI 不显得突兀

                    lblStatus.Text = "配置成功！已霸道接管 Win7/XP 的默认浏览器关联。";
                    lblStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                }

                // 无论如何，恢复控件状态并按照注册表真实结果渲染边框和选中态
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
                ManageShortcuts();
                Process.Start(chromeExe);
                Application.Current.Shutdown();
            }
            else MessageBox.Show("请先检查并更新！");
        }

        // ================== 更新核心逻辑 ==================
        public async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            btnUpdate.IsEnabled = btnLaunch.IsEnabled = false;

            string os = GetOsType();
            int channelIndex = cmbChannel.SelectedIndex;
            string onlineVer = await Task.Run(() => FetchLatestVersionFromAli(os, channelIndex));

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

                var (_, downloadUrl) = GetChromeDownloadInfo(os, onlineVer, rdo64.IsChecked == true, channelIndex);

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

        // ================== WPF 特色的纯正 GPU 动画引擎 ==================
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

                DoubleAnimation scrollAnim = new DoubleAnimation
                {
                    From = 180,
                    To = -350,
                    Duration = TimeSpan.FromSeconds(15),
                    RepeatBehavior = RepeatBehavior.Forever
                };

                poemStoryboard = new Storyboard(); poemStoryboard.Children.Add(scrollAnim);
                Storyboard.SetTarget(scrollAnim, txtPoem); Storyboard.SetTargetProperty(scrollAnim, new PropertyPath("(Canvas.Top)"));
                poemStoryboard.Begin();
            }
        }

        // ================== 底层网络与环境工具 ==================
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

        private string FetchLatestVersionFromAli(string osType, int channelIndex)
        {
            if (osType == "XP") return "49.0.2623.112"; if (osType == "7/8") return "109.0.5414.120";
            try
            {
                using (var c = CreateSafeWebClient())
                {
                    string json = c.DownloadString("https://registry.npmmirror.com/-/binary/chrome-for-testing/last-known-good-versions.json");
                    string ch = channelIndex == 0 ? "Stable" : channelIndex == 1 ? "Beta" : channelIndex == 2 ? "Dev" : "Canary";
                    Match m = Regex.Match(json, @"""" + ch + @"""\s*:\s*\{[^}]*""version""\s*:\s*""([^""]+)""");
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


        // ================= 阶段一：给便携版上“系统户口” (终极增强版) =================
        private void RegisterPortableChrome()
        {
            try
            {
                string exePath = chromeExe;
                string iconPath = $"{exePath},0";
                string progId = "ChromeHTML.Portable";
                string appName = "Google Chrome 便携版"; // 认准这个中文名！
                string appDesc = "智能且快速的便携式 Web 浏览器";

                using (var key = Microsoft.Win32.Registry.CurrentUser)
                {
                    // 1. 注册专属 ProgID
                    using (var progIdKey = key.CreateSubKey($@"Software\Classes\{progId}"))
                    {
                        progIdKey.SetValue("", appName);
                        using (var icon = progIdKey.CreateSubKey("DefaultIcon")) icon.SetValue("", iconPath);
                        using (var cmd = progIdKey.CreateSubKey(@"shell\open\command")) cmd.SetValue("", $"\"{exePath}\" --single-argument %1");

                        // 🌟 新增：Win10/11 要求 ProgId 下必须有 Application 元数据，否则不认图标
                        using (var appKey = progIdKey.CreateSubKey("Application"))
                        {
                            appKey.SetValue("ApplicationIcon", iconPath);
                            appKey.SetValue("ApplicationName", appName);
                            appKey.SetValue("ApplicationDescription", appDesc);
                            appKey.SetValue("ApplicationCompany", "Google");
                        }
                    }

                    // 2. 在“开始菜单”和默认客户端注册表里挂号
                    // 注意：注册表键名不要带空格，改为 ChromePortable
                    using (var clientKey = key.CreateSubKey(@"Software\Clients\StartMenuInternet\ChromePortable"))
                    {
                        clientKey.SetValue("", appName);
                        using (var icon = clientKey.CreateSubKey("DefaultIcon")) icon.SetValue("", iconPath);
                        using (var cmd = clientKey.CreateSubKey(@"shell\open\command")) cmd.SetValue("", $"\"{exePath}\"");

                        using (var capKey = clientKey.CreateSubKey("Capabilities"))
                        {
                            capKey.SetValue("ApplicationDescription", appDesc);
                            capKey.SetValue("ApplicationIcon", iconPath);
                            capKey.SetValue("ApplicationName", appName);

                            // 🌟 致命修复：Win10 判定“是不是浏览器”的死逻辑是：必须能关联 .htm 和 .html！
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

                    // 3. 彻底暴露给 Windows 设置面板 (值必须是指向 Capabilities 的相对路径)
                    using (var regAppsKey = key.CreateSubKey(@"Software\RegisteredApplications"))
                    {
                        regAppsKey.SetValue("ChromePortable", @"Software\Clients\StartMenuInternet\ChromePortable\Capabilities");
                    }
                }
            }
            catch { }
        }

        // ================= 阶段二 (策略A)：XP/Win7 老系统暴力强改 =================
        private void ForceSetLegacyDefault()
        {
            try
            {
                string exePath = chromeExe;
                using (var key = Microsoft.Win32.Registry.CurrentUser)
                {
                    // 对于没有 Hash 保护的老系统，直接接管 http 和 https 关联
                    string[] protocols = { "http", "https" };
                    foreach (var p in protocols)
                    {
                        using (var cmdKey = key.CreateSubKey($@"Software\Classes\{p}\shell\open\command"))
                        {
                            cmdKey.SetValue("", $"\"{exePath}\" --single-argument %1");
                        }
                    }
                }
            }
            catch { }
        }
    }
}
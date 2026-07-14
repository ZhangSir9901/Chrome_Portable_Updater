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

        // 🌟 读取你在 AssemblyInfo.cs 代码里填写的任何漂亮文本
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
            // 🌟 启动拦截：程序一加载，立马检测运行路径！
            CheckRunningDirectory();

            try { this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/logo.ico")); } catch { }

            if (Environment.Is64BitOperatingSystem) rdo64.Content = "64位 (自动检测)";
            else { rdo32.IsChecked = true; rdo32.Content = "32位 (自动检测)"; }

            // 绑定动态版本信息
            lblCopyright.Text = $"版本: {APP_VERSION}";

            // 启动时检测
            await CheckAndDisplayVersionAsync();

            // 🌟 新增：启动时自动扫描一次本地与系统 AI 模型垃圾
            UpdateAiButtonStatus();
        }

        private void CheckRunningDirectory()
        {
            try
            {
                // 1. 获取当前程序所在的真实完整路径，并去掉末尾的斜杠
                string currentDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');

                // 2. 获取系统所在盘符 (绝大多数情况下是 C:\)
                string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));

                // 3. 获取当前用户的桌面和下载目录
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string downloadsPath = Path.Combine(userProfile, "Downloads");

                // 4. 核心判定逻辑：只要在系统盘、桌面、下载目录，就触发警告
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

        // 根据下拉菜单选择，动态计算出本地 C 盘 User Data 对应的具体物理路径
        private string GetLocalUserDataDir()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                int channelIndex = cmbChannel.SelectedIndex;
                string folderName;

                switch (channelIndex)
                {
                    case 1:
                        folderName = "Chrome Beta"; // 测试版目录
                        break;
                    case 2:
                        folderName = "Chrome Dev";  // 开发版目录
                        break;
                    case 3:
                        folderName = "Chrome SxS";  // 金丝雀版目录 (SxS)
                        break;
                    default:
                        folderName = "Chrome";      // 稳定版默认目录
                        break;
                }
                return Path.Combine(localAppData, "Google", folderName, "User Data");
            }
            catch
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
            }
        }

        // 🌟 正则探测 AI 模型目录（OptGuideOnDeviceModel）是否存在且里面有物理文件
        private bool IsAiModelPresent(out List<string> foundPaths)
        {
            foundPaths = new List<string>();

            // 1. 便携版路径
            string portableAiPath = Path.Combine(userDataDir, "OptGuideOnDeviceModel");
            if (Directory.Exists(portableAiPath) && Directory.GetFiles(portableAiPath, "*", SearchOption.AllDirectories).Length > 0)
            {
                foundPaths.Add(portableAiPath);
            }

            // 2. 本地 C 盘路径
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

        // 🌟 核心升级：按照规则动态刷新 AI 体检按钮状态和颜色
        private void UpdateAiButtonStatus()
        {
            try
            {
                bool chromeExists = (Directory.Exists(portableDir) && File.Exists(chromeExe)) || Directory.Exists(GetLocalUserDataDir());

                if (!chromeExists)
                {
                    // 1、未检测到浏览器时 ➔ 显示 「AI 模型体检」（按钮灰色，不可点击）
                    btnAICheck.Content = "AI 模型体检";
                    btnAICheck.IsEnabled = false;
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166)); // #95A5A6 灰色
                    btnAICheck.ToolTip = "未检测到本地或系统 Chrome 浏览器";
                    return;
                }

                if (IsAiModelPresent(out List<string> foundPaths))
                {
                    // 3、检测到存在 AI 垃圾时 ➔ 显示 「存在AI模型」（鼠标放上去提示，按钮橙色，点击可执行清理）
                    btnAICheck.Content = "存在AI模型";
                    btnAICheck.IsEnabled = true;
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 84, 0)); // #D35400 深橙色
                    btnAICheck.ToolTip = "点击可清理！！！";
                }
                else
                {
                    // 2、检测到浏览器但无 AI 垃圾时 ➔ 显示 「纯净无AI模型」（按钮状态正常，按钮紫色，点击无反应）
                    btnAICheck.Content = "纯净无AI模型";
                    btnAICheck.IsEnabled = true;
                    btnAICheck.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(155, 89, 182)); // #9B59B6 皇家紫
                    btnAICheck.ToolTip = "恭喜！当前环境非常纯净，没有 AI 模型垃圾文件。";
                }
            }
            catch { }
        }

        // ================== 🌟 极客级环境注入：纯原生正则免 DLL 注入引擎 (双向注入) ==================
        private void ApplyChromeTuningConfig()
        {
            // 🌟 守护逻辑：检测本地是否有正在运行的 chrome 进程。若不关闭，Chrome 退出时会覆盖我们的参数修改
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
                if (!string.IsNullOrEmpty(localPath))
                {
                    targetUserDirs.Add(localPath);
                }
            }
            catch { }

            foreach (var uDir in targetUserDirs)
            {
                try
                {
                    string defaultDir = Path.Combine(uDir, "Default");
                    if (!Directory.Exists(defaultDir)) Directory.CreateDirectory(defaultDir);

                    // --------------------------------------------------------------------------------
                    // 模块 1：注入 [Local State] -> 禁用 AI、开启多线程下载、平滑滚动、GPU加速
                    // --------------------------------------------------------------------------------
                    string localStatePath = Path.Combine(uDir, "Local State");
                    string localStateContent = File.Exists(localStatePath) ? File.ReadAllText(localStatePath) : "{}";

                    localStateContent = InjectLabsExperiments(localStateContent);
                    File.WriteAllText(localStatePath, localStateContent);

                    // --------------------------------------------------------------------------------
                    // 模块 2：注入 [Preferences] -> 启动页、省电、省内存
                    // --------------------------------------------------------------------------------
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

        // 🌟 正则注入 Local State Flag (禁用 AI 以及性能调优)
        private string InjectLabsExperiments(string json)
        {
            var targetFlags = new List<string>
            {
                "optimization-guide-on-device-model@2", // [必关] 彻底禁止 Chrome 后台偷下 4GB 的 Gemini Nano AI 模型
                "prompt-api-for-gemini-nano@2",         // [必关] 额外封锁：禁用网页 Prompt API，防止网页唤醒 AI 重新下载
                "ai-mode-omnibox-entrypoint@2",         // [必关] 额外封锁：隐藏地址栏中自动弹出的 "Ask Gemini" 快捷入口
                "glic@2",                               // [必关] 彻底屏蔽 146+ 新版标签栏/工具栏中新增的 "问问 Gemini" (glic) 浮动窗口与右键菜单
                "summarization-api-for-gemini-nano@2",  // [必关] 禁用本地 Gemini Nano 总结 API 接口
                "compose@2",                            // [必关] 禁用 Help me write 等 AI 辅助撰写功能
                
                "enable-parallel-downloading@1",        // [必开] 开启多线程并发下载，让自带下载器速度媲美 IDM
                "hardware-media-key-handling@2",        // [必关] 禁用系统级媒体按键监听（解决按音量键出现巨大黑块的问题）
                "smooth-scrolling@1",                   // [必开] 强制开启网页平滑滚动，让鼠标滚轮如丝般顺滑
                "enable-gpu-rasterization@1"            // [必开] 强制开启 GPU 网页渲染加速，提升看图和看视频性能
            };

            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            {
                return "{\"browser\":{\"enabled_labs_experiments\":[" + string.Join(",", targetFlags.ConvertAll(f => $"\"{f}\"")) + "]}}";
            }

            // 确保 browser 节点存在
            if (!json.Contains("\"browser\""))
            {
                int index = json.IndexOf('{');
                if (index >= 0) json = json.Insert(index + 1, "\"browser\":{},");
            }

            // 确保 enabled_labs_experiments 存在于 browser 内部
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

            // 提取并更新 enabled_labs_experiments 数组
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

        // 🌟 正则注入 Preferences (设置起始页、省电、省内存)
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

        // 纯正则构建 JSON 层级注入辅助函数
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
            if (IsLoaded)
            {
                await CheckAndDisplayVersionAsync();
                // 🌟 新增：切换通道时，本地 C 盘 AI 路径改变，自动重刷按钮状态
                UpdateAiButtonStatus();
            }
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

            // 更新下拉菜单项
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

        // 🌟 路径归一化匹配算法，100% 精准判定默认浏览器
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

        // 🌟 快捷方式创建逻辑：两个快捷方式名字改为：“Chrome 便携版”和“Chrome 浏览器”
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

                    // 🌟 第一个：便携版快捷方式（数据强制存储在 Chrome_Portable\UserData 目录下）
                    dynamic sPortable = shell.CreateShortcut(shortcutPortable);
                    sPortable.TargetPath = chromeExe;
                    sPortable.Arguments = $"--user-data-dir=\"{userDataDir}\" --no-first-run";
                    sPortable.WorkingDirectory = Path.GetDirectoryName(chromeExe);
                    sPortable.Save();

                    // 🌟 第二个：原生浏览器快捷方式（不带便携参数，直接读取 C 盘本地 UserData 数据）
                    dynamic sNormal = shell.CreateShortcut(shortcutNormal);
                    sNormal.TargetPath = chromeExe;
                    sNormal.Arguments = ""; // 👈 按照你的要求：这里保持为空，以读取 C 盘本地配置
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

                // 无论如何，恢复控件状态并按照注册表真实结果渲染边框 and 选中态
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
                    // 1. 启动前，对【便携版目录】和【系统 C 盘本地目录】同步注入黄金优化参数
                    ApplyChromeTuningConfig();

                    // 2. 刷新桌面快捷方式状态
                    ManageShortcuts();

                    // 3. 🌟 核心修复：添加 UseShellExecute=true 确保在升级器进程安全退出后，浏览器依然能不受干扰地运行
                    Process.Start(new ProcessStartInfo(chromeExe, $"--user-data-dir=\"{userDataDir}\"") { UseShellExecute = true });

                    // 4. 安全退出升级器
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"启动浏览器时发生错误: {ex.Message}", "启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else MessageBox.Show("请先检查并更新！");
        }

        // 🌟 新增：主界面 AI 体检按钮的点击事件 (负责根据状态机执行相应逻辑)
        private void BtnAICheck_Click(object sender, RoutedEventArgs e)
        {
            // 如果是纯净无AI模型，点击按钮无任何反应
            if (!IsAiModelPresent(out List<string> foundPaths))
            {
                return;
            }

            // 如果检测到本地/系统存在 AI 模型缓存大垃圾，开始物理超度
            var result = MessageBox.Show(
                "【AI 模型极客清理】\n\n检测到本地便携版或系统 Chrome 的 UserData 目录中存在已下载的本地 AI 模型 (Gemini Nano)！\n\n这些模型文件通常在后台偷偷下载运行并占用高达 4GB 的系统磁盘空间。\n\n是否立即物理清理这些 AI 模型缓存文件，并强力锁定系统安全策略阻断未来再次静默下载？",
                "确认清理 AI 模型", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 先写入优化参数禁用未来下载（双管齐下）
                ApplyChromeTuningConfig();

                int deletedCount = 0;
                long freedBytes = 0;

                // 物理删除目录
                foreach (var path in foundPaths)
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            // 计算释放的空间
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
                string freedText = freedMb > 1024
                    ? $"{Math.Round(freedMb / 1024, 2)} GB"
                    : $"{freedMb} MB";

                MessageBox.Show($"【清理完成】\n\n成功物理清除 {deletedCount} 个 AI 模型缓存目录！\n共释放磁盘空间: {freedText}\n\n已为您注入最新安全机制，彻底锁死 AI 静默下载！", "清理成功", MessageBoxButton.OK, MessageBoxImage.Information);

                // 重新扫描刷新状态
                UpdateAiButtonStatus();
            }
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

                // 🌟 新增：更新完新版本后，立刻对【双路径】注入配置！
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
                // 🌟 更新完成后刷新一次本地 AI 缓存状态
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
                        // 🌟 修复：强力注入 --user-data-dir，确保从外部唤醒也是便携版
                        using (var cmd = progIdKey.CreateSubKey(@"shell\open\command"))
                            cmd.SetValue("", $"\"{exePath}\" --user-data-dir=\"{userDataDir}\" --single-argument %1");

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
                        // 🌟 修复：同样强力注入 --user-data-dir
                        using (var cmd = clientKey.CreateSubKey(@"shell\open\command"))
                            cmd.SetValue("", $"\"{exePath}\" --user-data-dir=\"{userDataDir}\"");

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
                            // 🌟 修复：XP 和 Win7 下的关联也必须指向便携版数据目录
                            cmdKey.SetValue("", $"\"{exePath}\" --user-data-dir=\"{userDataDir}\" --single-argument %1");
                        }
                    }
                }
            }
            catch { }
        }
        // 🌟 新增：微信小图标点击事件 (点击一键复制，极致用户体验)
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

        // 🌟 新增：Telegram 小图标点击事件 (继承了你完美的“盲配安全跳转”逻辑)
        private void Telegram_Click(object sender, RoutedEventArgs e)
        {
            bool chromeExists = Directory.Exists(portableDir) && File.Exists(chromeExe);
            bool isDefault = IsPortableChromeDefault();

            if (chromeExists && isDefault)
            {
                OpenUrl("https://t.me/YuC2027");
            }
        }
    }
}
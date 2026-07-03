using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChromeUpdater
{
    public partial class Form1 : Form
    {
        // 导入系统底层的画圆角函数
        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,     // 窗口左上角 X 坐标
            int nTopRect,      // 窗口左上角 Y 坐标
            int nRightRect,    // 窗口右下角 X 坐标
            int nBottomRect,   // 窗口右下角 Y 坐标
            int nWidthEllipse, // 圆角的宽度 (数值越大越圆)
            int nHeightEllipse // 圆角的高度
        );

        // 在窗口创建或大小改变时，自动将窗口裁剪为圆角
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 12, 12));
        }

        // 1. 定义便携版的核心路径
        private string portableDir = Path.Combine(Application.StartupPath, "Chrome_Portable");
        private string chromeExe;
        private string userDataDir;

        public Form1()
        {
            InitializeComponent();

            // 2. 初始化具体的子路径
            chromeExe = Path.Combine(portableDir, "Chrome", "chrome.exe");
            userDataDir = Path.Combine(portableDir, "UserData");
        }

        // 窗口启动时自动运行的初始化代码
        private void Form1_Load(object sender, EventArgs e)
        {
            // 3. 自动检测本机的系统架构（64位或32位），自动勾选并动态追加“(自动检测)”提示
            if (Environment.Is64BitOperatingSystem)
            {
                rdo64.Checked = true;
                rdo64.Text = "64位 (自动检测)";
            }
            else
            {
                rdo32.Checked = true;
                rdo32.Text = "32位 (自动检测)";
            }

            // 4. 让下拉菜单默认选中第一项“稳定版 (Stable)”
            if (cmbChannel.Items.Count > 0)
            {
                cmbChannel.SelectedIndex = 0;
            }

            // 5. 设置默认初始值 (将默认浏览器和快捷方式默认都设为“是”)
            rdoDefaultYes.Checked = true;
            rdoShortcutYes.Checked = true;

            // 6. 检测本地是否已经存在浏览器，并显示版本
            CheckLocalChromeVersion();

            // ================= 🌟 核心修复 1：调用美化按钮函数，让按钮变色生效 =================
            StyleButtons();

            // ================= 🌟 核心修复 2：用代码让版权 Label 变成手型光标 =================
            LabelCopyright.Cursor = Cursors.Hand;

            // ================= 🌟 核心修复 3：用代码直接绑定 LabelCopyright 的点击跳转事件 =================
            LabelCopyright.Click += LabelCopyright_Click;

            // 7. 动态读取你在项目属性里配置的版本号
            string rawVersion = Application.ProductVersion;
            string appVersion = rawVersion;
            if (rawVersion.EndsWith(".0.0"))
                appVersion = rawVersion.Substring(0, rawVersion.Length - 4); // 截掉末尾多余 of .0.0
            else if (rawVersion.EndsWith(".0"))
                appVersion = rawVersion.Substring(0, rawVersion.Length - 2);

            // 动态拼接并显示
            LabelCopyright.Text = $"版本: {appVersion}   技术支持: 微信: jiujiujiayi666   Telegram: @YuC2027";
        }

        // 🌟 强制扁平化高级按钮渲染
        private void StyleButtons()
        {
            // 1. 启动浏览器 按钮（绿色风格）
            btnLaunch.FlatStyle = FlatStyle.Flat;
            btnLaunch.FlatAppearance.BorderSize = 0; // 去掉边框
            btnLaunch.BackColor = Color.FromArgb(46, 204, 113); // 优雅绿 #2ECC71
            btnLaunch.ForeColor = Color.White;
            btnLaunch.UseVisualStyleBackColor = false; // 必须设为 false，防止被系统默认灰色主题覆盖！
            btnLaunch.Font = new Font("微软雅黑", 10.5F, FontStyle.Bold);

            // 2. 检查并更新 按钮（蓝色风格）
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.BackColor = Color.FromArgb(52, 152, 219); // 活力蓝 #3498DB
            btnUpdate.ForeColor = Color.White;
            btnUpdate.UseVisualStyleBackColor = false; // 必须设为 false，防止被系统默认灰色主题覆盖！
            btnUpdate.Font = new Font("微软雅黑", 10.5F, FontStyle.Bold);
        }

        // 检测本地版本的辅助函数
        private void CheckLocalChromeVersion()
        {
            if (Directory.Exists(portableDir) && File.Exists(chromeExe))
            {
                try
                {
                    // 读取本地已存在的 chrome.exe 的版本号属性
                    FileVersionInfo info = FileVersionInfo.GetVersionInfo(chromeExe);
                    string localVer = info.FileVersion;
                    lblStatus.Text = $"本地已存在 Chrome，版本号: {localVer}";
                }
                catch
                {
                    lblStatus.Text = "本地存在 Chrome，但读取版本号失败。";
                }
            }
            else
            {
                lblStatus.Text = "本地未检测到 Chrome 便携版，请点击【检查并更新】开始下载！";
            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            // 1. 检查本地 chrome.exe 是否真的存在
            if (File.Exists(chromeExe))
            {
                lblStatus.Text = "正在启动 Google Chrome 便携增强版...";

                // 2. 构造便携启动参数（强制将用户配置、缓存数据写在同级目录下的 UserData 文件夹里，实现彻底隔离）
                string arguments = $"--user-data-dir=\"{userDataDir}\" --no-first-run";

                try
                {
                    // 3. 启动 Chrome 进程
                    Process.Start(chromeExe, arguments);

                    // 4. 启动成功后，更新器作为引导程序功成身退，自动关闭退出
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"启动浏览器失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // 5. 如果本地没有，友好地弹窗提示
                MessageBox.Show("本地尚未检测到 Chrome 便携版，请先点击右侧的【检查并更新】按钮进行全自动下载和安装！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 🌟 升级后的“检查并更新”按钮逻辑
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            btnUpdate.Enabled = false; // 暂时禁用，防止重复点击
            lblStatus.Text = "正在直连服务器检查最新版本号...";

            bool is64Bit = rdo64.Checked;
            int channelIndex = cmbChannel.SelectedIndex;
            bool isDefaultBrowser = rdoDefaultYes.Checked;
            bool isShortcut = rdoShortcutYes.Checked;

            string os = GetOsType();
            string onlineVer = "";

            // 1. 在后台新线程中静默查询最新版本号，主界面绝对不卡死
            await Task.Run(() =>
            {
                onlineVer = FetchLatestVersionFromAli(os, channelIndex);
            });

            if (string.IsNullOrEmpty(onlineVer))
            {
                lblStatus.Text = "网络连接超时，获取最新版本号失败！";
                btnUpdate.Enabled = true;
                return;
            }

            // 2. 获取本地 Chrome 的实际版本
            string localVer = "";
            if (File.Exists(chromeExe))
            {
                try
                {
                    localVer = FileVersionInfo.GetVersionInfo(chromeExe).FileVersion;
                }
                catch { }
            }

            // 3. 🌟 核心修复：采用高精度数学版本号比对算法！
            // 如果 [本地版本] 大于或等于 [云端解析版本]，则判定为已经是最新版并直接拦截！
            if (CompareVersions(localVer, onlineVer) >= 0)
            {
                MessageBox.Show("已经是最新版了亲。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                lblStatus.Text = $"当前已是最新版本 ({localVer})";
                btnUpdate.Enabled = true;
                return; // 退出，跳过后面所有的动作！
            }

            // 4. 如果版本落后，正常拉起进度弹出层
            lblStatus.Text = "发现新版本，正在准备部署...";
            ProgressForm pf = new ProgressForm(is64Bit, channelIndex, isDefaultBrowser, isShortcut);
            pf.Size = this.Size;
            pf.StartPosition = FormStartPosition.Manual;
            pf.Location = this.Location;

            pf.ShowDialog(this);

            CheckLocalChromeVersion(); // 更新完后刷新版本显示
            btnUpdate.Enabled = true;
        }

        // 🌟 辅助方法：静默抓取阿里源版本号
        private string FetchLatestVersionFromAli(string osType, int channelIndex)
        {
            if (osType == "XP") return "49.0.2623.112";
            if (osType == "7/8") return "109.0.5414.120";

            try
            {
                using (var client = new WebClient())
                {
                    client.Proxy = null;
                    client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
                    string json = client.DownloadString("https://registry.npmmirror.com/-/binary/chrome-for-testing/last-known-good-versions.json");

                    string channelName = "Stable";
                    if (channelIndex == 1) channelName = "Beta";
                    else if (channelIndex == 2) channelName = "Dev";
                    else if (channelIndex == 3) channelName = "Canary";

                    string pattern = @"""" + channelName + @"""\s*:\s*\{([^}]+)\}";
                    Match m = Regex.Match(json, pattern);
                    if (m.Success)
                    {
                        string block = m.Groups[1].Value;
                        Match vMatch = Regex.Match(block, @"""version""\s*:\s*""([^""]+)""");
                        if (vMatch.Success) return vMatch.Groups[1].Value;
                    }
                }
            }
            catch { }
            return "";
        }

        // 🌟 标准 LabelCopyright 的点击事件，完美支持浏览器跳转
        private void LabelCopyright_Click(object sender, EventArgs e)
        {
            try
            {
                // 直接拉起浏览器跳转到 Telegram 链接
                Process.Start("https://t.me/YuC2027");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开链接: {ex.Message}");
            }
        }

        // 🌟 核心：高精度版本号大小比对算法 (支持 150.0.7871.47 这样复杂的4段式版本号)
        private int CompareVersions(string versionA, string versionB)
        {
            try
            {
                Version vA = new Version(versionA);
                Version vB = new Version(versionB);
                return vA.CompareTo(vB); // vA > vB 返回 1，vA == vB 返回 0，vA < vB 返回 -1
            }
            catch
            {
                // 如果解析失败，采用退化字符串比对
                return string.Compare(versionA, versionB, StringComparison.OrdinalIgnoreCase);
            }
        }

        // 🌟 读取注册表底层硬件值检测系统，防止主窗口判断出错
        private string GetOsType()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        var major = key.GetValue("CurrentMajorVersionNumber");
                        if (major != null)
                        {
                            int majorNum = int.Parse(major.ToString());
                            if (majorNum >= 10) return "10/11"; // 100% 锁定为 Win10 或 Win11
                        }
                    }
                }
            }
            catch
            {
                // 异常时采用备用兜底逻辑
            }

            Version vs = Environment.OSVersion.Version;
            if (vs.Major <= 5) return "XP";
            if (vs.Major == 6 && vs.Minor <= 3) return "7/8";
            return "10/11";
        }
    }
}
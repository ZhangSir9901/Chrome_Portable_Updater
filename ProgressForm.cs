using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Text.RegularExpressions;
using System.Drawing;

namespace ChromeUpdater
{
    public partial class ProgressForm : Form
    {
        // 🌟 诗词数据与彩蛋指针（合并定义，移除重复，保证零警告）
        private string[] poemLines = {
            "巴女浅醉黄鹤楼，",
            "江风吹皱美人绸。",
            "此别经年何时了，",
            "云锁巫山夜未犹。",
            "",
            ""
        };
        private float poemY = 0;
        private DateTime lastClickTime = DateTime.MinValue;

        // 🌟 呼吸闪烁定时器与界面参数
        private Timer timerBlink;
        private bool is64Bit;
        private int channelIndex;
        private bool isDefaultBrowser;
        private bool isShortcut;

        private string portableDir;
        private string chromeExe;
        private string userDataDir;

        public ProgressForm(bool is64Bit, int channelIndex, bool isDefaultBrowser, bool isShortcut)
        {
            InitializeComponent();
            this.is64Bit = is64Bit;
            this.channelIndex = channelIndex;
            this.isDefaultBrowser = isDefaultBrowser;
            this.isShortcut = isShortcut;

            portableDir = Path.Combine(Application.StartupPath, "Chrome_Portable");
            chromeExe = Path.Combine(portableDir, "Chrome", "chrome.exe");
            userDataDir = Path.Combine(portableDir, "UserData");
        }

        // 创建带有安全标头、且彻底绕过一切系统代理干扰的直连下载器
        private WebClient CreateSafeWebClient()
        {
            WebClient client = new WebClient();
            client.Proxy = null;
            client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
            return client;
        }

        private async void ProgressForm_Load(object sender, EventArgs e)
        {
            // 🌟 核心修复：纯代码强制让提示文字在任何分辨率下都完美横向居中，免去你手动去属性窗口调
            lblStatus.AutoSize = false;
            lblStatus.Width = this.Width;
            lblStatus.Left = 0;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;

            // 🌟 呼吸闪烁定时器初始化
            timerBlink = new Timer { Interval = 500 };
            timerBlink.Tick += (s, ev) => { lblStatus.Visible = !lblStatus.Visible; };
            timerBlink.Start();

            progressBar1.Click += ProgressBar1_Click;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                Directory.CreateDirectory(portableDir);

                string os = GetOsType();

                lblStatus.Text = "正在直连国内镜像源解析 Google 官方下载地址...";
                var (version, downloadUrl) = GetChromeDownloadInfo(os);

                // 3. 容灾机制获取 7zr.exe
                string szPath = Path.Combine(portableDir, "7zr.exe");
                if (!File.Exists(szPath))
                {
                    var res7z = Properties.Resources.ResourceManager.GetObject("_7zr") as byte[];
                    if (res7z != null)
                    {
                        lblStatus.Text = "正在从本地资源包中释放解压核心...";
                        File.WriteAllBytes(szPath, res7z);
                    }
                    else
                    {
                        await Download7zWithFallback(szPath);
                    }
                }

                // 4. 下载 Chrome 离线包
                string installerPath = Path.Combine(portableDir, "chrome_installer.exe");
                lblStatus.Text = $"正在官方直连下载 Chrome {version} 离线包...";

                using (var client = CreateSafeWebClient())
                {
                    client.DownloadProgressChanged += (s, ev) => { progressBar1.Value = ev.ProgressPercentage; };
                    await client.DownloadFileTaskAsync(new Uri(downloadUrl), installerPath);
                    progressBar1.Value = 0;
                }

                // 5. 🌟 终极套娃穿透算法：全自动剥离 4 层嵌套压缩包
                lblStatus.Text = "正在执行双重静默解压缩，请勿关闭程序...";
                progressBar1.Style = ProgressBarStyle.Marquee;

                await Task.Run(() =>
                {
                    // 定义 4 个临时套娃解压目录
                    string temp1 = Path.Combine(portableDir, "Temp1");
                    string temp2 = Path.Combine(portableDir, "Temp2");
                    string temp3 = Path.Combine(portableDir, "Temp3");
                    string temp4 = Path.Combine(portableDir, "Temp4");

                    if (Directory.Exists(temp1)) Directory.Delete(temp1, true);
                    if (Directory.Exists(temp2)) Directory.Delete(temp2, true);
                    if (Directory.Exists(temp3)) Directory.Delete(temp3, true);
                    if (Directory.Exists(temp4)) Directory.Delete(temp4, true);

                    // ================= 第 1 层剥离 =================
                    RunProcess(szPath, $"x \"{installerPath}\" -o\"{temp1}\" -y");

                    // ================= 第 2 层剥离 =================
                    string updater7z = Path.Combine(temp1, "updater.7z");
                    if (File.Exists(updater7z))
                    {
                        RunProcess(szPath, $"x \"{updater7z}\" -o\"{temp2}\" -y");
                    }
                    else
                    {
                        temp2 = temp1;
                    }

                    // ================= 第 3 层剥离 =================
                    string[] innerInstallers = Directory.GetFiles(temp2, "*_chrome_installer.exe", SearchOption.AllDirectories);
                    if (innerInstallers.Length > 0)
                    {
                        string realInstaller = innerInstallers[0];
                        RunProcess(szPath, $"x \"{realInstaller}\" -o\"{temp3}\" -y");
                    }
                    else
                    {
                        temp3 = temp2;
                    }

                    // ================= 第 4 层剥离 =================
                    string[] chrome7zs = Directory.GetFiles(temp3, "chrome.7z", SearchOption.AllDirectories);
                    if (chrome7zs.Length > 0)
                    {
                        string chrome7z = chrome7zs[0];
                        RunProcess(szPath, $"x \"{chrome7z}\" -o\"{temp4}\" -y");
                    }
                    else
                    {
                        temp4 = temp3;
                    }

                    // ================= 收尾：定位并移动运行主程序 =================
                    string[] chromeBinDirs = Directory.GetDirectories(temp4, "Chrome-bin", SearchOption.AllDirectories);
                    if (chromeBinDirs.Length > 0)
                    {
                        string sourceDir = chromeBinDirs[0];
                        string targetDir = Path.Combine(portableDir, "Chrome");

                        if (Directory.Exists(targetDir))
                        {
                            Directory.Delete(targetDir, true);
                        }
                        Directory.Move(sourceDir, targetDir);
                    }
                    else
                    {
                        throw new Exception("解压已成功，但在提取的目录中未找到核心的 'Chrome-bin' 文件夹！");
                    }

                    if (Directory.Exists(temp1)) Directory.Delete(temp1, true);
                    if (Directory.Exists(temp2)) Directory.Delete(temp2, true);
                    if (Directory.Exists(temp3)) Directory.Delete(temp3, true);
                    if (Directory.Exists(temp4)) Directory.Delete(temp4, true);
                    if (File.Exists(installerPath)) File.Delete(installerPath);
                });

                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.Value = 100;

                if (isShortcut)
                {
                    lblStatus.Text = "正在创建桌面快捷方式...";
                    CreateDesktopShortcut();
                }

                if (isDefaultBrowser)
                {
                    lblStatus.Text = "正在关联默认浏览器参数...";
                    RunProcess(chromeExe, "--make-default-browser");
                }

                lblStatus.Text = "全自动更新部署完成！";

                // 🌟 成功提示音
                System.Media.SystemSounds.Asterisk.Play();

                await Task.Delay(2000);
                timerBlink.Stop(); // 停止闪烁
                lblStatus.Visible = true;
                this.Close();
            }
            catch (Exception ex)
            {
                progressBar1.Style = ProgressBarStyle.Blocks;
                if (timerBlink != null) timerBlink.Stop();
                lblStatus.Visible = true;
                MessageBox.Show($"更新过程中发生网络或系统错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        // 采用你提供的三个真实有效的 7zr 下载源
        private async Task Download7zWithFallback(string savePath)
        {
            string[] urls = {
                "https://www.7-zip.org/a/7zr.exe",                                                         // 源 1：官方直连
                "https://github.com/ip7z/7zip/releases/download/26.02/7zr.exe",                           // 源 2：GitHub 官方源
                "https://cytranet-dal.dl.sourceforge.net/project/sevenzip/7-Zip/25.01/7zr.exe"            // 源 3：SourceForge 镜像源
            };

            bool success = false;
            foreach (string url in urls)
            {
                try
                {
                    string domain = new Uri(url).Host;
                    lblStatus.Text = $"正在从 [{domain}] 获取解压引擎...";

                    using (var client = CreateSafeWebClient())
                    {
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            progressBar1.Value = e.ProgressPercentage;
                        };
                        await client.DownloadFileTaskAsync(new Uri(url), savePath);
                        progressBar1.Value = 0;
                        success = true;
                        break;
                    }
                }
                catch
                {
                    // 失败则顺延尝试下一个源
                }
            }

            if (!success)
            {
                throw new Exception("无法下载解压引擎 7zr.exe，所有备份源均不可用！");
            }
        }

        private (string Version, string Url) GetChromeDownloadInfo(string osType)
        {
            if (osType == "XP")
            {
                return ("49.0.2623.112", "https://dl.google.com/release2/h8vnfiy7pvn3lxy9ehfsaxlrnnukgff8jnghr1/49.0.2623.112_chrome_installer.exe");
            }
            if (osType == "7/8")
            {
                string win7Url = is64Bit
                    ? "https://dl.google.com/release2/chrome/czao2hrvpk5wgqrkz4kks5r734_109.0.5414.120/109.0.5414.120_chrome_installer.exe"
                    : "https://dl.google.com/release2/chrome/acihtkcueyye3ymoj2afvv7ulzxa_109.0.5414.120/109.0.5414.120_chrome_installer.exe";
                return ("109.0.5414.120", win7Url);
            }

            string version = "126.0.0.0";
            try
            {
                using (var client = CreateSafeWebClient())
                {
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
                        if (vMatch.Success)
                        {
                            version = vMatch.Groups[1].Value;
                        }
                    }
                }
            }
            catch
            {
                version = "126.0.6478.127";
            }

            string ap = "";
            string filename = "";

            if (channelIndex == 0)
            {
                ap = is64Bit ? "x64-stable-statsdef_1" : "stable-arch_x86-statsdef_1";
                filename = is64Bit ? "ChromeStandaloneSetup64.exe" : "ChromeStandaloneSetup.exe";
            }
            else if (channelIndex == 1)
            {
                ap = is64Bit ? "x64-beta-statsdef_1" : "beta-arch_x86-statsdef_1";
                filename = is64Bit ? "ChromeStandaloneSetup64.exe" : "ChromeStandaloneSetup.exe";
            }
            else if (channelIndex == 2)
            {
                ap = is64Bit ? "x64-dev-statsdef_1" : "dev-arch_x86-statsdef_1";
                filename = is64Bit ? "ChromeStandaloneSetup64.exe" : "ChromeStandaloneSetup.exe";
            }
            else if (channelIndex == 3)
            {
                string canaryAppId = "%7B4ea16ac7-fd5a-47c3-875b-dbf8a2000d21%7D";
                ap = is64Bit ? "x64-canary-statsdef_1" : "canary-arch_x86-statsdef_1";
                return (version, $"https://dl.google.com/tag/s/appguid%3D{canaryAppId}%26iid%3D%7B00000000-0000-0000-0000-000000000000%7D%26lang%3Dzh-CN%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%2520Canary%26needsadmin%3Dprefers%26ap%3D{ap}%26installdataindex%3Dempty/update2/installers/ChromeCanarySetup64.exe");
            }

            string defaultAppId = "%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D";
            return (version, $"https://dl.google.com/tag/s/appguid%3D{defaultAppId}%26iid%3D%7B00000000-0000-0000-0000-000000000000%7D%26lang%3Dzh-CN%26browser%3D4%26usagestats%3D0%26appname%3DGoogle%2520Chrome%2520NeedsAdmin%3Dprefers%26ap%3D{ap}%26installdataindex%3Dempty/update2/installers/{filename}");
        }

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
                            if (majorNum >= 10) return "10/11";
                        }
                    }
                }
            }
            catch
            {
            }

            Version vs = Environment.OSVersion.Version;
            if (vs.Major <= 5) return "XP";
            if (vs.Major == 6 && vs.Minor <= 3) return "7/8";
            return "10/11";
        }

        private void RunProcess(string exe, string args)
        {
            Process p = new Process();
            p.StartInfo.FileName = exe;
            p.StartInfo.Arguments = args;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();
        }

        private void CreateDesktopShortcut()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = Path.Combine(desktop, "Google Chrome (便携增强版).lnk");
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            dynamic shell = Activator.CreateInstance(t);
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = chromeExe;
            shortcut.Arguments = $"--user-data-dir=\"{userDataDir}\" --no-first-run";
            shortcut.WorkingDirectory = Path.GetDirectoryName(chromeExe);
            shortcut.Save();
        }

        // 🌟 核心点击事件：用时间差算法手动模拟出“双击”，完美解决 Windows 进度条吞双击的缺陷！
        private void ProgressBar1_Click(object sender, EventArgs e)
        {
            TimeSpan span = DateTime.Now - lastClickTime;
            if (span.TotalMilliseconds < SystemInformation.DoubleClickTime)
            {
                TogglePoem(); // 触发彩蛋
            }
            lastClickTime = DateTime.Now;
        }

        // 🌟 开启或关闭彩蛋
        private void TogglePoem()
        {
            lblPoem.Visible = !lblPoem.Visible;

            if (lblPoem.Visible)
            {
                lblPoem.Text = "";
                poemY = lblPoem.Height;

                lblPoem.Paint += LblPoem_Paint;

                timerPoem.Tick += TimerPoem_Tick;
                timerPoem.Interval = 30; // 30毫秒一帧
                timerPoem.Start();
            }
            else
            {
                timerPoem.Stop();
                timerPoem.Tick -= TimerPoem_Tick;
                lblPoem.Paint -= LblPoem_Paint;
                lblPoem.Text = "";
            }
        }

        private void TimerPoem_Tick(object sender, EventArgs e)
        {
            poemY -= 1.0f;

            Font f = lblPoem.Font;
            float lineHeight = f.Height + 10;
            float totalHeight = lineHeight * poemLines.Length;

            if (poemY <= -totalHeight)
            {
                poemY = lblPoem.Height;
            }

            lblPoem.Invalidate();
        }

        private void LblPoem_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Font f = lblPoem.Font;
            Brush b = new SolidBrush(lblPoem.ForeColor);
            float lineHeight = f.Height + 10;

            float currentY = poemY;

            for (int i = 0; i < poemLines.Length; i++)
            {
                if (currentY > -lineHeight && currentY < lblPoem.Height)
                {
                    float stringWidth = e.Graphics.MeasureString(poemLines[i], f).Width;
                    float x = (lblPoem.Width - stringWidth) / 2;

                    e.Graphics.DrawString(poemLines[i], f, b, x, currentY);
                }
                currentY += lineHeight;
            }
        }
    }
}
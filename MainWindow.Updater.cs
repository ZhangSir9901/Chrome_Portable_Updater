using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChromeUpdaterWPF
{
    public partial class MainWindow
    {
        private void CleanupOldUpdater()
        {
            try { string oldExe = Process.GetCurrentProcess().MainModule.FileName + ".old"; if (File.Exists(oldExe)) File.Delete(oldExe); } catch { }
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
                    foreach (string url in jsonUrls) { try { json = await client.DownloadStringTaskAsync(url); break; } catch { continue; } }
                    if (string.IsNullOrEmpty(json)) return;

                    Match mVer = Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
                    Match mUrl = Regex.Match(json, @"""url""\s*:\s*""([^""]+)""");

                    if (mVer.Success && mUrl.Success)
                    {
                        if (string.Compare(mVer.Groups[1].Value, APP_VERSION, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            lblAppVersion.Visibility = Visibility.Collapsed;
                            btnAppUpdate.Visibility = Visibility.Visible;
                            btnAppUpdate.Content = $"🚀 发现新版本 {mVer.Groups[1].Value}，点击立即自动升级！";
                            btnAppUpdate.Tag = mUrl.Groups[1].Value;
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
            progressBar.Value = 0; StartBlinking();

            try
            {
                string currentExe = Process.GetCurrentProcess().MainModule.FileName;
                string newExe = currentExe + ".new", oldExe = currentExe + ".old";
                using (var client = CreateSafeWebClient())
                {
                    client.DownloadProgressChanged += (s, ev) => { progressBar.Value = ev.ProgressPercentage; };
                    await client.DownloadFileTaskAsync(new Uri(downloadUrl + (downloadUrl.Contains("?") ? "&" : "?") + "t=" + DateTime.Now.Ticks), newExe);
                }

                lblOverlayStatus.Text = "下载完成，正在执行无感影子热替换...";
                progressBar.IsIndeterminate = true; await Task.Delay(800);

                if (File.Exists(oldExe)) File.Delete(oldExe);
                File.Move(currentExe, oldExe); File.Move(newExe, currentExe);
                Process.Start(currentExe); Application.Current.Shutdown();
            }
            catch (Exception ex) { MessageBox.Show($"自升级失败: {ex.Message}\n请检查网络。", "热更新错误", MessageBoxButton.OK, MessageBoxImage.Error); StopBlinking(); OverlayPanel.Visibility = Visibility.Collapsed; }
        }

        private async Task CheckAndDisplayVersionAsync()
        {
            string localVer = "";
            if (Directory.Exists(portableDir) && File.Exists(chromeExe)) { try { localVer = FileVersionInfo.GetVersionInfo(chromeExe).FileVersion; ManageShortcuts(); } catch { } }

            if (string.IsNullOrEmpty(localVer))
            {
                lblStatus.Text = "本地还没有Chrome浏览器，请点击【检查更新】来获取！";
                lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)); UpdateBorderStyles(false, false, false); return;
            }

            UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop());
            lblStatus.Text = $"本地已存在版本为 {localVer}，正在比对云端最新版本...";
            lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94));

            string os = GetOsType(); int channelIndex = cmbChannel.SelectedIndex; bool is64 = rdo64.IsChecked == true;
            string onlineVer = await Task.Run(() => FetchLatestVersion(os, channelIndex, is64));

            if (string.IsNullOrEmpty(onlineVer)) { lblStatus.Text = $"本地Chrome版本为 {localVer} (云端检测失败)"; return; }

            if (CompareVersions(localVer, onlineVer) >= 0) { lblStatus.Text = $"本地Chrome版本为 {localVer}，已经是最新版了！"; lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)); }
            else { lblStatus.Text = $"本地浏览器版本为 {localVer}，最新版 {onlineVer}，请【检查更新】！"; lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(230, 126, 34)); }
        }

        public async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            btnUpdate.IsEnabled = btnLaunch.IsEnabled = false;
            string os = GetOsType(); int channelIndex = cmbChannel.SelectedIndex; bool is64 = rdo64.IsChecked == true;
            string onlineVer = await Task.Run(() => FetchLatestVersion(os, channelIndex, is64));

            if (string.IsNullOrEmpty(onlineVer)) { MessageBox.Show("网络连接超时！"); btnUpdate.IsEnabled = btnLaunch.IsEnabled = true; return; }
            string localVer = File.Exists(chromeExe) ? FileVersionInfo.GetVersionInfo(chromeExe).FileVersion : "";
            if (CompareVersions(localVer, onlineVer) >= 0) { MessageBox.Show("已经是最新版了亲。"); await CheckAndDisplayVersionAsync(); btnUpdate.IsEnabled = btnLaunch.IsEnabled = true; return; }

            // 🌟 强杀运行中的 Chrome 以防解除占用失败
            Process[] runningChromes = Process.GetProcessesByName("chrome");
            if (runningChromes.Length > 0)
            {
                if (MessageBox.Show("检测到 Chrome 浏览器正在后台运行。为了顺利覆盖升级文件，必须先关闭浏览器。\n\n是否允许升级器现在为您自动关闭浏览器并继续升级？\n(注：由于已开启启动恢复，您打开的网页不会丢失)", "浏览器运行中", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    foreach (var p in runningChromes) { try { p.Kill(); p.WaitForExit(3000); } catch { } }
                }
                else { btnUpdate.IsEnabled = btnLaunch.IsEnabled = true; return; }
            }

            OverlayPanel.Visibility = Visibility.Visible; StartBlinking();
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; Directory.CreateDirectory(portableDir);
                var (_, downloadUrl) = GetChromeDownloadInfo(os, onlineVer, is64, channelIndex);
                string szPath = Path.Combine(portableDir, "7zr.exe");
                if (!File.Exists(szPath)) await Download7zWithFallback(szPath);

                string installerPath = Path.Combine(portableDir, "chrome_installer.exe");
                lblOverlayStatus.Text = $"正在直连下载 Chrome {onlineVer}...";

                using (var client = CreateSafeWebClient()) { client.DownloadProgressChanged += (s, ev) => { progressBar.Value = ev.ProgressPercentage; }; await client.DownloadFileTaskAsync(new Uri(downloadUrl), installerPath); progressBar.Value = 0; }

                lblOverlayStatus.Text = "正在执行双重静静解压缩...";
                progressBar.IsIndeterminate = true; await Task.Run(() => PerformExtraction(szPath, installerPath)); progressBar.IsIndeterminate = false; progressBar.Value = 100;

                ApplyChromeTuningConfig(); ManageShortcuts();
                if (rdoDefaultYes.IsChecked == true) RunProcess(chromeExe, "--make-default-browser");

                System.Media.SystemSounds.Asterisk.Play(); lblOverlayStatus.Text = $"Chrome浏览器已成功升级为最新版 {onlineVer}！"; await Task.Delay(7000);
            }
            catch (Exception ex) { MessageBox.Show($"更新发生错误: {ex.Message}"); }
            finally { StopBlinking(); OverlayPanel.Visibility = Visibility.Collapsed; await CheckAndDisplayVersionAsync(); UpdateAiButtonStatus(); btnUpdate.IsEnabled = btnLaunch.IsEnabled = true; }
        }

        private void PerformExtraction(string szPath, string installerPath)
        {
            string temp1 = Path.Combine(portableDir, "Temp1"), temp2 = Path.Combine(portableDir, "Temp2"), temp3 = Path.Combine(portableDir, "Temp3"), temp4 = Path.Combine(portableDir, "Temp4");
            if (Directory.Exists(temp1)) Directory.Delete(temp1, true); if (Directory.Exists(temp2)) Directory.Delete(temp2, true); if (Directory.Exists(temp3)) Directory.Delete(temp3, true); if (Directory.Exists(temp4)) Directory.Delete(temp4, true);

            RunProcess(szPath, $"x \"{installerPath}\" -o\"{temp1}\" -y");
            string updater7z = Path.Combine(temp1, "updater.7z"); if (File.Exists(updater7z)) RunProcess(szPath, $"x \"{updater7z}\" -o\"{temp2}\" -y"); else temp2 = temp1;
            string[] innerInstallers = Directory.GetFiles(temp2, "*_chrome_installer.exe", SearchOption.AllDirectories); if (innerInstallers.Length > 0) RunProcess(szPath, $"x \"{innerInstallers[0]}\" -o\"{temp3}\" -y"); else temp3 = temp2;
            string[] chrome7zs = Directory.GetFiles(temp3, "chrome.7z", SearchOption.AllDirectories); if (chrome7zs.Length > 0) RunProcess(szPath, $"x \"{chrome7zs[0]}\" -o\"{temp4}\" -y"); else temp4 = temp3;

            string[] chromeBinDirs = Directory.GetDirectories(temp4, "Chrome-bin", SearchOption.AllDirectories);
            if (chromeBinDirs.Length > 0) { string targetDir = Path.Combine(portableDir, "Chrome"); if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true); Directory.Move(chromeBinDirs[0], targetDir); }

            if (!Directory.Exists(userDataDir)) Directory.CreateDirectory(userDataDir);
            if (Directory.Exists(temp1)) Directory.Delete(temp1, true); if (Directory.Exists(temp2)) Directory.Delete(temp2, true); if (Directory.Exists(temp3)) Directory.Delete(temp3, true); if (Directory.Exists(temp4)) Directory.Delete(temp4, true); if (File.Exists(installerPath)) File.Delete(installerPath);
        }

        private WebClient CreateSafeWebClient()
        {
            WebClient c = new WebClient { Proxy = null };
            c.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
            c.Headers[HttpRequestHeader.Accept] = "*/*"; return c;
        }

        private async Task Download7zWithFallback(string savePath)
        {
            string[] urls = { "https://gitee.com/zhoupan/AI-OCR/raw/master/7zr.exe", "https://www.7-zip.org/a/7zr.exe", "https://cytranet-dal.dl.sourceforge.net/project/sevenzip/7-Zip/25.01/7zr.exe" };
            foreach (string u in urls) { try { using (var c = CreateSafeWebClient()) { await c.DownloadFileTaskAsync(new Uri(u), savePath); return; } } catch { } }
            throw new Exception("7zr.exe 下载失败");
        }

        private string FetchLatestVersion(string osType, int channelIndex, bool is64)
        {
            if (osType == "XP") return "49.0.2623.112"; if (osType == "7/8") return "109.0.5414.120";
            string ap = is64 ? (channelIndex == 0 ? "x64-stable-statsdef_1" : channelIndex == 1 ? "x64-beta-statsdef_1" : channelIndex == 2 ? "x64-dev-statsdef_1" : "x64-canary-statsdef_1") : (channelIndex == 0 ? "stable-arch_x86-statsdef_1" : channelIndex == 1 ? "beta-arch_x86-statsdef_1" : channelIndex == 2 ? "dev-arch_x86-statsdef_1" : "canary-arch_x86-statsdef_1");
            string appID = channelIndex == 3 ? "{4ea16ac7-fd5a-47c3-875b-dbf8a2000d21}" : "{8A69D345-D564-463C-AFF1-A69D9E530F96}"; string arch = is64 ? "x64" : "x86";

            string xmlData = $@"<?xml version=""1.0"" encoding=""UTF-8""?><request protocol=""3.0"" updater=""Omaha"" updaterversion=""1.3.36.352"" shell_version=""1.3.36.352"" ismachine=""0"" sessionid=""{{00000000-0000-0000-0000-000000000000}}"" installsource=""ondemandcheckforupdate"" requestid=""{{00000000-0000-0000-0000-000000000000}}"" dedup=""cr""><os platform=""win"" version=""10.0"" sp="""" arch=""{arch}""/><app appid=""{appID}"" version=""0.0.0.0"" nextversion="""" ap=""{ap}"" lang=""zh-CN"" brand=""GGLS"" client="""" installage=""-1""><updatecheck/></app></request>";
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(xmlData);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://update.googleapis.com/service/update2"); req.Method = "POST"; req.ContentType = "application/x-www-form-urlencoded"; req.ContentLength = postBytes.Length; req.Timeout = 8000;
                using (Stream reqStream = req.GetRequestStream()) { reqStream.Write(postBytes, 0, postBytes.Length); }
                using (HttpWebResponse res = (HttpWebResponse)req.GetResponse()) using (StreamReader sr = new StreamReader(res.GetResponseStream())) { Match m = Regex.Match(sr.ReadToEnd(), @"<manifest\s+version=""([^""]+)"""); if (m.Success) return m.Groups[1].Value; }
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

        private string GetOsType() { try { using (var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion")) { if (k != null && k.GetValue("CurrentMajorVersionNumber") != null && int.Parse(k.GetValue("CurrentMajorVersionNumber").ToString()) >= 10) return "10/11"; } } catch { } return Environment.OSVersion.Version.Major <= 5 ? "XP" : "7/8"; }
        private int CompareVersions(string vA, string vB) { try { return new Version(vA).CompareTo(new Version(vB)); } catch { return string.Compare(vA, vB, StringComparison.OrdinalIgnoreCase); } }
        private void RunProcess(string exe, string args) { Process p = new Process { StartInfo = { FileName = exe, Arguments = args, CreateNoWindow = true, UseShellExecute = false } }; p.Start(); p.WaitForExit(); }

        private bool ShortcutsExistOnDesktop() { string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); return File.Exists(Path.Combine(desktop, "Chrome 便携版.lnk")); }
        private bool IsPortableChromeDefault() { try { string normalizedExe = GetNormalizedPath(chromeExe).ToLower().Replace("\\", "/"); using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice")) { if (key != null && key.GetValue("ProgId") != null) { string progId = key.GetValue("ProgId").ToString(); using (var cmdKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command") ?? Microsoft.Win32.Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command")) { if (cmdKey != null && cmdKey.GetValue("") != null && cmdKey.GetValue("").ToString().ToLower().Replace("\\", "/").Contains(normalizedExe)) return true; } return false; } } using (var legacyKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes\https\shell\open\command")) { if (legacyKey != null && legacyKey.GetValue("") != null && legacyKey.GetValue("").ToString().ToLower().Replace("\\", "/").Contains(normalizedExe)) return true; } } catch { } return false; }

        private void ManageShortcuts()
        {
            try { string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); string shortcutPortable = Path.Combine(desktop, "Chrome 便携版.lnk"); string shortcutNormal = Path.Combine(desktop, "Chrome 浏览器.lnk"); if (File.Exists(shortcutNormal)) { try { File.Delete(shortcutNormal); } catch { } } if (rdoShortcutYes.IsChecked == true) { Type t = Type.GetTypeFromProgID("WScript.Shell"); dynamic shell = Activator.CreateInstance(t); string exePath = GetNormalizedPath(chromeExe); string dataPath = GetNormalizedPath(userDataDir); dynamic sPortable = shell.CreateShortcut(shortcutPortable); sPortable.TargetPath = exePath; sPortable.Arguments = $"--user-data-dir=\"{dataPath}\""; sPortable.WorkingDirectory = Path.GetDirectoryName(exePath); sPortable.Save(); } else { if (File.Exists(shortcutPortable)) File.Delete(shortcutPortable); } } catch (Exception ex) { Console.WriteLine($"更新快捷方式失败: {ex.Message}"); }
        }

        public void RdoShortcut_Click(object sender, RoutedEventArgs e) { if (Directory.Exists(portableDir) && File.Exists(chromeExe)) { ManageShortcuts(); UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop()); } }

        public async void RdoDefault_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(portableDir) || !File.Exists(chromeExe)) return;
            if (rdoDefaultNo.IsChecked == true) { UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop()); return; }
            if (rdoDefaultYes.IsChecked == true)
            {
                borderDefault.IsEnabled = false; string originalText = rdoDefaultYes.Content.ToString();
                await Task.Run(() => RegisterPortableChrome());
                Version osVer = Environment.OSVersion.Version;
                if (osVer.Major >= 10 || (osVer.Major == 6 && osVer.Minor >= 2))
                {
                    rdoDefaultYes.Content = "请在弹出的系统设置中确认...";
                    await Task.Run(() => { try { if (osVer.Major >= 10) Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true }); else Process.Start(new ProcessStartInfo("control.exe", "/name Microsoft.DefaultPrograms /page pageDefaultProgram") { UseShellExecute = true }); } catch { } });
                    bool isSetSuccess = false; for (int i = 0; i < 30; i++) { await Task.Delay(1000); if (IsPortableChromeDefault()) { isSetSuccess = true; break; } }
                    if (isSetSuccess) { lblStatus.Text = "太棒了！已成功将便携版设为默认浏览器。"; lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)); } else { lblStatus.Text = "等待超时或已取消，未检测到默认浏览器变更。"; lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60)); }
                }
                else { rdoDefaultYes.Content = "正在极速配置..."; await Task.Run(() => ForceSetLegacyDefault()); await Task.Delay(500); lblStatus.Text = "配置成功！已接管 Win7/XP 默认浏览器关联。"; lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)); }
                rdoDefaultYes.Content = originalText; borderDefault.IsEnabled = true; UpdateBorderStyles(true, IsPortableChromeDefault(), ShortcutsExistOnDesktop());
            }
        }

        private void RegisterPortableChrome()
        {
            string exePath = GetNormalizedPath(chromeExe); string iconPath = $"{exePath},0"; string progId = "ChromePortableHTML"; string clientName = "ChromePortable"; string appName = "Google Chrome 便携版"; string appDesc = "智能且快速的便携式 Web 浏览器";
            string configJson = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}"; ApplyEnterprisePolicies(configJson);
            try { using (var key = Microsoft.Win32.Registry.CurrentUser) { string[] garbageKeys = { @"Software\Classes\ChromeHTML.Portable", @"Software\Clients\StartMenuInternet\ChromePortable", @"Software\Classes\ChromePortableHTML" }; foreach (var gk in garbageKeys) { try { key.DeleteSubKeyTree(gk, false); } catch { } } bool isHijacked = false; try { using (var chk = key.OpenSubKey(@"Software\Classes\ChromeHTML\shell\open\command")) { if (chk != null && chk.GetValue("")?.ToString().Contains("--user-data-dir") == true) isHijacked = true; } } catch { } if (isHijacked) { try { key.DeleteSubKeyTree(@"Software\Classes\ChromeHTML", false); } catch { } try { key.DeleteSubKeyTree(@"Software\Clients\StartMenuInternet\Google Chrome", false); } catch { } try { using (var regApps = key.OpenSubKey(@"Software\RegisteredApplications", true)) { regApps?.DeleteValue("Google Chrome", false); } } catch { } } } } catch { }
            try { using (var key = Microsoft.Win32.Registry.CurrentUser) { using (var progIdKey = key.CreateSubKey($@"Software\Classes\{progId}")) { progIdKey.SetValue("", appName); progIdKey.SetValue("URL Protocol", ""); using (var icon = progIdKey.CreateSubKey("DefaultIcon")) icon.SetValue("", iconPath); using (var cmd = progIdKey.CreateSubKey(@"shell\open\command")) { cmd.SetValue("", $"\"{exePath}\" --single-argument %1"); cmd.SetValue("DelegateExecute", ""); } try { progIdKey.DeleteSubKeyTree(@"shell\open\ddeexec", false); } catch { } using (var appKey = progIdKey.CreateSubKey("Application")) { appKey.SetValue("ApplicationIcon", iconPath); appKey.SetValue("ApplicationName", appName); appKey.SetValue("ApplicationDescription", appDesc); appKey.SetValue("ApplicationCompany", "Google"); } } using (var clientKey = key.CreateSubKey($@"Software\Clients\StartMenuInternet\{clientName}")) { clientKey.SetValue("", appName); using (var icon = clientKey.CreateSubKey("DefaultIcon")) icon.SetValue("", iconPath); using (var cmd = clientKey.CreateSubKey(@"shell\open\command")) cmd.SetValue("", $"\"{exePath}\""); using (var capKey = clientKey.CreateSubKey("Capabilities")) { capKey.SetValue("ApplicationDescription", appDesc); capKey.SetValue("ApplicationIcon", iconPath); capKey.SetValue("ApplicationName", appName); using (var fileAssoc = capKey.CreateSubKey("FileAssociations")) { fileAssoc.SetValue(".htm", progId); fileAssoc.SetValue(".html", progId); fileAssoc.SetValue(".webp", progId); fileAssoc.SetValue(".pdf", progId); } using (var urlAssoc = capKey.CreateSubKey("URLAssociations")) { urlAssoc.SetValue("http", progId); urlAssoc.SetValue("https", progId); urlAssoc.SetValue("ftp", progId); } } } using (var regAppsKey = key.CreateSubKey(@"Software\RegisteredApplications")) { regAppsKey.SetValue(clientName, $@"Software\Clients\StartMenuInternet\{clientName}\Capabilities"); } } } catch (Exception ex) { MessageBox.Show($"注册失败: {ex.Message}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ForceSetLegacyDefault()
        {
            try { string exePath = GetNormalizedPath(chromeExe); string configJson = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}"; ApplyEnterprisePolicies(configJson); using (var key = Microsoft.Win32.Registry.CurrentUser) { string[] protocols = { "http", "https" }; foreach (var p in protocols) { using (var cmdKey = key.CreateSubKey($@"Software\Classes\{p}\shell\open\command")) { cmdKey.SetValue("", $"\"{exePath}\" --single-argument %1"); cmdKey.SetValue("DelegateExecute", ""); } try { key.DeleteSubKeyTree($@"Software\Classes\{p}\shell\open\ddeexec", false); } catch { } } } } catch { }
        }

        private void UpdateBorderStyles(bool chromeExists, bool isDefault, bool shortcutsExist)
        {
            var activeRed = new SolidColorBrush(Color.FromRgb(231, 76, 60)); var defaultText = new SolidColorBrush(Color.FromRgb(52, 73, 94));
            if (rdo64.IsChecked == true) { rdo64.Foreground = activeRed; rdo64.FontWeight = FontWeights.Bold; rdo32.Foreground = defaultText; rdo32.FontWeight = FontWeights.Normal; } else { rdo32.Foreground = activeRed; rdo32.FontWeight = FontWeights.Bold; rdo64.Foreground = defaultText; rdo64.FontWeight = FontWeights.Normal; }
            for (int i = 0; i < cmbChannel.Items.Count; i++) { if (cmbChannel.Items[i] is ComboBoxItem item) { if (chromeExists && i == cmbChannel.SelectedIndex) { item.Foreground = activeRed; item.FontWeight = FontWeights.Bold; } else { item.Foreground = defaultText; item.FontWeight = FontWeights.Normal; } } }
            if (chromeExists) { cmbChannel.Foreground = activeRed; cmbChannel.FontWeight = FontWeights.Bold; if (isDefault) { rdoDefaultYes.IsChecked = true; rdoDefaultYes.Foreground = activeRed; rdoDefaultYes.FontWeight = FontWeights.Bold; rdoDefaultNo.Foreground = defaultText; rdoDefaultNo.FontWeight = FontWeights.Normal; } else { rdoDefaultNo.IsChecked = true; rdoDefaultNo.Foreground = activeRed; rdoDefaultNo.FontWeight = FontWeights.Bold; rdoDefaultYes.Foreground = defaultText; rdoDefaultYes.FontWeight = FontWeights.Normal; } if (shortcutsExist) { rdoShortcutYes.IsChecked = true; rdoShortcutYes.Foreground = activeRed; rdoShortcutYes.FontWeight = FontWeights.Bold; rdoShortcutNo.Foreground = defaultText; rdoShortcutNo.FontWeight = FontWeights.Normal; } else { rdoShortcutNo.IsChecked = true; rdoShortcutNo.Foreground = activeRed; rdoShortcutNo.FontWeight = FontWeights.Bold; rdoShortcutYes.Foreground = defaultText; rdoShortcutYes.FontWeight = FontWeights.Normal; } } else { cmbChannel.Foreground = defaultText; cmbChannel.FontWeight = FontWeights.Normal; rdoDefaultNo.IsChecked = true; rdoDefaultYes.Foreground = defaultText; rdoDefaultYes.FontWeight = FontWeights.Normal; rdoDefaultNo.Foreground = defaultText; rdoDefaultNo.FontWeight = FontWeights.Normal; rdoShortcutNo.IsChecked = true; rdoShortcutYes.Foreground = defaultText; rdoShortcutYes.FontWeight = FontWeights.Normal; rdoShortcutNo.Foreground = defaultText; rdoShortcutNo.FontWeight = FontWeights.Normal; }
            if (rdoDisableCacheYes.IsChecked == true) { rdoDisableCacheYes.Foreground = activeRed; rdoDisableCacheYes.FontWeight = FontWeights.Bold; rdoDisableCacheNo.Foreground = defaultText; rdoDisableCacheNo.FontWeight = FontWeights.Normal; } else { rdoDisableCacheNo.Foreground = activeRed; rdoDisableCacheNo.FontWeight = FontWeights.Bold; rdoDisableCacheYes.Foreground = defaultText; rdoDisableCacheYes.FontWeight = FontWeights.Normal; }
            if (rdoLimitYes.IsChecked == true) { rdoLimitYes.Foreground = activeRed; rdoLimitYes.FontWeight = FontWeights.Bold; rdoLimitNo.Foreground = defaultText; rdoLimitNo.FontWeight = FontWeights.Normal; } else { rdoLimitNo.Foreground = activeRed; rdoLimitNo.FontWeight = FontWeights.Bold; rdoLimitYes.Foreground = defaultText; rdoLimitYes.FontWeight = FontWeights.Normal; }
        }
    }
}
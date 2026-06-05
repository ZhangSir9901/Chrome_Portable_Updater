# =========================================================================
# 核心配置与防报错设置 (已包含详尽备注)
# =========================================================================
$ErrorActionPreference = "Stop"  # 遇错立停，触发捕获
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8 # 强制控制台输出 UTF-8

# 启用 TLS 1.2/1.3 协议，防止老旧系统因网络安全协议过低无法下载
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls13

# =========================================================================
# 路径定义 (从环境安全获取路径，支持空格路径)
# =========================================================================
$BaseDir        = (Get-Location).Path                     # 强制获取当前执行目录
$BinDir         = Join-Path $BaseDir "bin"                # 存放 7zr 工具的目录
$ChromeDir      = Join-Path $BaseDir "Chrome"             # 存放 Chrome 主程序的目录
$ChromeExe      = Join-Path $ChromeDir "chrome.exe"       # Chrome.exe 主程序路径
$UserData       = Join-Path $BaseDir "UserData"           # 便携版数据目录
$TempDir        = Join-Path $BaseDir "temp"               # 下载临时目录
$SevenZip       = Join-Path $BinDir "7zr.exe"             # 7-Zip 解压引擎路径
$SysTempWorkDir = Join-Path $BaseDir "_Install_Workspace" # 临时解包工作区
$LogFile        = Join-Path $BaseDir "Chrome_Update_Debug.log" # 调试日志路径

# =========================================================================
# 【核心要求】日志系统：每次启动强制覆写清空旧日志
# =========================================================================
$LogHeader = @"
===========================================================
Chrome Updater Debug Log (每次运行自动覆盖)
启动时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
运行路径: $BaseDir
===========================================================
"@
$LogHeader | Out-File -LiteralPath $LogFile -Force -Encoding UTF8

# =========================================================================
# 统一日志输出函数 (同时输出到屏幕和日志文件)
# =========================================================================
function Log ($Msg, $Color="Cyan") {
    $Time = Get-Date -Format "HH:mm:ss"
    $FormattedMsg = "[$Time] $Msg"
    
    # 输出到屏幕
    Write-Host " [$Time] " -NoNewline -ForegroundColor Gray
    Write-Host ">> " -NoNewline -ForegroundColor White
    Write-Host $Msg -ForegroundColor $Color
    
    # 写入日志
    Add-Content -LiteralPath $LogFile -Value $FormattedMsg -Encoding UTF8
}

# =========================================================================
# UI 界面绘制函数 (在这里可以随意修改显示的信息和排版)
# =========================================================================
function Show-Header {
    Clear-Host
    Write-Host "╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║              谷歌浏览器便携版智能升级 (GitHub Edition)               ║" -ForegroundColor Cyan
    Write-Host "╟──────────────────────────────────────────────────────────────────────╢" -ForegroundColor Cyan
    Write-Host "║      版本：26.06.05      出品人：偏店浪人      微信：jiujiujiayi666      ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

# 创建桌面快捷方式
function Create-Shortcut {
    if (!(Test-Path $ChromeExe)) { return }
    try {
        $LnkPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "Chrome Portable.lnk"
        $WshShell = New-Object -ComObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut($LnkPath)
        $Shortcut.TargetPath = $ChromeExe
        $Shortcut.Arguments = "--user-data-dir=`"$UserData`""
        $Shortcut.WorkingDirectory = $BaseDir
        $Shortcut.IconLocation = "$ChromeExe,0"
        $Shortcut.Save()
        Log "桌面快捷方式创建/更新成功。" "Green"
    } catch {
        Log "警告: 创建快捷方式失败，但不影响使用。原因: $($_.Exception.Message)" "Yellow"
    }
}

# 启动浏览器
function Start-Browser {
    Log "正在启动谷歌浏览器便携版..." "Green"
    Create-Shortcut
    Start-Process $ChromeExe -ArgumentList "--user-data-dir=`"$UserData`""
}

# 带百分比进度条的大文件下载函数
function Download-With-Progress ($Url, $OutFile) {
    try {
        Log "连接下载服务器..." "Yellow"
        $Request = [System.Net.WebRequest]::Create($Url)
        $Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0"
        $Response = $Request.GetResponse()
        
        if ($Response.ContentType -match "text/html" -or $Response.ContentLength -lt 1MB) { 
            Log "服务器返回内容有误或文件太小。" "Red"
            return $false 
        }
        
        $TotalSize = $Response.ContentLength
        $Stream = $Response.GetResponseStream()
        $FileStream = [System.IO.File]::Create($OutFile)
        $Buffer = New-Object byte[] 65536
        $TotalRead = 0
        $Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        [Console]::CursorVisible = $false
        
        while (($BytesRead = $Stream.Read($Buffer, 0, $Buffer.Length)) -gt 0) {
            $FileStream.Write($Buffer, 0, $BytesRead); $TotalRead += $BytesRead
            if ($Stopwatch.ElapsedMilliseconds -gt 200 -or $TotalRead -eq $TotalSize) {
                $Percent = [math]::Round(($TotalRead / $TotalSize) * 100)
                Write-Host -NoNewline ("`r [系统] 正在下载: {0}% [{1:N2}MB / {2:N2}MB] " -f $Percent, ($TotalRead/1MB), ($TotalSize/1MB)) -ForegroundColor Yellow
                $Stopwatch.Restart()
            }
        }
        [Console]::CursorVisible = $true; Write-Host ""
        $FileStream.Close(); $Stream.Close(); $Response.Close()
        return $true
    } catch {
        Log "下载过程中遭遇网络中断，详细报错: $($_.Exception.Message)" "Red"
        return $false 
    }
}

# 抓取和解析异次元(iplaysoft)提供的数据源，获取最新 Chrome 官方版本和下载链接
function Parse-Page-Data {
    try {
        $Wc = New-Object System.Net.WebClient
        $Wc.Encoding = [System.Text.Encoding]::UTF8
        $Wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)")
        Log "正在同步远程最新数据..." "Cyan"
        $Html = $Wc.DownloadString("https://www.iplaysoft.com/tools/chrome/")
        $HtmlFlat = $Html -replace "[\r\n\t]", " " -replace "\s+", " "
        
        $TargetTitles = @(
            @{ Title="Windows 正式版 / 稳定版 64 位"; Channel="Stable"; Arch="x64" },
            @{ Title="Windows 测试版 64 位"; Channel="Beta"; Arch="x64" },
            @{ Title="Windows 开发版 64 位"; Channel="Dev"; Arch="x64" }
        )
        $Results = @()
        foreach ($T in $TargetTitles) {
            $Pos = $HtmlFlat.IndexOf($T.Title)
            if ($Pos -ge 0) {
                $Block = $HtmlFlat.Substring($Pos, [math]::Min(2500, $HtmlFlat.Length - $Pos))
                if ($Block -match 'v(\d+\.\d+\.\d+\.\d+)') {
                    $Ver = $Matches[1]
                    $UrlMatches = [regex]::Matches($Block, 'href="([^"]+?\.exe)"')
                    if ($UrlMatches.Count -gt 0) {
                        $Results += [PSCustomObject]@{ Channel=$T.Channel; Arch=$T.Arch; Version=$Ver; Url=$UrlMatches[0].Groups[1].Value; BakUrl=$UrlMatches[1].Groups[1].Value; Display=$T.Title }
                    }
                }
            }
        }
        return $Results
    } catch {
        Log "解析网页失败: $($_.Exception.Message)" "Red"
        return @()
    }
}

# =======================================================
# 主执行流程 (带有全局崩溃防御与日志系统)
# =======================================================
try {
    Show-Header

    # 1. 自动提权检查 (如果未授权，会自动弹窗请求并在新窗口中继续)
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Log "检测到非管理员权限，正在请求提权..." "Yellow"
        Start-Process cmd.exe -ArgumentList "/c `"$ScriptPath`"" -Verb RunAs
        exit
    }

    # 2. 目录完整性自检与初始化
    if (!(Test-Path $BinDir)) { New-Item -ItemType Directory -Path $BinDir | Out-Null }
    if (!(Test-Path $UserData)) { New-Item -ItemType Directory -Path $UserData | Out-Null }
    if (!(Test-Path $SevenZip)) {
        Log "缺失解压工具，正在从 7-Zip 官网下载 7zr.exe..." "Yellow"
        Invoke-WebRequest -Uri "https://www.7-zip.org/a/7zr.exe" -OutFile $SevenZip
        Log "解压工具已就绪。" "Green"
    }

    # 3. 获取本地已安装的 Chrome 版本
    $LocalVer = "0.0.0.0"
    $LocalChannel = "Stable"
    if (Test-Path $ChromeExe) {
        $Info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ChromeExe)
        $LocalVer = $Info.ProductVersion
        if ($Info.FileDescription -match "Beta") { $LocalChannel = "Beta" }
        elseif ($Info.FileDescription -match "Dev") { $LocalChannel = "Dev" }
    }
    Log "本地版本检测: $LocalVer ($LocalChannel)" "Gray"

    # 4. 与服务器同步，比对版本
    $DataList = Parse-Page-Data
    if ($DataList.Count -eq 0) { 
        throw "同步失败，请检查网络。" 
    }

    $Match = $DataList | Where-Object { $_.Channel -eq $LocalChannel -and $_.Arch -eq "x64" } | Select-Object -First 1
    $Updated = $false

    # 5. 发现新版本，执行下载和部署
    if ($Match -and [version]$Match.Version -gt [version]$LocalVer) {
        Log "发现新版本: v$($Match.Version)" "Yellow"
        if (!(Test-Path $TempDir)) { New-Item -ItemType Directory -Path $TempDir | Out-Null }
        $SetupFile = Join-Path $TempDir "setup.exe"
        
        if (!(Download-With-Progress $Match.Url $SetupFile)) {
            Log "主下载通道失败，正在切换备用通道..." "Yellow"
            if (!(Download-With-Progress $Match.BakUrl $SetupFile)) { 
                throw "下载通道全部失效，请稍后再试。" 
            }
        }
        
        # 部署阶段：结束正在运行的 Chrome 进程防止文件占用
        if (Get-Process chrome -ErrorAction SilentlyContinue) { 
            Log "检测到浏览器正在运行，正在强制关闭以进行升级..." "Red"
            Stop-Process -Name chrome -Force
            Start-Sleep 2 
        }
        
        Log "正在解包并部署新版本 (可能需要1分钟)..." "Cyan"
        New-Item -ItemType Directory -Path $SysTempWorkDir | Out-Null
        $Queue = new-object System.Collections.Queue
        $Queue.Enqueue($SetupFile)
        $Found = $false
        $ChromeSrc = $null
        $Step = 0
        
        # 递归解压
        while ($Queue.Count -gt 0 -and $Step -lt 10) {
            $Cur = $Queue.Dequeue()
            $Step++
            $Work = Join-Path $SysTempWorkDir "Job_$Step"
            New-Item -ItemType Directory -Path $Work | Out-Null
            $P = Start-Process -FilePath $SevenZip -ArgumentList "x `"$Cur`" -o`"$Work`" -y" -PassThru -Wait -WindowStyle Hidden
            if ($P.ExitCode -le 1) {
                $Exe = Get-ChildItem $Work -Filter "chrome.exe" -Recurse | Select-Object -First 1
                if ($Exe) { 
                    $Found = $true
                    $ChromeSrc = $Exe.DirectoryName
                    break 
                }
                Get-ChildItem $Work -Recurse | Where-Object { $_.Length -gt 20MB -and $_.Extension -ne ".exe" } | ForEach-Object { $Queue.Enqueue($_.FullName) }
            }
        }
        
        # 替换老版 Chrome 目录
        if ($Found) {
            Log "正在覆盖替换 Chrome 主程序..." "Cyan"
            if (Test-Path $ChromeDir) { Remove-Item $ChromeDir -Recurse -Force }
            Copy-Item "$ChromeSrc\*" $ChromeDir -Recurse -Force
            $Updated = $true
        } else {
            throw "解包失败，未能提取到核心程序。"
        }
        
        # 垃圾清理
        Remove-Item $SysTempWorkDir -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    # 6. 结束与交互
    if ($Updated) {
        Log "Chrome 升级成功！" "Green"
        $Wsh = New-Object -ComObject WScript.Shell
        $Wsh.Popup("升级完成，最新版本为：$($Match.Version)", 7, "Chrome 便携版", 64) | Out-Null
        exit
    } else {
        Log "当前已经是最新版，无需更新。" "Green"
        Write-Host ""
        Write-Host " ---------------------------------------" -ForegroundColor Gray
        Write-Host " [O] 启动并打开浏览器 (Open)" -ForegroundColor White
        Write-Host " [Q] 直接退出升级脚本 (Quit) - [默认]" -ForegroundColor White
        Write-Host " ---------------------------------------" -ForegroundColor Gray
        
        $Choice = "Q"
        $Timeout = 7
        Write-Host " 请选择操作 (倒计时 $Timeout 秒自动退出): " -NoNewline
        
        while ($Timeout -gt 0) {
            if ([Console]::KeyAvailable) {
                $Key = [Console]::ReadKey($true)
                if ($Key.Key -eq 'O' -or $Key.Key -eq 'o') { $Choice = "O"; break }
                if ($Key.Key -eq 'Q' -or $Key.Key -eq 'q') { $Choice = "Q"; break }
            }
            Write-Host -NoNewline "$Timeout.." -ForegroundColor Yellow
            Start-Sleep 1
            $Timeout--
        }
        
        Write-Host ""
        if ($Choice -eq "O") {
            Start-Browser
        } else {
            Log "正在退出更新程序..." "Gray"
            Start-Sleep 1
        }
        exit
    }
}
catch {
    Log "【遇到严重错误导致程序被迫中止】" "Red"
    Log "报错信息: $($_.Exception.Message)" "Red"
    
    # 写入崩溃详情至日志
    "--- EXCEPTION STACKTRACE ---" | Add-Content -LiteralPath $LogFile -Encoding UTF8
    $_.ScriptStackTrace | Add-Content -LiteralPath $LogFile -Encoding UTF8
    "----------------------------" | Add-Content -LiteralPath $LogFile -Encoding UTF8
    Log "调试日志已保存至 Chrome_Update_Debug.log" "Yellow"
}
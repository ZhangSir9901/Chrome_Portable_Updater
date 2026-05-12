@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

:: ============================================================================
::   项目名称: Chrome Portable Launcher & Updater
::   版本: V53.2026.02
::   更新日期: 2026-05-12
::   作者: AI助手 & ZhangSir
:: ============================================================================

:: 1. 自动提权
openfiles >nul 2>&1
if %errorlevel% NEQ 0 (
    echo [System] Requesting Admin Privileges...
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /b
)

:: 2. 提取并运行 PowerShell 核心
set "PS_SCRIPT=%temp%\chrome_v53.ps1"
if exist "%PS_SCRIPT%" del "%PS_SCRIPT%"
powershell -Command "$c = Get-Content -LiteralPath '%~f0'; $i = [array]::IndexOf($c, '__POWERSHELL_BEGIN__'); if($i -ge 0){ $c[($i+1)..($c.Count-1)] | Out-File -Encoding UTF8 '%PS_SCRIPT%' }"

if exist "%PS_SCRIPT%" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%"
) else (
    echo [Fatal Error] Script extraction failed.
    pause
)

if exist "%PS_SCRIPT%" del "%PS_SCRIPT%"
exit /b

:: ============================================================================
::   PowerShell 核心逻辑
:: ============================================================================
__POWERSHELL_BEGIN__

# --- 环境配置 ---
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::GetEncoding("GB2312")
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls13

# --- 路径定义 ---
$BaseDir = (Get-Location).Path
$BinDir = Join-Path $BaseDir "bin"
$ChromeDir = Join-Path $BaseDir "Chrome"
$ChromeExe = Join-Path $ChromeDir "chrome.exe"
$UserData = Join-Path $BaseDir "UserData"
$TempDir = Join-Path $BaseDir "temp"
$SevenZip = Join-Path $BinDir "7zr.exe"
$SysTempWorkDir = Join-Path $BaseDir "_Install_Workspace"

# --- UI 组件 ---
function Show-Header {
    Clear-Host
    Write-Host "╔══════════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║        CHROME PORTABLE LAUNCHER & UPDATER (V53 GitHub Edition)       ║" -ForegroundColor Cyan
    Write-Host "╟──────────────────────────────────────────────────────────────────────╢" -ForegroundColor Cyan
    Write-Host "║  Build: 2026.02.05  |  Environment: Windows x64  |  Source: Iplaysoft║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Log ($Msg, $Color="Cyan") {
    $Time = Get-Date -Format "HH:mm:ss"
    Write-Host " [$Time] " -NoNewline -ForegroundColor Gray
    Write-Host ">> " -NoNewline -ForegroundColor White
    Write-Host $Msg -ForegroundColor $Color
}

# --- 功能组件 ---
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
    } catch {}
}

function Start-Browser {
    Log "正在启动浏览器..." "Green"
    Create-Shortcut
    Start-Process $ChromeExe -ArgumentList "--user-data-dir=`"$UserData`""
}

function Download-With-Progress ($Url, $OutFile) {
    try {
        $Request = [System.Net.WebRequest]::Create($Url)
        $Request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0"
        $Response = $Request.GetResponse()
        if ($Response.ContentType -match "text/html" -or $Response.ContentLength -lt 1MB) { return $false }
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
                Write-Host -NoNewline ("`r [System] Downloading: {0}% [{1:N2}MB / {2:N2}MB]   " -f $Percent, ($TotalRead/1MB), ($TotalSize/1MB)) -ForegroundColor Yellow
                $Stopwatch.Restart()
            }
        }
        [Console]::CursorVisible = $true; Write-Host ""
        $FileStream.Close(); $Stream.Close(); $Response.Close()
        return $true
    } catch { return $false }
}

function Parse-Page-Data {
    try {
        $Wc = New-Object System.Net.WebClient
        $Wc.Encoding = [System.Text.Encoding]::UTF8
        $Wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)")
        $Html = $Wc.DownloadString("https://www.iplaysoft.com/tools/chrome/")
        $HtmlFlat = $Html -replace "[\r\n\t]", " " -replace "\s+", " "
        $TargetTitles = @(
            @{ Title="Windows 正式版 / 稳定版 64 位"; Channel="Stable"; Arch="x64" },
            @{ Title="Windows 测试版 64 位";        Channel="Beta";   Arch="x64" },
            @{ Title="Windows 开发版 64 位";        Channel="Dev";    Arch="x64" }
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
    } catch { return @() }
}

# --- 主逻辑 ---
Show-Header

# 1. 初始化
if (!(Test-Path $BinDir)) { New-Item -ItemType Directory -Path $BinDir | Out-Null }
if (!(Test-Path $UserData)) { New-Item -ItemType Directory -Path $UserData | Out-Null }
if (!(Test-Path $SevenZip)) {
    Log "正在获取核心工具 (7zr.exe)..." "Yellow"
    Invoke-WebRequest -Uri "https://www.7-zip.org/a/7zr.exe" -OutFile $SevenZip
}

# 2. 本地自检
$LocalVer = "0.0.0.0"
$LocalChannel = "Stable"
if (Test-Path $ChromeExe) {
    $Info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ChromeExe)
    $LocalVer = $Info.ProductVersion
    if ($Info.FileDescription -match "Beta") { $LocalChannel = "Beta" }
    elseif ($Info.FileDescription -match "Dev") { $LocalChannel = "Dev" }
}
Log "本地版本: $LocalVer ($LocalChannel)" "Gray"

# 3. 检查更新
Log "正在同步远程数据源..." "Cyan"
$DataList = Parse-Page-Data
if ($DataList.Count -eq 0) { Log "警告: 无法同步页面数据。" "Red"; Pause; exit }

$Match = $DataList | Where-Object { $_.Channel -eq $LocalChannel -and $_.Arch -eq "x64" } | Select-Object -First 1
$Updated = $false

if ($Match -and [version]$Match.Version -gt [version]$LocalVer) {
    Log "发现更新: v$($Match.Version)" "Yellow"
    if (!(Test-Path $TempDir)) { New-Item -ItemType Directory -Path $TempDir | Out-Null }
    $SetupFile = Join-Path $TempDir "setup.exe"
    
    if (!(Download-With-Progress $Match.Url $SetupFile)) {
        Log "主线路失败，尝试备用线路..." "Yellow"
        if (!(Download-With-Progress $Match.BakUrl $SetupFile)) { Log "错误: 线路全部失效。" "Red"; Pause; exit }
    }
    
    # 部署
    if (Get-Process chrome -ErrorAction SilentlyContinue) { Log "正在强制结束浏览器进程..." "Red"; Stop-Process -Name chrome -Force; Start-Sleep 2 }
    Log "正在解压并部署新版本 (可能需要1分钟)..." "Cyan"
    New-Item -ItemType Directory -Path $SysTempWorkDir | Out-Null
    $Queue = new-object System.Collections.Queue; $Queue.Enqueue($SetupFile)
    $Found = $false; $ChromeSrc = $null; $Step = 0
    while ($Queue.Count -gt 0 -and $Step -lt 10) {
        $Cur = $Queue.Dequeue(); $Step++
        $Work = Join-Path $SysTempWorkDir "Job_$Step"; New-Item -ItemType Directory -Path $Work | Out-Null
        $P = Start-Process -FilePath $SevenZip -ArgumentList "x `"$Cur`" -o`"$Work`" -y" -PassThru -Wait -WindowStyle Hidden
        if ($P.ExitCode -le 1) {
            $Exe = Get-ChildItem $Work -Filter "chrome.exe" -Recurse | Select-Object -First 1
            if ($Exe) { $Found = $true; $ChromeSrc = $Exe.DirectoryName; break }
            Get-ChildItem $Work -Recurse | Where-Object { $_.Length -gt 20MB -and $_.Extension -ne ".exe" } | ForEach-Object { $Queue.Enqueue($_.FullName) }
        }
    }
    if ($Found) {
        if (Test-Path $ChromeDir) { Remove-Item $ChromeDir -Recurse -Force }
        Copy-Item "$ChromeSrc\*" $ChromeDir -Recurse -Force
        $Updated = $true
    }
    Remove-Item $SysTempWorkDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
}

# 4. 流程结束分支 (需求 a & b)
if ($Updated) {
    Log "升级任务已完成！" "Green"
    $Wsh = New-Object -ComObject WScript.Shell
    # 对话框提示 (文本, 秒数, 标题, 图标类型)
    $Wsh.Popup("老板，浏览器已经升级到最新的版本：$($Match.Version)", 7, "Chrome Updater", 64) | Out-Null
    exit
} else {
    Log "当前已是最新版，无需升级。" "Green"
    Write-Host ""
    Write-Host " ---------------------------------------" -ForegroundColor Gray
    Write-Host "  [O] 打开浏览器 (Open)" -ForegroundColor White
    Write-Host "  [Q] 直接退出   (Quit) - [默认]" -ForegroundColor White
    Write-Host " ---------------------------------------" -ForegroundColor Gray
    
    $Choice = "Q"
    $Timeout = 7
    Write-Host " 请选择操作 (倒计时 $Timeout 秒): " -NoNewline
    
    while ($Timeout -gt 0) {
        if ([Console]::KeyAvailable) {
            $Key = [Console]::ReadKey($true)
            if ($Key.Key -eq 'O') { $Choice = "O"; break }
            if ($Key.Key -eq 'Q') { $Choice = "Q"; break }
        }
        Write-Host -NoNewline "$Timeout.." -ForegroundColor Yellow
        Start-Sleep 1
        $Timeout--
    }
    
    Write-Host ""
    if ($Choice -eq "O") {
        Start-Browser
    } else {
        Log "正在退出脚本..." "Gray"
        Start-Sleep 1
    }
    exit
}

# EOF_MARKER_CHECK

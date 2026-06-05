@echo off
title Chrome Portable Launcher
cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -Command "try { $c = Get-Content -LiteralPath 'Chrome_Portable_Updater.ps1' -Encoding UTF8 -Raw; Invoke-Expression $c } catch { Write-Error $_; Read-Host 'Error occurred. Press Enter to exit...' }"

if %errorlevel% neq 0 pause
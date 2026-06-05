# Chrome Portable Launcher & Updater (智能更新器)

[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://www.microsoft.com/windows)
[![Version](https://img.shields.io/badge/Version-v26.06.05-green.svg)](https://github.com/ZhangSir9901/Chrome_Portable_Updater)
[![Source](https://img.shields.io/badge/Source-Iplaysoft-orange.svg)](https://www.iplaysoft.com/tools/chrome/)

一个基于纯净 CMD 与现代 PowerShell 架构开发的 **Google Chrome 便携版增强启动与自动升级工具**。专为追求“干净、便携、安全、无残留、自动化”的用户设计。

---

## 🌟 核心特性

- **真正便携化 (Portable)**：强制将所有用户数据（书签、插件、历史记录、缓存）重定向并存储在程序同级目录的 `UserData` 文件夹中，不污染系统 C 盘。
- **抗灾避错外壳 (Anti-Mangle Wrapper)**：启动器 `.bat` 采用 100% 纯英文（ASCII）环境构建，彻底杜绝了 Windows 中文环境下，由于 `UTF-8` 与 `GBK` 编码冲突导致的文件吞噬、乱码和闪退。
- **智能解压部署 (Smart Unzip)**：内置 7-Zip 递归解压逻辑，能从 Google 官方复杂的安装包（exe/7z）中自动提取核心程序文件。
- **全自动提权机制**：双击运行自动申请管理员特权（UAC），并在安全提权后完美继承原执行路径。
- **故障诊断日志 (Debug System)**：自带一键日志覆写系统。每次运行会自动强制清空、覆写历史日志，若遇到网络或部署问题，会在同级目录下生成详尽的 `Chrome_Update_Debug.log` 堆栈日志。

---

## 🚀 快速开始

为了满足不同用户的需求，项目提供以下两种部署与使用方式：

### 推荐方法 A：小白用户（一键双击 `.exe`）
1. 前往本仓库右侧的 [Releases (发布页面)](https://github.com/ZhangSir9901/Chrome_Portable_Updater/releases) 下载打包好的 **`Chrome_Portable_Updater.exe`**。
2. 将该 `.exe` 放置在你希望存放 Chrome 的目录下（例如 `D:\Program Files\Chrome\`）。
3. 双击运行即可！

### 推荐方法 B：进阶/开发者用户（源码运行）
1. 下载本仓库中 `main` 分支下的两个核心源码文件：
   - `Chrome_Portable_Updater.bat` （100% 纯 ASCII 避错启动脚本）
   - `Chrome_Portable_Updater.ps1` （自愈型更新器主体）
2. 将这两个文件放于同级目录下（例如 `D:\Tools\Chrome\`）。
3. 双击运行 `Chrome_Portable_Updater.bat` 即可拉起升级和启动流程。

---

## 📁 目录结构

升级并启动后，您的 Chrome 文件夹将呈现如下优雅而清爽的结构：

```text
.
├── bin/                          # 核心解压引擎（自动拉取 7zr.exe）
├── Chrome/                       # Chrome 主程序目录（完全隔离的绿色运行体）
├── UserData/                     # 个人用户数据（包含你的书签、插件，最核心资产，请定期备份！）
├── Chrome_Portable_Updater.bat   # 100% 纯英文批处理启动器
├── Chrome_Portable_Updater.ps1   # 强悍的自愈型 PowerShell 升级器
└── Chrome_Update_Debug.log       # 强制覆写的最新调试与诊断日志

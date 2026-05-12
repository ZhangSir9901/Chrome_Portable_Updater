# Chrome Portable Launcher & Updater

[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://www.microsoft.com/windows)
[![Version](https://img.shields.io/badge/Version-V53.2026-green.svg)](https://github.com/ZhangSir9901)
[![Source](https://img.shields.io/badge/Source-Iplaysoft-orange.svg)](https://www.iplaysoft.com/tools/chrome/)

一个基于 Batch 和 PowerShell 开发的 **Google Chrome 便携版增强启动与自动升级工具**。专为追求“干净、便携、自动化”的用户设计。

## 🌟 核心特性

- **真正便携化**：强制将所有用户数据（书签、插件、历史记录）存储在程序同级目录的 `UserData` 文件夹中，不污染系统路径。
- **自动静默升级**：自动同步异次元（Iplaysoft）提供的官方离线安装包，支持正式版、测试版、开发版等多通道自动识别。
- **智能解压部署**：内置 7-Zip 递归解压逻辑，能从 Google 官方复杂的安装包（exe/7z）中自动提取核心程序文件。
- **极简交互设计**：
    - 发现新版本并升级后，自动弹出确认框并倒计时退出。
    - 若无更新，提供 7 秒倒计时选择，支持一键启动浏览器或直接退出。
- **快捷方式维护**：每次启动自动在桌面生成带有专用参数的快捷方式，确保启动逻辑始终正确。

## 🚀 快速开始

1. **下载脚本**：下载本仓库中的 `Chrome_Portable_Updater.bat`。
2. **放置目录**：将脚本放置在您希望存放 Chrome 的目录下（如 `D:\Tools\Chrome\ `）。
3. **运行**：双击运行脚本。
    - 如果目录下没有 Chrome，它会自动下载并安装。
    - 如果已有 Chrome，它会自动检查更新并根据您的选择启动或升级。

## 📁 目录结构

运行后，您的文件夹将呈现如下结构：
```text
.
├── Chrome/             # Chrome 核心程序文件
├── UserData/           # 您的所有个人数据（核心资产，建议定期备份）
├── bin/                # 存放 7zr.exe 等辅助工具
├── Launcher.log        # 运行日志
└── Chrome_Portable_Updater.bat  # 启动与升级程序
```

## 🛠️ 技术要求

- **操作系统**：Windows 10 / 11 (x64)
- **权限**：脚本运行需要管理员权限（用于解压和写文件）
- **依赖**：系统需自带 PowerShell 5.1+（Win10自带）及 `curl.exe`

## ⚖️ 免责声明

1. 本脚本仅作为自动化工具使用，不包含任何 Google Chrome 的程序代码。
2. 浏览器程序和离线安装包链接均来源于 Google 官方及异次元软件世界分享。
3. 请确保在合规的环境下使用 Google Chrome 浏览器。

---
**ZhangSir9901** 项目作品 | [访问 GitHub 主页](https://github.com/ZhangSir9901)
